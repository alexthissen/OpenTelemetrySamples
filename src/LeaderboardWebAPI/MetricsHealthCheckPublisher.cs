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
                HealthCheckMeter.HealthCheck(reportEntry.Key, reportEntry.Value.Status);
            }
            
            HealthCheckMeter.HealthCheck("leaderboard.healthcheck", report.Status);

            return Task.CompletedTask;
        }
    }

    public static class HealthCheckMeter
    {
        private static readonly Meter Meter = new Meter(MeterName);
        private static readonly ObservableGauge<int> HealthCheckGauge;

        private static int _status;
        private static string _reportName = "";
        
        static HealthCheckMeter()
        {
            HealthCheckGauge =
                Meter.CreateObservableGauge<int>("healthcheck.status", 
                                                 () => new Measurement<int>(_status, new KeyValuePair<string, object>("report", _reportName)),
                                                 "points", "Health check status");
        }
        
        public static string MeterName => "leaderboard.healthcheck";

        public static void HealthCheck(string reportEntryKey, HealthStatus healthStatus)
        {
            _reportName = reportEntryKey;
            _status = (int)healthStatus;
        }
    }
}