using Azure.Monitor.OpenTelemetry.AspNetCore;
using Azure.Monitor.OpenTelemetry.Exporter;
using GamingWebApp;
using GamingWebApp.Proxy;
using Microsoft.Extensions.Telemetry.Enrichment;
using Microsoft.Extensions.Telemetry.Metering;
using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;
using OpenTelemetry.ResourceDetectors.Container;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Polly;
using Polly.Extensions.Http;
using Polly.Timeout;
using Refit;
using System.Diagnostics;

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
    .RetryAsync(3, onRetry: (exception, retryCount) =>
    {
        // TODO: Change to new trace event
        // Trace.TraceInformation($"Retry #{retryCount}");
        //Activity.Current.RecordException(exception, new TagList().Add("Retry count", retryCount));
    });

builder.Services.AddHttpClient("WebAPIs", options =>
{
    options.BaseAddress = new Uri(builder.Configuration["LeaderboardApiOptions:BaseUrl"]);
    options.Timeout = TimeSpan.FromMilliseconds(15000);
    options.DefaultRequestHeaders.Add("ClientFactory", "Check");
})
.AddPolicyHandler(retry.WrapAsync(timeout))
.AddTypedClient(RestService.For<ILeaderboardClient>);

builder.Services.AddHsts(
    options =>
    {
        options.MaxAge = TimeSpan.FromDays(100);
        options.IncludeSubDomains = true;
        options.Preload = true;
    });

var resourceBuilder = ResourceBuilder.CreateDefault().AddService("gaming-web-app")
    .AddAttributes(new List<KeyValuePair<string, object>>() {
        new("app-version", "1.0"),
        new("service.name", "gaming-web-app"),
        new("service.namespace", "techorama"),
        new("service.instance.id", "gamingwebapp"),
        new("region", "west-europe")
    })
    .AddDetector(new ContainerResourceDetector());

builder.Services
    .AddOpenTelemetry()
        //.ConfigureResource(builder => builder.AddService("otel-worker-service"))
        .WithMetrics(provider => provider
            .AddMeter("Techorama.Metrics")
            //.AddPrometheusExporter()
            .AddOtlpExporter(options => options.Endpoint = new Uri("http://jaeger:4317"))
            .AddAzureMonitorMetricExporter(options =>
            {
                options.ConnectionString = builder.Configuration["ApplicationInsights:ConnectionString"];
            }))
        .WithTracing(provider =>
        {
            //builder.SetErrorStatusOnException(true);
            provider.AddSource("GamingWebApp");
            provider.AddServiceTraceEnricher(options =>
            {
                options.ApplicationName = true;
                options.EnvironmentName = true;
                options.BuildVersion = true;
                options.DeploymentRing = true;
            });
            provider.AddHttpClientInstrumentation();
            provider.AddAspNetCoreInstrumentation();
            provider.SetResourceBuilder(resourceBuilder);
            provider.AddConsoleExporter(options => options.Targets = ConsoleExporterOutputTargets.Console);
            provider.AddZipkinExporter(options => options.Endpoint = new Uri("http://zipkin:9411/api/v2/spans"));
            provider.AddOtlpExporter(options => options.Endpoint = new Uri("http://jaeger:4317"));
            provider.AddAzureMonitorTraceExporter(options =>
            {
                options.ConnectionString = builder.Configuration["ApplicationInsights:ConnectionString"];
            });
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

host.UseHttpsRedirection();
host.UseStaticFiles();
host.UseRouting();
host.UseAuthorization();
host.MapRazorPages();

host.Run();