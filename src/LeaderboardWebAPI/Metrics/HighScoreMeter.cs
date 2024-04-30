using System.Collections.Generic;
using System.Diagnostics.Metrics;

namespace LeaderboardWebAPI.Metrics
{
    public  class HighScoreMeter
    {
        private readonly Counter<int> highScoreCounter;
        private readonly Histogram<int> scoreHistogram;

        public HighScoreMeter(IMeterFactory meterFactory)
        {
            var meter = meterFactory.Create(MeterName);
            highScoreCounter = meter.CreateCounter<int>("high_score.count", "points", "New high score");
            scoreHistogram = meter.CreateHistogram<int>("score", "points", "New score");
        }

        public static string MeterName => "leaderboard.high_score";

        public void NewHighScore(string game) =>
            highScoreCounter.Add(1, new[] { new KeyValuePair<string, object>("game", game) });

        public void AddScore(int score, string game) =>
            scoreHistogram.Record(score, new[] { new KeyValuePair<string, object>("game", game) });
    }
}