using Microsoft.EntityFrameworkCore;
using TenderRadar.Domain;

namespace TenderRadar.Infrastructure.Persistence;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Tender> Tenders => Set<Tender>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var tender = modelBuilder.Entity<Tender>();

        tender.ToTable("tenders");
        tender.HasKey(t => new { t.Source, t.PublicationNumber });

        tender.Property(t => t.Source).HasMaxLength(32);
        tender.Property(t => t.PublicationNumber).HasMaxLength(64);
        tender.Property(t => t.Title).HasMaxLength(2000);
        tender.Property(t => t.ShortTitle).HasMaxLength(2000);
        tender.Property(t => t.BuyerName).HasMaxLength(500);
        tender.Property(t => t.Country).HasMaxLength(8);
        tender.Property(t => t.Currency).HasMaxLength(8);
        tender.Property(t => t.Url).HasMaxLength(500);
        tender.Property(t => t.EstimatedValue).HasPrecision(18, 2);

        tender.Property(t => t.CpvCodes).HasColumnType("text[]");
        tender.Property(t => t.MatchedKeywords).HasColumnType("text[]");

        tender.HasIndex(t => t.ExportedAt);
        tender.HasIndex(t => t.PublicationDate);
        tender.HasIndex(t => t.Score);
    }
}