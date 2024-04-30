using System.Diagnostics.Metrics;

namespace GamingWebApp;

public class HighScoreMeter
{
    private readonly Counter<int>? highScoreCounter;
    
    public HighScoreMeter(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create(Name);
        highScoreCounter = meter.CreateCounter<int>("high_score.retrieved.count", "points", "Retrieved high scores");
    }

    public static string Name => "gaming_webapp.high_score";
    public void HighScoreRetrieved() => highScoreCounter?.Add(1);
}