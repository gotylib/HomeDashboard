using Home.Api.Data;
using Home.Api.Dtos;
using Home.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Home.Api.Controllers;

[ApiController]
[Route("api/settings")]
public class SettingsController(AppDbContext db, UploadService uploads) : ControllerBase
{
    [HttpGet("wallpaper")]
    public async Task<ActionResult<WallpaperDto>> GetWallpaper(CancellationToken ct)
    {
        var settings = await db.Settings.AsNoTracking().FirstOrDefaultAsync(ct)
                       ?? new Models.AppSettings();
        return Ok(new WallpaperDto(settings.WallpaperPath, settings.WallpaperType));
    }

    [Authorize]
    [HttpPut("wallpaper")]
    public async Task<ActionResult<WallpaperDto>> SetWallpaper([FromBody] SetWallpaperRequest request, CancellationToken ct)
    {
        var settings = await db.Settings.FirstOrDefaultAsync(ct);
        if (settings is null)
        {
            settings = new Models.AppSettings { Id = 1 };
            db.Settings.Add(settings);
        }

        var previous = settings.WallpaperPath;
        var newPath = string.IsNullOrWhiteSpace(request.Path) ? null : request.Path.Trim();

        settings.WallpaperPath = newPath;
        settings.WallpaperType = string.IsNullOrWhiteSpace(newPath)
            ? "none"
            : (string.IsNullOrWhiteSpace(request.Type)
                ? UploadService.DetectWallpaperType(newPath, null)
                : request.Type);

        await db.SaveChangesAsync(ct);

        if (!string.IsNullOrWhiteSpace(previous)
            && !string.Equals(previous, newPath, StringComparison.OrdinalIgnoreCase))
        {
            uploads.TryDelete(previous);
        }

        return Ok(new WallpaperDto(settings.WallpaperPath, settings.WallpaperType));
    }

    [Authorize]
    [HttpDelete("wallpaper")]
    public async Task<ActionResult<WallpaperDto>> ClearWallpaper(CancellationToken ct)
    {
        var settings = await db.Settings.FirstOrDefaultAsync(ct);
        if (settings is null)
        {
            settings = new Models.AppSettings { Id = 1, WallpaperType = "none" };
            db.Settings.Add(settings);
        }

        var previous = settings.WallpaperPath;
        settings.WallpaperPath = null;
        settings.WallpaperType = "none";
        await db.SaveChangesAsync(ct);

        if (!string.IsNullOrWhiteSpace(previous))
            uploads.TryDelete(previous);

        return Ok(new WallpaperDto(null, "none"));
    }
}
