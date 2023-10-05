using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using OpenTelemetry.Metrics;

namespace LeaderboardWebAPI.Infrastructure
{
    public static class HealthExtensions
    {
        public static IServiceCollection AddHealthMetrics(this IServiceCollection services)
        {
            services.AddSingleton<IHealthCheckPublisher, MetricsHealthCheckPublisher>();
        
            return services;
        }
        
        public static MeterProviderBuilder AddHealthCheckMetrics(
            this MeterProviderBuilder builder)
        {
            builder.AddMeter(HealthCheckMeter.MeterName);
            return builder;
        }
    }
}