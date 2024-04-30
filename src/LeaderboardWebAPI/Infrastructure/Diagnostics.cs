using System.Diagnostics;

namespace LeaderboardWebAPI.Infrastructure
{
    public static class Diagnostics
    {
        public static readonly ActivitySource LeaderboardActivitySource = new("leaderboard-api");
    }
}