using LeaderboardWebAPI.Infrastructure;
using LeaderboardWebAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using LeaderboardWebAPI.Metrics;
using Microsoft.Extensions.Logging;

namespace LeaderboardWebAPI.Controllers
{
    [ApiController]
    [Route("api/v1.0/[controller]")]
    [Produces("application/xml", "application/json")]
    public class ScoresController : ControllerBase
    {
        private readonly LeaderboardContext context;
        private readonly ILogger<ScoresController> logger;
        private readonly HighScoreMeter highScoreMeter;

        public ScoresController(LeaderboardContext context, HighScoreMeter highScoreMeter, ILogger<ScoresController> logger)
        {
            this.context = context;
            this.highScoreMeter = highScoreMeter;
            this.logger = logger;
        }

        [HttpGet("{game}")]
        public async Task<IEnumerable<Score>> Get(string game)
        {
            logger.LogInformation("Retrieving scores for {Game}", game);
            var scores = context.Scores.Where(s => s.Game == game).Include(s => s.Gamer);
            return await scores.ToListAsync().ConfigureAwait(false);
        }

        [HttpPost("{nickname}/{game}")]
        public async Task<IActionResult> PostScore(string nickname, string game, [FromBody] int points)
        {
            using (var activity = Diagnostics.LeaderboardActivitySource.StartActivity())
            {
                activity?.SetTag("score.nickname", nickname);
                activity?.SetTag("score.game", game);
                activity?.SetTag("score.points", points);

                // Lookup gamer based on nickname
                Gamer gamer = await context.Gamers
                    .FirstOrDefaultAsync(g => g.Nickname.ToLower() == nickname.ToLower())
                    .ConfigureAwait(false);

                if (gamer is null)
                {
                    activity?.AddEvent(new ActivityEvent("Gamer not found", DateTimeOffset.Now,
                        new ActivityTagsCollection(new List<KeyValuePair<string, object>>()
                        {
                            new("gamer.nickname", nickname)
                        })));
                    return NotFound();
                }

                activity?.AddEvent(new ActivityEvent("Gamer found", DateTimeOffset.Now,
                    new ActivityTagsCollection(new List<KeyValuePair<string, object>>()
                    {
                        new("gamer.nickname", gamer.Nickname),
                        new("gamer.id", gamer.Id)
                    })));

                // Find the highest score for game
                var score = await context.Scores
                     .Where(s => s.Game == game)
                     .OrderByDescending(s => s.Points)
                     .FirstOrDefaultAsync()
                     .ConfigureAwait(false);

                if (score is null)
                {
                    score = new Score { Gamer = gamer, Points = points, Game = game };
                    await context.Scores.AddAsync(score);
                    
                    activity?.AddEvent(new ActivityEvent("added_score", DateTimeOffset.Now,
                        new ActivityTagsCollection
                        {
                            new("gamer.name", gamer.Nickname),
                            new("score.points", points),
                            new("game.name", game)
                        }));
                }
                else
                {
                    highScoreMeter.AddScore(points, score.Game);
                    if (score.Points > points)
                        return Ok();

                    score.Points = points;
                }

                logger.LogInformation("New high score {Points}", points);
                
                highScoreMeter.NewHighScore(score.Game);
                
                activity?.AddEvent(new ActivityEvent("NewHighScore", DateTimeOffset.Now, new ActivityTagsCollection
                {
                    new("score", points),
                    new("game", game)
                }));

                await context.SaveChangesAsync().ConfigureAwait(false);
                return Ok();
            }
        }
    }
}