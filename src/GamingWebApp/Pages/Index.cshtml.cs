using System.Diagnostics;
using System.Diagnostics.Metrics;
using GamingWebApp.Proxy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;
using OpenTelemetry.Trace;
using Polly.Timeout;

namespace GamingWebApp.Pages;

public class IndexModel(IOptionsSnapshot<LeaderboardApiOptions> options,
                        ILeaderboardClient proxy, 
                        ILogger<IndexModel> logger, 
                        IEnumerable<HighScore> scores,
                        HighScoreMeter highScoreMeter) : PageModel
{
    public IEnumerable<HighScore> Scores { get; private set; } = scores;

    public async Task OnGetAsync([FromQuery] int limit = 10)
    {
        using var activity = Diagnostics.GamingWebActivitySource.StartActivity("get_high_scores");
        Scores = new List<HighScore>();
        try
        {
            logger.LogInformation("Retrieving high score list with limit of {Limit}", limit);
            // Using injected typed HTTP client instead of locally created proxy
            Scores = await proxy.GetHighScores(limit).ConfigureAwait(false);
            
            activity?.AddEvent(new ActivityEvent("HighScoresRetrieved", DateTimeOffset.Now));
            
            highScoreMeter.HighScoreRetrieved();
            logger.LogInformation("Retrieved {Count} high scores", Scores.Count());
        }
        catch (HttpRequestException ex)
        {
            logger.LogInformation(ex, "Http request failed");
        }
        catch (TimeoutRejectedException ex)
        {
            logger.LogWarning(ex, "Timeout occurred when retrieving high score list");
            
            activity?.RecordException(ex);
            activity?.SetStatus(ActivityStatusCode.Error);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unknown exception occurred while retrieving high score list");
        }
    }
}