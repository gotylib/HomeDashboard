using Home.Api.Data;
using Home.Api.Models;
using Home.Api.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Home.Api.Services;

public class DbSeeder(
    AppDbContext db,
    IOptions<AdminOptions> adminOptions,
    ILogger<DbSeeder> logger)
{
    public async Task SeedAsync(CancellationToken ct = default)
    {
        await db.Database.MigrateAsync(ct);

        var admin = adminOptions.Value;
        var exists = await db.Users.AnyAsync(u => u.Username == admin.Username, ct);
        if (!exists)
        {
            db.Users.Add(new User
            {
                Username = admin.Username,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(admin.Password)
            });
            await db.SaveChangesAsync(ct);
            logger.LogInformation("Seeded admin user '{Username}'", admin.Username);
        }

        if (!await db.Settings.AnyAsync(ct))
        {
            db.Settings.Add(new AppSettings { Id = 1, WallpaperType = "none" });
            await db.SaveChangesAsync(ct);
        }
    }
}
