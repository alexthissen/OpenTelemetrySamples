using System.Diagnostics;
using GamingWebApp.Proxy;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;
using OpenTelemetry.Trace;
using Polly.Timeout;

namespace GamingWebApp.Pages;

public class IndexModel : PageModel
{
    private readonly ILogger<IndexModel> logger;
    private readonly IOptionsSnapshot<LeaderboardApiOptions> options;
    private readonly ILeaderboardClient proxy;

    public IndexModel(IOptionsSnapshot<LeaderboardApiOptions> options,
                      ILeaderboardClient proxy, ILogger<IndexModel> logger)
    {
        this.logger = logger;
        this.options = options;
        this.proxy = proxy;
    }

    public IEnumerable<HighScore> Scores { get; private set; }

    public async Task OnGetAsync()
    {
        using var activity = Diagnostics.GamingWebActivitySource.StartActivity("GetHighScores");
        Scores = new List<HighScore>();
        try
        {
            var hasLimit = int.TryParse(Request.Query["limit"], out var limit);
            logger.LogInformation("Retrieving high score list with limit of {Limit}", limit);
            // Using injected typed HTTP client instead of locally created proxy
            Scores = await proxy.GetHighScores(hasLimit ? limit : 10).ConfigureAwait(false);
            activity?.AddEvent(new("HighScoresRetrieved"));
            HighScoreMeter.HighScoreRetrieved();
            logger.LogInformation("retrieved {Count} high scores", Scores.Count());
        }
            
        catch (HttpRequestException ex)
        {
            logger.LogInformation(ex, "Http request failed.");
        }
        catch (TimeoutRejectedException ex)
        {
            logger.LogDebug(ex, "Timeout occurred when retrieving high score list.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unknown exception occurred while retrieving high score list");
            HighScoreMeter.HighScoreRetrievedException();
            activity?.SetStatus(ActivityStatusCode.Error);
            activity?.RecordException(ex);
        }
    }
}