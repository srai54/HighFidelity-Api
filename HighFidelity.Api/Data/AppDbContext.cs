using Microsoft.EntityFrameworkCore;
using HighFidelity.Api.Models;

namespace HighFidelity.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<DashboardCard> DashboardCards => Set<DashboardCard>();
    public DbSet<RevenueCard> RevenueCards => Set<RevenueCard>();
    public DbSet<Activity> Activities => Set<Activity>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<TrafficSource> TrafficSources => Set<TrafficSource>();
    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DashboardCard>(entity =>
        {
            entity.ToTable("DashboardCards", "dbo");
            entity.HasKey(e => e.Id).IsClustered();
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.Title).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Amount).HasColumnType("decimal(18,2)").IsRequired();
            entity.Property(e => e.AmountDisplay).HasMaxLength(50).IsRequired();
            entity.Property(e => e.Icon).HasMaxLength(4).IsRequired();
            entity.Property(e => e.ThemeColorHex).HasMaxLength(7).IsRequired();
        });

        modelBuilder.Entity<RevenueCard>(entity =>
        {
            entity.ToTable("RevenueCards", "dbo");
            entity.HasKey(e => e.Id).IsClustered();
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.Title).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Value).HasMaxLength(50).IsRequired();
            entity.Property(e => e.Subtitle).HasMaxLength(100).HasDefaultValue(string.Empty);
            entity.Property(e => e.ChartType).HasMaxLength(10).IsRequired();
            entity.Property(e => e.BackgroundHex).HasMaxLength(7).IsRequired();
            entity.Property(e => e.AccentHex).HasMaxLength(7).IsRequired();
        });

        modelBuilder.Entity<Activity>(entity =>
        {
            entity.ToTable("Activities", "dbo");
            entity.HasKey(e => e.Id).IsClustered();
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.Title).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Actor).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Action).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Time).HasMaxLength(50).IsRequired();
            entity.Property(e => e.Icon).HasMaxLength(4).IsRequired();
            entity.Property(e => e.IconColorHex).HasMaxLength(7).IsRequired();
        });

        modelBuilder.Entity<Order>(entity =>
        {
            entity.ToTable("Orders", "dbo");
            entity.HasKey(e => e.Id).IsClustered();
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.Invoice).IsRequired();
            entity.Property(e => e.Customer).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Country).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Price).HasColumnType("decimal(18,2)").IsRequired();
            entity.Property(e => e.Status).HasMaxLength(20).IsRequired();
        });

        modelBuilder.Entity<TrafficSource>(entity =>
        {
            entity.ToTable("TrafficSources", "dbo");
            entity.HasKey(e => e.Id).IsClustered();
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.Source).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Percentage).IsRequired();
            entity.Property(e => e.SegmentColorHex).HasMaxLength(7).IsRequired();
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("Users", "dbo");
            entity.HasKey(e => e.Id).IsClustered();
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.Username).HasMaxLength(50).IsRequired();
            entity.HasIndex(e => e.Username).IsUnique();
            entity.Property(e => e.PasswordHash).HasMaxLength(200).IsRequired();
            entity.Property(e => e.CreatedAtUtc).IsRequired();
        });
    }
}
