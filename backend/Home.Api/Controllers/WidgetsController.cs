using Home.Api.Data;
using Home.Api.Dtos;
using Home.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Home.Api.Controllers;

[ApiController]
[Route("api/widgets")]
public class WidgetsController(AppDbContext db) : ControllerBase
{
    private static readonly HashSet<string> AllowedTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "clock", "weather", "notes", "search", "countdown"
    };

    [HttpGet]
    public async Task<ActionResult<IEnumerable<WidgetDto>>> List(CancellationToken ct)
    {
        var items = await db.Widgets.AsNoTracking()
            .OrderBy(w => w.GridY).ThenBy(w => w.GridX)
            .ToListAsync(ct);
        return Ok(items.Select(ToDto));
    }

    [Authorize]
    [HttpPost]
    public async Task<ActionResult<WidgetDto>> Create([FromBody] CreateWidgetRequest request, CancellationToken ct)
    {
        if (!AllowedTypes.Contains(request.Type))
            return BadRequest(new { message = "Unsupported widget type" });

        var entity = new Widget
        {
            Type = request.Type.ToLowerInvariant(),
            ConfigJson = string.IsNullOrWhiteSpace(request.ConfigJson) ? "{}" : request.ConfigJson!,
            GridX = request.GridX,
            GridY = request.GridY,
            GridW = Math.Max(1, request.GridW),
            GridH = Math.Max(1, request.GridH)
        };

        db.Widgets.Add(entity);
        await db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(Get), new { id = entity.Id }, ToDto(entity));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<WidgetDto>> Get(Guid id, CancellationToken ct)
    {
        var entity = await db.Widgets.AsNoTracking().FirstOrDefaultAsync(w => w.Id == id, ct);
        return entity is null ? NotFound() : Ok(ToDto(entity));
    }

    [Authorize]
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<WidgetDto>> Update(Guid id, [FromBody] UpdateWidgetRequest request, CancellationToken ct)
    {
        var entity = await db.Widgets.FindAsync([id], ct);
        if (entity is null) return NotFound();
        if (!AllowedTypes.Contains(request.Type))
            return BadRequest(new { message = "Unsupported widget type" });

        entity.Type = request.Type.ToLowerInvariant();
        entity.ConfigJson = request.ConfigJson;
        entity.GridX = request.GridX;
        entity.GridY = request.GridY;
        entity.GridW = Math.Max(1, request.GridW);
        entity.GridH = Math.Max(1, request.GridH);

        await db.SaveChangesAsync(ct);
        return Ok(ToDto(entity));
    }

    [Authorize]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var entity = await db.Widgets.FindAsync([id], ct);
        if (entity is null) return NotFound();
        db.Widgets.Remove(entity);
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    private static WidgetDto ToDto(Widget w) => new(
        w.Id, w.Type, w.ConfigJson, w.GridX, w.GridY, w.GridW, w.GridH);
}
