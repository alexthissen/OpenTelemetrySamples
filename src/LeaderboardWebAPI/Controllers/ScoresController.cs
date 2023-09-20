using LeaderboardWebAPI.Infrastructure;
using LeaderboardWebAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace LeaderboardWebAPI.Controllers
{
    [ApiController]
    [Route("api/v1.0/[controller]")]
    [Produces("application/xml", "application/json")]
    public class ScoresController : ControllerBase
    {
        private readonly LeaderboardContext context;
        
        public ScoresController(LeaderboardContext context)
        {
            this.context = context;
        }

        [HttpGet("{game}")]
        public async Task<IEnumerable<Score>> Get(string game)
        {
            var scores = context.Scores.Where(s => s.Game == game).Include(s => s.Gamer);
            return await scores.ToListAsync().ConfigureAwait(false);
        }

        [HttpPost("{nickname}/{game}")]
        public async Task PostScore(string nickname, string game, [FromBody] int points)
        {
            Activity activity = new Activity("GotNewHighScore");
            activity.SetStartTime(DateTime.Now);
            activity.AddTag("Gamer", nickname);

            //using (var dependency = client.StartOperation<DependencyTelemetry>(activity))
            {
                // Lookup gamer based on nickname
                Gamer gamer = await context.Gamers
                  .FirstOrDefaultAsync(g => g.Nickname.ToLower() == nickname.ToLower())
                  .ConfigureAwait(false);

                if (gamer == null) return;

                // Find highest score for game
                var score = await context.Scores
                      .Where(s => s.Game == game && s.Gamer == gamer)
                      .OrderByDescending(s => s.Points)
                      .FirstOrDefaultAsync()
                      .ConfigureAwait(false);

                if (score == null)
                {
                    score = new Score() { Gamer = gamer, Points = points, Game = game };
                    await context.Scores.AddAsync(score);
                }
                else
                {
                    if (score.Points > points) return;
                    score.Points = points;
                }

                // Application Insights tracing and metrics
                //client.TrackEvent("NewHighScore");
                //client.GetMetric("HighScore").TrackValue(points);

                // .NET Diagnostics metrics
                RetroGamingEventSource.Log.NewHighScore(points);

                await context.SaveChangesAsync().ConfigureAwait(false);
            }
        }
    }
}
