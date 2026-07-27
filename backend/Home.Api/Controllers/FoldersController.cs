using Home.Api.Data;
using Home.Api.Dtos;
using Home.Api.Models;
using Home.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Home.Api.Controllers;

[ApiController]
[Route("api/folders")]
public class FoldersController(AppDbContext db, UploadService uploads) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<FolderDto>>> List(CancellationToken ct)
    {
        var folders = await db.Folders
            .AsNoTracking()
            .Include(f => f.Services)
            .OrderBy(f => f.SortOrder)
            .ThenBy(f => f.Title)
            .ToListAsync(ct);
        return Ok(folders.Select(ToDto));
    }

    [Authorize]
    [HttpPost]
    public async Task<ActionResult<FolderDto>> Create([FromBody] CreateFolderRequest request, CancellationToken ct)
    {
        var entity = new Folder
        {
            Title = request.Title.Trim(),
            ImagePath = request.ImagePath,
            GridX = request.GridX,
            GridY = request.GridY,
            GridW = Math.Max(1, request.GridW),
            GridH = Math.Max(1, request.GridH),
            SortOrder = await db.Folders.CountAsync(ct)
        };
        db.Folders.Add(entity);
        await db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(Get), new { id = entity.Id }, ToDto(entity));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<FolderDto>> Get(Guid id, CancellationToken ct)
    {
        var entity = await db.Folders
            .AsNoTracking()
            .Include(f => f.Services)
            .FirstOrDefaultAsync(f => f.Id == id, ct);
        return entity is null ? NotFound() : Ok(ToDto(entity));
    }

    [Authorize]
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<FolderDto>> Update(Guid id, [FromBody] UpdateFolderRequest request, CancellationToken ct)
    {
        var entity = await db.Folders.Include(f => f.Services).FirstOrDefaultAsync(f => f.Id == id, ct);
        if (entity is null) return NotFound();

        var previousImage = entity.ImagePath;
        entity.Title = request.Title.Trim();
        entity.ImagePath = request.ImagePath;
        entity.GridX = request.GridX;
        entity.GridY = request.GridY;
        entity.GridW = Math.Max(1, request.GridW);
        entity.GridH = Math.Max(1, request.GridH);
        entity.SortOrder = request.SortOrder;

        await db.SaveChangesAsync(ct);

        if (!string.IsNullOrWhiteSpace(previousImage)
            && !string.Equals(previousImage, entity.ImagePath, StringComparison.OrdinalIgnoreCase))
        {
            uploads.TryDelete(previousImage);
        }

        return Ok(ToDto(entity));
    }

    [Authorize]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var entity = await db.Folders.Include(f => f.Services).FirstOrDefaultAsync(f => f.Id == id, ct);
        if (entity is null) return NotFound();

        foreach (var service in entity.Services)
            service.FolderId = null;

        var image = entity.ImagePath;
        db.Folders.Remove(entity);
        await db.SaveChangesAsync(ct);
        if (!string.IsNullOrWhiteSpace(image))
            uploads.TryDelete(image);
        return NoContent();
    }

    private static FolderDto ToDto(Folder f) => new(
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
            .ToList());
}
