using Home.Api.Data;
using Home.Api.Dtos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Home.Api.Controllers;

[ApiController]
[Route("api/dashboard")]
public class DashboardController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<DashboardDto>> Get(CancellationToken ct)
    {
        var settings = await db.Settings.AsNoTracking().FirstOrDefaultAsync(ct)
                       ?? new Models.AppSettings();

        var folders = await db.Folders
            .AsNoTracking()
            .Include(f => f.Services)
            .OrderBy(f => f.SortOrder)
            .ThenBy(f => f.Title)
            .ToListAsync(ct);

        var folderDtos = folders.Select(f => new FolderDto(
            f.Id,
            f.Title,
            f.ImagePath,
            f.GridX,
            f.GridY,
            f.GridW,
            f.GridH,
            f.SortOrder,
            f.Services.Count,
            f.Services
                .OrderBy(s => s.SortOrder)
                .ThenBy(s => s.Title)
                .Take(4)
                .Select(s => new FolderPreviewItemDto(s.Id, s.Title, s.ImagePath))
                .ToList())).ToList();

        var services = await db.Services
            .AsNoTracking()
            .Include(s => s.HealthStatus)
            .OrderBy(s => s.SortOrder)
            .ThenBy(s => s.Title)
            .Select(s => new ServiceDto(
                s.Id,
                s.Title,
                s.Url,
                s.ImagePath,
                s.HealthUrl,
                s.FolderId,
                s.GridX,
                s.GridY,
                s.GridW,
                s.GridH,
                s.SortOrder,
                s.HealthStatus != null ? s.HealthStatus.IsUp : null,
                s.HealthStatus != null ? s.HealthStatus.CheckedAt : null))
            .ToListAsync(ct);

        var widgets = await db.Widgets
            .AsNoTracking()
            .OrderBy(w => w.GridY)
            .ThenBy(w => w.GridX)
            .Select(w => new WidgetDto(
                w.Id,
                w.Type,
                w.ConfigJson,
                w.GridX,
                w.GridY,
                w.GridW,
                w.GridH))
            .ToListAsync(ct);

        return Ok(new DashboardDto(
            new WallpaperDto(settings.WallpaperPath, settings.WallpaperType),
            folderDtos,
            services,
            widgets));
    }
}
