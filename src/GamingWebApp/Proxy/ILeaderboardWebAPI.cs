using Refit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GamingWebApp.Proxy
{
    [Headers("User-Agent: Leaderboard WebAPI Client 1.0")]
    public interface ILeaderboardClient
    {
        [Get("/api/v1.0/leaderboard")]
        Task<IEnumerable<HighScore>> GetHighScores(int limit = 10);
    }

    public record HighScore
    {
        public string Game { get; init; }
        public string Nickname { get; init; }
        public int Points { get; init; }
    }
}
