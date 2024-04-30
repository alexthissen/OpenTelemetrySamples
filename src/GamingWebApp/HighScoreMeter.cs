using System.Diagnostics.Metrics;

namespace GamingWebApp;

public class HighScoreMeter
{
    private readonly Counter<int>? _highScoreCounter;
    
    public HighScoreMeter(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create(Name);
        _highScoreCounter = meter.CreateCounter<int>("high_score.retrieved.count", "points", "Retrieved high scores");
    }

    public static string Name => "gaming_webapp.high_score";
    public void HighScoreRetrieved() => _highScoreCounter?.Add(1);
}