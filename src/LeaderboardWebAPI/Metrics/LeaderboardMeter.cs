using System.Diagnostics.Metrics;

namespace LeaderboardWebAPI.Metrics
{
    public class LeaderboardMeter
    {
        private static Counter<int> _highScoreRetrievedCounter;
        private static readonly Meter Meter = new Meter(MeterName, "1.0-rc1");
        
        static LeaderboardMeter()
        {
            _highScoreRetrievedCounter = Meter.CreateCounter<int>("highScoreRetrieved", "points", "Retrieved high scores");
        }

        public static string MeterName => "LeaderboardWebAPI.Score";

        public static void ScoreRetrieved() => _highScoreRetrievedCounter.Add(1);

    }
}