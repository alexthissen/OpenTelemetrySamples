using System.Diagnostics.Metrics;

namespace LeaderboardWebAPI.Metrics
{
    public class LeaderboardMeter
    {
        private static Counter<int> _highScoreRetrievedCounter;
        private static Counter<int> _exceptionCounter;
        private static readonly Meter Meter = new Meter(MeterName);
        public LeaderboardMeter()
        {
            _highScoreRetrievedCounter = Meter.CreateCounter<int>("highScoreRetrieved", "points", "Retrieved high scores");
            _exceptionCounter = Meter.CreateCounter<int>("exceptionsOccured");
        }

        public static string MeterName => "LeaderboardWebAPI.Score";

        public static void ScoreRetrieved() => _highScoreRetrievedCounter.Add(1);

        public static void ExceptionOccured() => _exceptionCounter.Add(1);
    }
}