using System.Diagnostics.Metrics;

namespace GamingWebApp;

public class HighScoreMeter
{
    private static Counter<int>? _highScoreCounter;
    private static Counter<int>? _highScoreExceptionsCounter;

    public HighScoreMeter()
    {
        var meter = new Meter(Name);
        _highScoreCounter = meter.CreateCounter<int>("HighScoreRetrieved");
        _highScoreExceptionsCounter = meter.CreateCounter<int>("HighScoreRetrievedExceptions");
    }

    public static string Name => "GamingWebApp.HighScoreMeter";
    public static void HighScoreRetrieved() => _highScoreCounter?.Add(1);
    public static void HighScoreRetrievedException () => _highScoreExceptionsCounter?.Add(1);
}