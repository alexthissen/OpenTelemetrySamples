using System.Diagnostics;
using Azure.Monitor.OpenTelemetry.AspNetCore;
using GamingWebApp;
using GamingWebApp.Proxy;
using Microsoft.Extensions.AmbientMetadata;
using Microsoft.Extensions.Diagnostics.Enrichment;
using OpenTelemetry.Exporter;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.ResourceDetectors.Container;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Polly;
using Polly.Extensions.Http;
using Polly.Timeout;
using Refit;

var builder = WebApplication.CreateBuilder(args);

// Centrally stored policies
var policyRegistry = builder.Services.AddPolicyRegistry();
var timeoutPolicy = Policy.TimeoutAsync<HttpResponseMessage>(TimeSpan.FromMilliseconds(1500));
policyRegistry.Add("timeout", timeoutPolicy);

builder.Services.Configure<LeaderboardApiOptions>(builder.Configuration.GetSection(nameof(LeaderboardApiOptions)));
builder.Services.AddRazorPages();

var timeout = Policy.TimeoutAsync<HttpResponseMessage>(TimeSpan.FromMilliseconds(1500));
var retry = HttpPolicyExtensions
   .HandleTransientHttpError()
   .Or<TimeoutRejectedException>()
   .RetryAsync(2, onRetry: (exception, retryCount) =>
    {
        Activity.Current?.RecordException(exception.Exception,
            new TagList()
            {
                new KeyValuePair<string, object?>("retry-count", retryCount.ToString())
            });
        Activity.Current?.SetStatus(Status.Error);
    });

builder.Services.AddHttpClient("WebAPIs", options =>
    {
        options.BaseAddress = new Uri(builder.Configuration["LeaderboardApiOptions:BaseUrl"]);
        options.Timeout = TimeSpan.FromMilliseconds(15000);
        options.DefaultRequestHeaders.Add("ClientFactory", "Check");
    })
   .AddPolicyHandler(retry.WrapAsync(timeout))
   .AddTypedClient(RestService.For<ILeaderboardClient>);

var resourceBuilder = ResourceBuilder.CreateDefault()
   .AddService(serviceName: "gaming-web-app",
               serviceNamespace: "observability-demo",
               serviceVersion: "1.0.0",
               autoGenerateServiceInstanceId: false,
               serviceInstanceId: "gamingwebapp")
   .AddAttributes(new List<KeyValuePair<string, object>>()
    {
        new("app-version", "1.0"),
        new("region", "west-europe")
    })
   .AddDetector(new ContainerResourceDetector());

builder.Host.UseApplicationMetadata("AmbientMetadata:Application");
builder.Services.AddSingleton<HighScoreMeter>();
builder.Services.AddServiceLogEnricher(options =>
{
    options.BuildVersion = true;
    options.ApplicationName = true;
    options.EnvironmentName = true;
    options.DeploymentRing = true;
});

builder.Services
   .AddOpenTelemetry()
   .WithTracing(tracing =>
    {
        tracing.AddSource(Diagnostics.GamingWebActivitySource.Name);
        tracing.SetResourceBuilder(resourceBuilder);
        //tracing.AddServiceTraceEnricher(options =>
        //{
        //    options.ApplicationName = true;
        //    options.EnvironmentName = true;
        //    options.BuildVersion = true;
        //    options.DeploymentRing = true;
        //});
        tracing.AddHttpClientInstrumentation();
        tracing.AddAspNetCoreInstrumentation();

        tracing.AddConsoleExporter(options => options.Targets = ConsoleExporterOutputTargets.Console);
        tracing.AddOtlpExporter();
    })
   .WithMetrics(metrics =>
    {
        metrics.AddMeter(HighScoreMeter.Name);
        metrics.AddOtlpExporter();
    });

builder.Logging.AddOpenTelemetry(options =>
{
    // Some important options to improve data quality
    options.IncludeScopes = true;
    options.IncludeFormattedMessage = true;

    options.SetResourceBuilder(resourceBuilder);

    options.AddConsoleExporter();
    options.AddOtlpExporter();
});

var host = builder.Build();

if (host.Environment.IsDevelopment())
{
    host.UseDeveloperExceptionPage();
}
else
{
    host.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    host.UseHsts();
}

// host.UseHttpsRedirection();
host.UseStaticFiles();
host.UseRouting();
host.UseAuthorization();
host.MapRazorPages();

host.Run();