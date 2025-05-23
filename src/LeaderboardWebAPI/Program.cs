using Azure.Extensions.AspNetCore.Configuration.Secrets;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using LeaderboardWebAPI.Infrastructure;
using LeaderboardWebAPI.Metrics;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AmbientMetadata;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Diagnostics.Enrichment;
using Microsoft.Net.Http.Headers;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json;
using OpenTelemetry.Exporter;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Extensions.Diagnostics.Metrics;
using Azure.Monitor.OpenTelemetry.Exporter;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

var resourceBuilder = ResourceBuilder.CreateDefault()
     .AddService(serviceName: "leaderboard-web-api-service",
          serviceNamespace: "observability-demo",
          serviceVersion: "1.0",
          autoGenerateServiceInstanceId: false,
          serviceInstanceId: "leaderboardwebapi")
     .AddAttributes(new List<KeyValuePair<string, object>>
      {
          new("app-version", "1.0"),
          new("region", "west-europe")
      });

builder.Host.UseApplicationMetadata("AmbientMetadata:Application");
Activity.DefaultIdFormat = ActivityIdFormat.W3C;

builder.Services.AddMetrics();
builder.Services.AddSingleton<HighScoreMeter>();

builder.Services.AddOpenTelemetry()
    .WithTracing(tracing =>
    {
        tracing.AddSource(Diagnostics.LeaderboardActivitySource.Name);
        tracing.SetResourceBuilder(resourceBuilder);
        tracing.AddAspNetCoreInstrumentation();
        tracing.AddHttpClientInstrumentation();
        tracing.AddEntityFrameworkCoreInstrumentation(options => { options.SetDbStatementForText = true; });

        // Exporters
        tracing.AddConsoleExporter(options => options.Targets = ConsoleExporterOutputTargets.Console);
        tracing.AddOtlpExporter();
    })
    .WithMetrics(metrics =>
    {
        metrics.AddMeter(LeaderboardMeter.MeterName, HighScoreMeter.MeterName);
        metrics.AddHealthCheckMeter();
        metrics.SetResourceBuilder(resourceBuilder);

        // Exporters
        metrics.AddConsoleExporter();
        metrics.AddOtlpExporter();
    });

builder.Logging.AddOpenTelemetry(options =>
{
    options.SetResourceBuilder(resourceBuilder);

    // Some important options to improve data quality
    options.IncludeScopes = true;
    options.IncludeFormattedMessage = true;

    // Exporters
    options.AddOtlpExporter(exporter =>
    {
        exporter.Endpoint = new Uri("http://seq:5341/ingest/otlp/v1/logs");
        exporter.Protocol = OtlpExportProtocol.HttpProtobuf;
    });
});

builder.Services.AddProcessLogEnricher();
builder.Services.AddServiceLogEnricher(options =>
{
    options.BuildVersion = true;
    options.ApplicationName = true;
    options.EnvironmentName = true;
    options.DeploymentRing = true;
});

// Prepare health checks services
IHealthChecksBuilder healthChecks = builder.Services.AddHealthChecks();
builder.Services.AddHealthMetrics();
builder.Services.Configure<HealthCheckPublisherOptions>(options =>
{
    options.Delay = TimeSpan.FromSeconds(60);
});

// Add configuration provider for Azure Key Vault
if (!String.IsNullOrEmpty(builder.Configuration["KeyVaultUri"]))
{
    Uri keyVaultUri = new Uri(builder.Configuration["KeyVaultUri"]);
    ClientSecretCredential credential = new(
        builder.Configuration["KeyVaultTenantID"],
        builder.Configuration["KeyVaultClientID"],
        builder.Configuration["KeyVaultClientSecret"]);
    // For managed identities use:
    //   new DefaultAzureCredential()
    var secretClient = new SecretClient(keyVaultUri, credential);
    builder.Configuration.AddAzureKeyVault(secretClient, new KeyVaultSecretManager());

    healthChecks?.AddAzureKeyVault(keyVaultUri, credential,
        options =>
        {
            options
               .AddSecret("ApplicationInsights--InstrumentationKey")
               .AddKey("RetroKey");
        }, name: "keyvault"
    );
}

// Database 
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddDbContext<LeaderboardContext>(options =>
    {
        options.UseInMemoryDatabase("LeaderboardInMemoryDb");
    });
}
else
{
    builder.Services.AddDbContext<LeaderboardContext>(options =>
{
    string connectionString =
        builder.Configuration.GetConnectionString("LeaderboardContext");
    options.UseSqlServer(connectionString, sqlOptions =>
    {
        sqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(30),
            errorNumbersToAdd: null);
    });
});
}

builder.Services.AddDatabaseDeveloperPageExceptionFilter();
healthChecks?.AddDbContextCheck<LeaderboardContext>("database", tags: new[] { "ready" });

// Regular Web API services
builder.Services
       .AddControllers(options =>
        {
            options.RespectBrowserAcceptHeader = true;
            options.ReturnHttpNotAcceptable = true;
            options.FormatterMappings.SetMediaTypeMappingForFormat("json",
                new MediaTypeHeaderValue("application/json"));
            options.FormatterMappings.SetMediaTypeMappingForFormat("xml", new MediaTypeHeaderValue("application/xml"));
        })
       .AddNewtonsoftJson(setup => { setup.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore; })
       .AddControllersAsServices(); // For resolving controllers as services via DI

builder.Services.AddCors(options =>
{
    options.AddPolicy("CorsPolicy",
        builder => builder.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()
    );
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi("v1", options =>
{
    options.OpenApiVersion = Microsoft.OpenApi.OpenApiSpecVersion.OpenApi3_1;
});

WebApplication app = builder.Build();
app.UseCors("CorsPolicy");

if (app.Environment.IsDevelopment())
{
    // ApplicationServices does not exist anymore
    using (var scope = app.Services.CreateScope())
    {
        scope.ServiceProvider.GetRequiredService<LeaderboardContext>().Database.EnsureCreated();
    }

    app.UseDeveloperExceptionPage();
    app.MapOpenApi();
}

app.UseAuthorization();
app.MapControllers();
app.Run();