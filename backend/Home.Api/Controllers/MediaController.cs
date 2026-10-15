using Home.Api.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Home.Api.Controllers;

[ApiController]
[Route("api/media")]
public class MediaController(AppDbContext db) : ControllerBase
{
    public const string RoutePrefix = "/api/media";

    [HttpGet("{id:guid}")]
    [ResponseCache(Duration = 86400, Location = ResponseCacheLocation.Any)]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct)
    {
        var asset = await db.MediaAssets.AsNoTracking()
            .Where(m => m.Id == id)
            .Select(m => new { m.Data, m.ContentType })
            .FirstOrDefaultAsync(ct);

        if (asset is null || asset.Data.Length == 0)
            return NotFound();

        return File(asset.Data, asset.ContentType);
    }

    public static string PublicPath(Guid id) => $"{RoutePrefix}/{id:D}";

    public static bool TryParseId(string? path, out Guid id)
    {
        id = Guid.Empty;
        if (string.IsNullOrWhiteSpace(path))
            return false;

        var normalized = path.Replace('\\', '/').Trim();
        var marker = $"{RoutePrefix}/";
        var idx = normalized.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
        if (idx < 0)
            return false;

        var rest = normalized[(idx + marker.Length)..];
        var slash = rest.IndexOf('/');
        if (slash >= 0)
            rest = rest[..slash];
        var query = rest.IndexOf('?');
        if (query >= 0)
            rest = rest[..query];

        return Guid.TryParse(rest, out id);
    }
}
