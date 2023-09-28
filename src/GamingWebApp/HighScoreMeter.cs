using System.Diagnostics.Metrics;

namespace GamingWebApp;

public class HighScoreMeter
{
    private static Counter<int>? _highScoreCounter;

    public HighScoreMeter()
    {
        var meter = new Meter(Name);
        _highScoreCounter = meter.CreateCounter<int>("highscore.retrieved.count", "points", "Retrieved high scores");
    }

    public static string Name => "gamingwebapp.highscore";
    public static void HighScoreRetrieved() => _highScoreCounter?.Add(1);
}