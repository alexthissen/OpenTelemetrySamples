using LeaderboardWebAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace LeaderboardWebAPI.Infrastructure
{
    public class LeaderboardContext : DbContext
    {
        public LeaderboardContext(DbContextOptions<LeaderboardContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Use entity configuration
            modelBuilder.ApplyConfiguration(new GamerConfiguration());

            // Or configure entity here
            modelBuilder.Entity<Score>()
                .ToTable("Scores")
                .HasData(
                    new Score() { Id = 1, GamerId = 1, Points = 1234, Game = "Pac-man" },
                    new Score() { Id = 2, GamerId = 2, Points = 424242, Game = "Donkey Kong" }
                );
        }

        public DbSet<Gamer> Gamers { get; set; }
        public DbSet<Score> Scores { get; set; }
    }
}
