using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace LeaderboardWebAPI.Metrics
{
    public class ScoreMeter
    {
        private static Histogram<int> highScoreHistogram;

        static ScoreMeter()
        {
            var meter = new Meter(MeterName);
            highScoreHistogram = meter.CreateHistogram<int>("highscore", "points", "New high score");
        }

        public static string MeterName => "LeaderboardWebAPI.Score";

        public static void AddHighScore(int points, string game, string playerName)
        {
            TagList tags = new TagList();
            tags.Add("game", game);
            tags.Add("player_name", playerName);
            highScoreHistogram.Record(points, new("game", game), new("player_name", playerName));
            highScoreHistogram.Record(points, tags);
        }
    }
}