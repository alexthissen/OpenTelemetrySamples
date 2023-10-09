using System;
using LeaderboardWebAPI.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using LeaderboardWebAPI.Metrics;
using LeaderboardWebAPI.Models;
using OpenTelemetry.Trace;

namespace LeaderboardWebAPI.Controllers
{
    [ApiController]
    [Route("api/v1.0/[controller]")]
    [Produces("application/xml", "application/json")]
    public class LeaderboardController : ControllerBase
    {
        private readonly LeaderboardContext context;
        private readonly ILogger<LeaderboardController> logger;

        public LeaderboardController(LeaderboardContext context, ILogger<LeaderboardController> logger)
        {
            this.context = context;
            this.logger = logger;
        }

        // GET api/leaderboard
        /// <summary>
        /// Retrieve a list of leaderboard scores.
        /// </summary>
        /// <returns>List of high scores per game.</returns>
        /// <response code="200">The list was successfully retrieved.</response>
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<HighScore>), 200)]
        public async Task<ActionResult<IEnumerable<HighScore>>> Get(int limit = 10)
        {
            using var activity = Diagnostics.LeaderboardActivitySource.StartActivity("get_scores");

            activity?.SetTag("leaderboard.limit", limit);
            logger?.LogInformation("Retrieving score list with a limit of {SearchLimit}", limit);

            AnalyzeLimit(limit);

            try
            {
                var scores = context.Scores
                    .Select(score => new HighScore()
                     {
                         Game = score.Game,
                         Points = score.Points,
                         Nickname = score.Gamer.Nickname
                     }).Take(limit);

                LeaderboardMeter.ScoreRetrieved();
                
                return Ok(await scores.ToListAsync().ConfigureAwait(false));
            }
            catch (Exception ex)
            {
                logger!.LogError(ex, "Unknown exception occurred while retrieving high score list");

                activity?.RecordException(ex);
                activity?.SetStatus(ActivityStatusCode.Error);
            }

            return BadRequest();
        }

        private void AnalyzeLimit(int limit)
        {
            // This is a demo bug, supposedly "hard" to find
            do
            {
                limit--;
            } while (limit != 0);
        }
    }
}