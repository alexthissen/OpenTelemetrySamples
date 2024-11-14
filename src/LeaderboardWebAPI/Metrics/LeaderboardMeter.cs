using System.Diagnostics.Metrics;

namespace LeaderboardWebAPI.Metrics
{
    public static class LeaderboardMeter
    {
        private static Counter<int> highScoreRetrievedCounter;
        private static readonly Meter Meter = new Meter(MeterName);

        static LeaderboardMeter()
        {
            highScoreRetrievedCounter =
                Meter.CreateCounter<int>("high_score.retrieved", "points", "Retrieved high scores");
            Meter.CreateCounter<int>("exceptionsOccured");
        }

        public static string MeterName => "leaderboard.score";

        public static void ScoreRetrieved() => highScoreRetrievedCounter.Add(1);
    }
}