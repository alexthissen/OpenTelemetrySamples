using System.Diagnostics.Metrics;

namespace LeaderboardWebAPI.Metrics
{
    public static class LeaderboardMeter
    {
        private static Counter<int> _highScoreRetrievedCounter;
        private static readonly Meter Meter = new Meter(MeterName);

        static LeaderboardMeter()
        {
            _highScoreRetrievedCounter =
                Meter.CreateCounter<int>("high_score.retrieved", "points", "Retrieved high scores");
            Meter.CreateCounter<int>("exceptionsOccured");
        }

        public static string MeterName => "leaderboard.score";

        public static void ScoreRetrieved() => _highScoreRetrievedCounter.Add(1);
    }
}