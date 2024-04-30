using System.Collections.Generic;
using System.Diagnostics.Metrics;

namespace LeaderboardWebAPI.Metrics
{
    public class HighScoreMeter
    {
        private readonly Counter<int> _highScoreCounter;
        private readonly Histogram<int> _scoreHistogram;

        public HighScoreMeter(IMeterFactory meterFactory)
        {
            var meter = meterFactory.Create(MeterName);
            _highScoreCounter = meter.CreateCounter<int>("high_score.count", "points", "New high score");
            _scoreHistogram = meter.CreateHistogram<int>("score", "points", "New score");
        }
        public static string MeterName => "leaderboard.high_score";

        public void NewHighScore(string game) =>
            _highScoreCounter.Add(1, new[] { new KeyValuePair<string, object>("game", game) });

        public void AddScore(int score, string game) =>
            _scoreHistogram.Record(score, new[] { new KeyValuePair<string, object>("game", game) });
    }
}