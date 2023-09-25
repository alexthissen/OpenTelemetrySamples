using System.Diagnostics.Tracing;

namespace LeaderboardWebAPI.Infrastructure
{
    [EventSource(Name = "RetroGaming-API")]
    public class RetroGamingEventSource : EventSource
    {
        public static RetroGamingEventSource Log = new RetroGamingEventSource();

        private EventCounter? NewHighScoreEventCounter;

        private RetroGamingEventSource() : base(EventSourceSettings.EtwSelfDescribingEventFormat)
        {
            NewHighScoreEventCounter = new EventCounter("newhighscore", this)
            {
                DisplayName = "New high score",
                DisplayUnits = "points"
            };
        }
        protected override void Dispose(bool disposing)
        {
            NewHighScoreEventCounter?.Dispose();
            NewHighScoreEventCounter = null;

            base.Dispose(disposing);
        }

        [Event(eventId: 1, Version = 0, Level = EventLevel.Informational,
            Keywords = Keywords.NewHighScore, Message = "New high score {0}")]
        public void NewHighScore(int points)
        { 
            WriteEvent(1, points);
            NewHighScoreEventCounter.WriteMetric(points);
        }
    }

    public class Keywords   // This is a bitvector
    {
        public const EventKeywords NewHighScore = (EventKeywords)0x0001;
    }
}
