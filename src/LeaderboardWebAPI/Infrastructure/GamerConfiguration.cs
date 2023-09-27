using LeaderboardWebAPI.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;

namespace LeaderboardWebAPI.Infrastructure
{
    public class GamerConfiguration : IEntityTypeConfiguration<Gamer>
    {
        public void Configure(EntityTypeBuilder<Gamer> builder)
        {
            builder.ToTable("Gamers");
            builder.HasData(
                new Gamer() { Id = 1, GamerGuid = Guid.NewGuid(), Nickname = "LX360" },
                new Gamer() { Id = 2, GamerGuid = Guid.NewGuid(), Nickname = "LiekGeek" },
                new Gamer() { Id = 3, GamerGuid = Guid.NewGuid(), Nickname = "Techorama" }
            );
        }
    }
}