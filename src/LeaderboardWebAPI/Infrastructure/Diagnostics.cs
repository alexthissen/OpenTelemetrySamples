using System.Diagnostics;

namespace LeaderboardWebAPI.Infrastructure
{
    public class Diagnostics
    {
        public static readonly ActivitySource LeaderboardActivitySource = new("leaderboard-api");
    }
}