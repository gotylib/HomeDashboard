using Home.Api.Data;
using Home.Api.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Home.Api.Controllers;

[ApiController]
[Route("api/layout")]
[Authorize]
public class LayoutController(AppDbContext db) : ControllerBase
{
    [HttpPut]
    public async Task<IActionResult> Save([FromBody] SaveLayoutRequest request, CancellationToken ct)
    {
        var serviceIds = request.Items.Where(i => i.Kind == "service").Select(i => i.Id).ToHashSet();
        var widgetIds = request.Items.Where(i => i.Kind == "widget").Select(i => i.Id).ToHashSet();
        var folderIds = request.Items.Where(i => i.Kind == "folder").Select(i => i.Id).ToHashSet();

        var services = await db.Services.Where(s => serviceIds.Contains(s.Id)).ToListAsync(ct);
        var widgets = await db.Widgets.Where(w => widgetIds.Contains(w.Id)).ToListAsync(ct);
        var folders = await db.Folders.Where(f => folderIds.Contains(f.Id)).ToListAsync(ct);

        foreach (var item in request.Items)
        {
            if (item.Kind == "service")
            {
                var s = services.FirstOrDefault(x => x.Id == item.Id);
                if (s is null) continue;
                s.GridX = item.GridX;
                s.GridY = item.GridY;
                s.GridW = Math.Max(1, item.GridW);
                s.GridH = Math.Max(1, item.GridH);
            }
            else if (item.Kind == "widget")
            {
                var w = widgets.FirstOrDefault(x => x.Id == item.Id);
                if (w is null) continue;
                w.GridX = item.GridX;
                w.GridY = item.GridY;
                w.GridW = Math.Max(1, item.GridW);
                w.GridH = Math.Max(1, item.GridH);
            }
            else if (item.Kind == "folder")
            {
                var f = folders.FirstOrDefault(x => x.Id == item.Id);
                if (f is null) continue;
                f.GridX = item.GridX;
                f.GridY = item.GridY;
                f.GridW = Math.Max(1, item.GridW);
                f.GridH = Math.Max(1, item.GridH);
            }
        }

        await db.SaveChangesAsync(ct);
        return NoContent();
    }
}
