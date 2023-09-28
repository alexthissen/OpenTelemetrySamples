using System.Diagnostics.Metrics;

namespace LeaderboardWebAPI.Metrics
{
    public static class HighScoreMeter
    {
        private static readonly Counter<int> HighScoreCounter;
        private static readonly Histogram<int> ScoreHistogram;

        static HighScoreMeter()
        {
            var meter = new Meter(MeterName);
           HighScoreCounter = meter.CreateCounter<int>("highscore.count", "points", "New high score");
           ScoreHistogram = meter.CreateHistogram<int>("score", "points", "score");
        }
        
        public static string MeterName => "leaderboard.highscore";

        public static void AddHighScore()
        {
            HighScoreCounter.Add(1);
        }
        
        public static void AddScore(int score)
        {
            ScoreHistogram.Record(score);
        }
    }
}