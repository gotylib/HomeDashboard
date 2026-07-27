using Home.Api.Data;
using Home.Api.Dtos;
using Home.Api.Models;
using Home.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Home.Api.Controllers;

[ApiController]
[Route("api/services")]
public class ServicesController(
    AppDbContext db,
    IHttpClientFactory httpClientFactory,
    UploadService uploads) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ServiceDto>>> List(CancellationToken ct)
    {
        var items = await db.Services
            .AsNoTracking()
            .Include(s => s.HealthStatus)
            .OrderBy(s => s.SortOrder)
            .ToListAsync(ct);
        return Ok(items.Select(ToDto));
    }

    [Authorize]
    [HttpPost]
    public async Task<ActionResult<ServiceDto>> Create([FromBody] CreateServiceRequest request, CancellationToken ct)
    {
        var entity = new ServiceLink
        {
            Title = request.Title.Trim(),
            Url = request.Url.Trim(),
            ImagePath = request.ImagePath,
            HealthUrl = string.IsNullOrWhiteSpace(request.HealthUrl) ? null : request.HealthUrl.Trim(),
            GridX = request.GridX,
            GridY = request.GridY,
            GridW = Math.Max(1, request.GridW),
            GridH = Math.Max(1, request.GridH),
            SortOrder = await db.Services.CountAsync(ct)
        };

        db.Services.Add(entity);
        await db.SaveChangesAsync(ct);
        await RefreshHealthAsync(entity, ct);

        var created = await db.Services.AsNoTracking()
            .Include(s => s.HealthStatus)
            .FirstAsync(s => s.Id == entity.Id, ct);
        return CreatedAtAction(nameof(Get), new { id = entity.Id }, ToDto(created));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ServiceDto>> Get(Guid id, CancellationToken ct)
    {
        var entity = await db.Services
            .AsNoTracking()
            .Include(s => s.HealthStatus)
            .FirstOrDefaultAsync(s => s.Id == id, ct);
        return entity is null ? NotFound() : Ok(ToDto(entity));
    }

    [Authorize]
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ServiceDto>> Update(Guid id, [FromBody] UpdateServiceRequest request, CancellationToken ct)
    {
        var entity = await db.Services.Include(s => s.HealthStatus).FirstOrDefaultAsync(s => s.Id == id, ct);
        if (entity is null) return NotFound();

        var previousImage = entity.ImagePath;
        entity.Title = request.Title.Trim();
        entity.Url = request.Url.Trim();
        entity.ImagePath = request.ImagePath;
        entity.HealthUrl = string.IsNullOrWhiteSpace(request.HealthUrl) ? null : request.HealthUrl.Trim();
        entity.GridX = request.GridX;
        entity.GridY = request.GridY;
        entity.GridW = Math.Max(1, request.GridW);
        entity.GridH = Math.Max(1, request.GridH);
        entity.SortOrder = request.SortOrder;

        await db.SaveChangesAsync(ct);
        await RefreshHealthAsync(entity, ct);

        if (!string.IsNullOrWhiteSpace(previousImage)
            && !string.Equals(previousImage, entity.ImagePath, StringComparison.OrdinalIgnoreCase))
        {
            uploads.TryDelete(previousImage);
        }

        await db.Entry(entity).Reference(s => s.HealthStatus).LoadAsync(ct);
        return Ok(ToDto(entity));
    }

    [Authorize]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var entity = await db.Services.FindAsync([id], ct);
        if (entity is null) return NotFound();
        var image = entity.ImagePath;
        db.Services.Remove(entity);
        await db.SaveChangesAsync(ct);
        if (!string.IsNullOrWhiteSpace(image))
            uploads.TryDelete(image);
        return NoContent();
    }

    [HttpGet("{id:guid}/health")]
    public async Task<ActionResult<object>> Health(Guid id, CancellationToken ct)
    {
        var status = await db.HealthStatuses.AsNoTracking().FirstOrDefaultAsync(h => h.ServiceId == id, ct);
        if (status is null)
            return Ok(new { isUp = (bool?)null, checkedAt = (DateTime?)null });
        return Ok(new { isUp = status.IsUp, checkedAt = status.CheckedAt });
    }

    private async Task RefreshHealthAsync(ServiceLink service, CancellationToken ct)
    {
        var url = string.IsNullOrWhiteSpace(service.HealthUrl) ? service.Url : service.HealthUrl!;
        var client = httpClientFactory.CreateClient("health");
        var isUp = await HealthCheckWorker.ProbeAsync(client, url, ct);

        var status = await db.HealthStatuses.FindAsync([service.Id], ct);
        if (status is null)
        {
            db.HealthStatuses.Add(new HealthStatus
            {
                ServiceId = service.Id,
                IsUp = isUp,
                CheckedAt = DateTime.UtcNow
            });
        }
        else
        {
            status.IsUp = isUp;
            status.CheckedAt = DateTime.UtcNow;
        }

        await db.SaveChangesAsync(ct);
    }

    private static ServiceDto ToDto(ServiceLink s) => new(
        s.Id,
        s.Title,
        s.Url,
        s.ImagePath,
        s.HealthUrl,
        s.GridX,
        s.GridY,
        s.GridW,
        s.GridH,
        s.SortOrder,
        s.HealthStatus?.IsUp,
        s.HealthStatus?.CheckedAt);
}
