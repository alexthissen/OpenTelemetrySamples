using System.Diagnostics.Metrics;

namespace GamingWebApp;

public static class HighScoreMeter
{
    private static readonly Counter<int>? _highScoreCounter;
    
    static HighScoreMeter()
    {
        var meter = new Meter(Name);
        _highScoreCounter = meter.CreateCounter<int>("high_score.retrieved.count", "points", "Retrieved high scores");
    }

    public static string Name => "gaming_webapp.high_score";
    public static void HighScoreRetrieved() => _highScoreCounter?.Add(1);
}