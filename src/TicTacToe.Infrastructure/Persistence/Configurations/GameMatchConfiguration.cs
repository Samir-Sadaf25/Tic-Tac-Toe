using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TicTacToe.Domain.Entities;

namespace TicTacToe.Infrastructure.Persistence.Configurations;

public class GameMatchConfiguration : IEntityTypeConfiguration<GameMatch>
{
    public void Configure(EntityTypeBuilder<GameMatch> builder)
    {
        builder.ToTable("GameMatches");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.BoardState)
            .HasMaxLength(9)
            .IsFixedLength()
            .IsRequired();

        builder.Property(m => m.Status)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(m => m.CurrentTurn)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(m => m.Outcome)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(m => m.Revision)
            .IsRequired();

        builder.Property(m => m.CreatedAtUtc)
            .IsRequired();

        builder.Property(m => m.CompletedAtUtc);

        // EF Core shadow rowversion for optimistic concurrency
        builder.Property<byte[]>("RowVersion")
            .IsRowVersion();

        builder.HasMany(m => m.Players)
            .WithOne()
            .HasForeignKey(p => p.MatchId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(m => m.Players)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
