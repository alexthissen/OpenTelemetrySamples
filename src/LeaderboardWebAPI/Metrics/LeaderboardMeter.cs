using System.Diagnostics.Metrics;

namespace LeaderboardWebAPI.Metrics
{
    public class LeaderboardMeter
    {
        private static Counter<int> _highScoreRetrievedCounter;
        private static readonly Meter Meter = new Meter(MeterName);
        public LeaderboardMeter()
        {
            _highScoreRetrievedCounter = Meter.CreateCounter<int>("highscore.retrieved", "points", "Retrieved high scores");
            Meter.CreateCounter<int>("exceptionsOccured");
        }

        public static string MeterName => "leaderboard.score";

        public static void ScoreRetrieved() => _highScoreRetrievedCounter.Add(1);
    }
}