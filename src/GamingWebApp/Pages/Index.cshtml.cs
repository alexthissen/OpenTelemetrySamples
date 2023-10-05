using System.Diagnostics;
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
                        IEnumerable<HighScore> scores) : PageModel
{
    private readonly IOptionsSnapshot<LeaderboardApiOptions> options = options;

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
            
            activity?.AddEvent(new("HighScoresRetrieved"));
            
            HighScoreMeter.HighScoreRetrieved();
            
            logger.LogInformation("retrieved {Count} high scores", Scores.Count());
        }
            
        catch (HttpRequestException ex)
        {
            logger.LogInformation(ex, "Http request failed");
        }
        catch (TimeoutRejectedException ex)
        {
            logger.LogDebug(ex, "Timeout occurred when retrieving high score list");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unknown exception occurred while retrieving high score list");
            
            activity?.SetStatus(ActivityStatusCode.Error);
            activity?.RecordException(ex);
        }
    }
}