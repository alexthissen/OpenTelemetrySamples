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
        private readonly LeaderboardContext _context;
        private readonly ILogger<ScoresController> _logger;

        public ScoresController(LeaderboardContext context, ILogger<ScoresController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet("{game}")]
        public async Task<IEnumerable<Score>> Get(string game)
        {
            _logger.LogInformation("Retrieving scores for {Game}", game);
            var scores = _context.Scores.Where(s => s.Game == game).Include(s => s.Gamer);
            return await scores.ToListAsync().ConfigureAwait(false);
        }

        [HttpPost("{nickname}/{game}")]
        public async Task<IActionResult> PostScore(string nickname, string game, [FromBody] int points)
        {
            using (var activity = Diagnostics.LeaderboardActivitySource.StartActivity("PostScore"))
            {

                _logger.LogInformation("adding score {Score} for {Nickname} in {Game}", points, nickname, game);

                activity?.SetTag("score.nickname", nickname);
                activity?.SetTag("score.game", game);
                activity?.SetTag("score.points", points);

                // Lookup gamer based on nickname
                Gamer gamer = await _context.Gamers
                   .FirstOrDefaultAsync(g => g.Nickname.ToLower() == nickname.ToLower())
                   .ConfigureAwait(false);


                if (gamer == null)
                {
                    _logger.LogInformation("Gamer {Nickname} not found", nickname);
                    activity?.AddEvent(new ActivityEvent("Gamer not found", DateTimeOffset.Now,
                                                         new ActivityTagsCollection(new List<KeyValuePair<string,
                                                             object>>()
                                                         {
                                                             new("gamer.nickname", nickname)
                                                         })));
                    
                    return NotFound();
                }

                activity?.AddEvent(new ActivityEvent("Gamer found", DateTimeOffset.Now,
                                                     new ActivityTagsCollection(new List<KeyValuePair<string, object>>()
                                                     {
                                                         new("gamer.nickname", gamer.Nickname)
                                                     })));

                // Find highest score for game
                var score = await _context.Scores
                   .Where(s => s.Game == game && s.Gamer == gamer)
                   .OrderByDescending(s => s.Points)
                   .FirstOrDefaultAsync()
                   .ConfigureAwait(false);

                if (score == null)
                {
                    _logger.LogInformation("Added score for {@Gamer} with {Points} for game {Game}", gamer, points,
                                           game);
                    score = new Score { Gamer = gamer, Points = points, Game = game };
                    await _context.Scores.AddAsync(score);
                    activity?.AddEvent(new ActivityEvent("AddedScore", DateTimeOffset.Now, new ActivityTagsCollection
                    {
                        new("score", points)
                    }));
                }
                else
                {
                    if (score.Points > points) return Ok();
                    score.Points = points;

                }

                // Application Insights tracing and metrics
                //client.TrackEvent("NewHighScore");
                //client.GetMetric("HighScore").TrackValue(points);

                // .NET Diagnostics metrics
                // RetroGamingEventSource.Log.NewHighScore(points);

                _logger.LogInformation("New high score {Points}", points);
                ScoreMeter.AddHighScore();
                activity?.AddEvent(new ActivityEvent("NewHighScore", DateTimeOffset.Now, new ActivityTagsCollection()
                {
                    new("score", points)
                }));

                await _context.SaveChangesAsync().ConfigureAwait(false);
                // activity?.Stop();
                return Ok();

            }
        }
    }
}