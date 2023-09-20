using HealthChecks.UI.Client;
using LeaderboardWebAPI.Infrastructure;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using System;

namespace LeaderboardWebApi.Infrastructure
{
    public static class InstrumentationExtensions
    {
        public static void MapInstrumentation(this WebApplication app)
        {
            if (app.Environment.IsDevelopment())
            {
                app.MapHealthChecks("/ping", new HealthCheckOptions() { Predicate = _ => false });
                app.MapHealthChecks("/health",
                    new HealthCheckOptions()
                    {
                        Predicate = _ => true,
                        ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
                    });
                app.MapHealthChecksUI();
            }

            // Readiness and liveliness endpoints
            app.MapHealthChecks("/health/ready",
                new HealthCheckOptions()
                {
                    Predicate = reg => reg.Tags.Contains("ready"),
                    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
                })
                .RequireHost($"*:{app.Configuration["ManagementPort"]}");
            app.MapHealthChecks("/health/lively",
                new HealthCheckOptions()
                {
                    Predicate = _ => true,
                    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
                })
                .RequireHost($"*:{app.Configuration["ManagementPort"]}");
        }

        public static void AddInstrumentation(this WebApplicationBuilder builder)
        {
            string connection = builder.Configuration["ApplicationInsights:ConnectionString"];

            IHealthChecksBuilder health = builder.Services.AddHealthChecks();
            health.AddApplicationInsightsPublisher(connectionString: connection);
            builder.Services.Configure<HealthCheckPublisherOptions>(options =>
            {
                options.Delay = TimeSpan.FromSeconds(60);
            });

            if (builder.Environment.IsDevelopment())
            {
                builder.Services.AddHealthChecksUI(setup =>
                {
                    setup.AddHealthCheckEndpoint("Leaderboard Web API Health checks", "http://localhost/health");
                    setup.SetEvaluationTimeInSeconds(60);
                })
                .AddInMemoryStorage();
            }


        }
    }
}
