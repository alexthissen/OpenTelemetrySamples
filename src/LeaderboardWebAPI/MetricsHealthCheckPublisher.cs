using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace LeaderboardWebAPI
{
    public class MetricsHealthCheckPublisher : IHealthCheckPublisher
    {
        public Task PublishAsync(HealthReport report, CancellationToken cancellationToken)
        {

            foreach (var reportEntry in report.Entries)
            {
                switch (reportEntry.Value)
                {
                    case { Status: HealthStatus.Healthy }:
                        HealthCheckMeter.HealthyCheck(reportEntry.Key);
                        break;
                    case { Status: HealthStatus.Degraded }:
                        HealthCheckMeter.DegradedCheck(reportEntry.Key);
                        break;
                    case { Status: HealthStatus.Unhealthy }:
                        HealthCheckMeter.UnhealthyCheck(reportEntry.Key);
                        break;
                }
            }

            return Task.CompletedTask;
        }
    }
    
    public static class HealthCheckMeter
    {
        
        private static readonly Meter Meter = new Meter(MeterName);
        private static readonly Counter<int> HealthyCheckCounter;
        private static readonly Counter<int> DegradedCheckCounter;
        private static readonly Counter<int> UnhealthyCheckCounter;
        
        static HealthCheckMeter()
        {
            HealthyCheckCounter = Meter.CreateCounter<int>("healthcheck.count", "points", "Health check count");
            UnhealthyCheckCounter = Meter.CreateCounter<int>("healthcheck.unhealthy.count", "points", "Unhealthy health check count");
            DegradedCheckCounter = Meter.CreateCounter<int>("healthcheck.degraded.count", "points", "Degraded health check count");
        }

        public static string MeterName => "leaderboard.healthcheck";

        public static void HealthyCheck(string reportEntryKey) => HealthyCheckCounter.Add(1, new []{ new KeyValuePair<string, object>("check", reportEntryKey)});
        public static void DegradedCheck(string reportEntryKey) => DegradedCheckCounter.Add(1, new []{ new KeyValuePair<string, object>("check", reportEntryKey)});
        public static void UnhealthyCheck(string reportEntryKey) => UnhealthyCheckCounter.Add(1, new []{ new KeyValuePair<string, object>("check", reportEntryKey)});
    }
}