using GamingWebApp.Proxy;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;
using Polly.Timeout;

namespace GamingWebApp.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> logger;
        private readonly IOptionsSnapshot<LeaderboardApiOptions> options;
        private readonly ILeaderboardClient proxy;

        public IndexModel(IOptionsSnapshot<LeaderboardApiOptions> options,
            ILeaderboardClient proxy, ILoggerFactory loggerFactory)
        {
            this.logger = loggerFactory.CreateLogger<IndexModel>();
            this.options = options;
            this.proxy = proxy;
        }

        public IEnumerable<HighScore> Scores { get; private set; }

        public async Task OnGetAsync()
        {
            Scores = new List<HighScore>();
            try
            {
                //using (var operation = telemetryClient.StartOperation<RequestTelemetry>("LeaderboardWebAPICall"))
                {
                    try
                    {
                        // Using injected typed HTTP client instead of locally created proxy
                        int limit;

                        Scores = await proxy.GetHighScores(
                            Int32.TryParse(Request.Query["limit"], out limit) ? limit : 10
                        ).ConfigureAwait(false);
                    }
                    catch
                    {
                        //operation.Telemetry.Success = false;
                        throw;
                    }
                }
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
            }
        }
    }
}
