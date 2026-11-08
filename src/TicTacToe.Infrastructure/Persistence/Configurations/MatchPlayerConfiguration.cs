using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TicTacToe.Domain.Entities;

namespace TicTacToe.Infrastructure.Persistence.Configurations;

public class MatchPlayerConfiguration : IEntityTypeConfiguration<MatchPlayer>
{
    public void Configure(EntityTypeBuilder<MatchPlayer> builder)
    {
        builder.ToTable("MatchPlayers");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.DisplayName)
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(p => p.Symbol)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(p => p.TokenHash)
            .HasMaxLength(128)
            .IsRequired();

        builder.HasIndex(p => new { p.MatchId, p.TokenHash });
    }
}
