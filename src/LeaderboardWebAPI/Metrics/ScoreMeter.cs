using System.Diagnostics.Metrics;

namespace LeaderboardWebAPI.Metrics
{
    public class ScoreMeter
    {
        private static Counter<int> _highScoreCounter;

        public ScoreMeter()
        {
            var meter = new Meter(MeterName);
           _highScoreCounter = meter.CreateCounter<int>("highscore", "points", "New high score");
           
        }
        
        public static string MeterName => "LeaderboardWebAPI.Score";

        public static void AddHighScore()
        {
            _highScoreCounter.Add(1);
        }
    }
}