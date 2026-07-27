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
    public const string WallpaperFileRoute = "/api/settings/wallpaper/file";

    [HttpGet("wallpaper")]
    public async Task<ActionResult<WallpaperDto>> GetWallpaper(CancellationToken ct)
    {
        var settings = await db.Settings.AsNoTracking()
            .Select(s => new
            {
                s.WallpaperPath,
                s.WallpaperType,
                s.WallpaperUpdatedAt,
                HasBlob = s.WallpaperData != null && s.WallpaperData.Length > 0
            })
            .FirstOrDefaultAsync(ct);

        if (settings is null)
            return Ok(new WallpaperDto(null, "none"));

        return Ok(ToDto(
            settings.WallpaperPath,
            settings.WallpaperType,
            settings.WallpaperUpdatedAt,
            settings.HasBlob));
    }

    [HttpGet("wallpaper/file")]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public async Task<IActionResult> GetWallpaperFile(CancellationToken ct)
    {
        var settings = await db.Settings.AsNoTracking().FirstOrDefaultAsync(ct);
        if (settings?.WallpaperData is not { Length: > 0 })
            return NotFound();

        var contentType = string.IsNullOrWhiteSpace(settings.WallpaperContentType)
            ? "application/octet-stream"
            : settings.WallpaperContentType!;

        return File(settings.WallpaperData, contentType);
    }

    [Authorize]
    [HttpPost("wallpaper")]
    [RequestSizeLimit(100_000_000)]
    public async Task<ActionResult<WallpaperDto>> UploadWallpaper(IFormFile file, CancellationToken ct)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new { message = "File is required" });

        var settings = await db.Settings.FirstOrDefaultAsync(ct);
        if (settings is null)
        {
            settings = new Models.AppSettings { Id = 1 };
            db.Settings.Add(settings);
        }

        await using var ms = new MemoryStream();
        await file.CopyToAsync(ms, ct);
        var bytes = ms.ToArray();

        var previousPath = settings.WallpaperPath;
        settings.WallpaperData = bytes;
        settings.WallpaperContentType = string.IsNullOrWhiteSpace(file.ContentType)
            ? "application/octet-stream"
            : file.ContentType;
        settings.WallpaperType = UploadService.DetectWallpaperType(file.FileName, file.ContentType);
        settings.WallpaperUpdatedAt = DateTime.UtcNow;
        settings.WallpaperPath = null;

        await db.SaveChangesAsync(ct);

        if (!string.IsNullOrWhiteSpace(previousPath))
            uploads.TryDelete(previousPath);

        return Ok(ToDto(settings));
    }

    [Authorize]
    [HttpPut("wallpaper")]
    public async Task<ActionResult<WallpaperDto>> SetWallpaper([FromBody] SetWallpaperRequest request, CancellationToken ct)
    {
        // Legacy path-based setter kept for compatibility; prefer POST multipart above.
        var settings = await db.Settings.FirstOrDefaultAsync(ct);
        if (settings is null)
        {
            settings = new Models.AppSettings { Id = 1 };
            db.Settings.Add(settings);
        }

        var previous = settings.WallpaperPath;
        var newPath = string.IsNullOrWhiteSpace(request.Path) ? null : request.Path.Trim();

        settings.WallpaperPath = newPath;
        settings.WallpaperData = null;
        settings.WallpaperContentType = null;
        settings.WallpaperUpdatedAt = DateTime.UtcNow;
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

        return Ok(ToDto(settings));
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
        settings.WallpaperData = null;
        settings.WallpaperContentType = null;
        settings.WallpaperUpdatedAt = null;
        settings.WallpaperType = "none";
        await db.SaveChangesAsync(ct);

        if (!string.IsNullOrWhiteSpace(previous))
            uploads.TryDelete(previous);

        return Ok(new WallpaperDto(null, "none"));
    }

    public static WallpaperDto ToDto(
        string? path,
        string type,
        DateTime? updatedAt,
        bool hasBlob)
    {
        if (hasBlob && type != "none")
        {
            var version = updatedAt?.Ticks ?? 0;
            return new WallpaperDto($"{WallpaperFileRoute}?v={version}", type);
        }

        return new WallpaperDto(path, type);
    }

    public static WallpaperDto ToDto(Models.AppSettings settings) =>
        ToDto(
            settings.WallpaperPath,
            settings.WallpaperType,
            settings.WallpaperUpdatedAt,
            settings.WallpaperData is { Length: > 0 });
}
