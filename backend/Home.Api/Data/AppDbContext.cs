using Home.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Home.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<AppSettings> Settings => Set<AppSettings>();
    public DbSet<ServiceLink> Services => Set<ServiceLink>();
    public DbSet<Folder> Folders => Set<Folder>();
    public DbSet<Widget> Widgets => Set<Widget>();
    public DbSet<HealthStatus> HealthStatuses => Set<HealthStatus>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(e =>
        {
            e.HasIndex(x => x.Username).IsUnique();
            e.Property(x => x.Username).HasMaxLength(100);
        });

        modelBuilder.Entity<AppSettings>(e =>
        {
            e.Property(x => x.WallpaperContentType).HasMaxLength(200);
            e.HasData(new AppSettings { Id = 1, WallpaperType = "none" });
        });

        modelBuilder.Entity<Folder>(e =>
        {
            e.Property(x => x.Title).HasMaxLength(200);
            e.Property(x => x.ImagePath).HasMaxLength(500);
        });

        modelBuilder.Entity<ServiceLink>(e =>
        {
            e.Property(x => x.Title).HasMaxLength(200);
            e.Property(x => x.Url).HasMaxLength(2000);
            e.Property(x => x.HealthUrl).HasMaxLength(2000);
            e.Property(x => x.ImagePath).HasMaxLength(500);
            e.HasOne(x => x.Folder)
                .WithMany(x => x.Services)
                .HasForeignKey(x => x.FolderId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<Widget>(e =>
        {
            e.Property(x => x.Type).HasMaxLength(50);
            e.Property(x => x.ConfigJson).HasMaxLength(4000);
        });

        modelBuilder.Entity<HealthStatus>(e =>
        {
            e.HasKey(x => x.ServiceId);
            e.HasOne(x => x.Service)
                .WithOne(x => x.HealthStatus)
                .HasForeignKey<HealthStatus>(x => x.ServiceId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
