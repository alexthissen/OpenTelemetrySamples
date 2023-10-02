using GamingWebApp;
using GamingWebApp.Proxy;
using Microsoft.Extensions.Telemetry.Enrichment;
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

var resourceBuilder = ResourceBuilder.CreateDefault().AddService("gaming-web-app")
   .AddAttributes(new List<KeyValuePair<string, object>>() {
        new("app-version", "1.0"),
        new("service.name", "gaming-web-app"),
        new("service.namespace", "techorama"),
        new("service.instance.id", "gamingwebapp"),
        new("region", "west-europe")
    })
   .AddDetector(new ContainerResourceDetector())
   .AddTelemetrySdk();

builder.Services.AddLogging(logging => logging.AddOpenTelemetry(openTelemetryLoggerOptions =>
{
    openTelemetryLoggerOptions.SetResourceBuilder(resourceBuilder);
    // Some important options to improve data quality
    openTelemetryLoggerOptions.IncludeScopes = true;
    openTelemetryLoggerOptions.IncludeFormattedMessage = true;
    openTelemetryLoggerOptions.AddOtlpExporter();
}));


builder.Services.AddServiceLogEnricher(options =>
{
    options.BuildVersion = true;
    options.ApplicationName = true;
    options.EnvironmentName = true;
    options.DeploymentRing = true;
});

builder.Services.AddSingleton(new HighScoreMeter());

builder.Services
    .AddOpenTelemetry()
        .WithMetrics(provider => provider
            .AddMeter(HighScoreMeter.Name)
            .AddOtlpExporter()
            // .AddAzureMonitorMetricExporter(options =>
            // {
            //     options.ConnectionString = builder.Configuration["ApplicationInsights:ConnectionString"];
            // })
                     )
        .WithTracing(provider =>
        {
            //builder.SetErrorStatusOnException(true);
            provider.AddSource(Diagnostics.GamingWebActivitySource.Name);
            provider.SetResourceBuilder(resourceBuilder);
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
            provider.AddOtlpExporter();
            
            //provider.AddZipkinExporter();
           
            // provider.AddAzureMonitorTraceExporter(options =>
            // {
            //     options.ConnectionString = builder.Configuration["ApplicationInsights:ConnectionString"];
            // });
            
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