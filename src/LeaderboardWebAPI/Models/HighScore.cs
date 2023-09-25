namespace LeaderboardWebAPI.Models
{
    public class HighScore
    {
        public string Game { get; set; }
        public string Nickname { get; set; }
        public int Points { get; set; }
    }
}