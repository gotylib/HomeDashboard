using Home.Api.Dtos;
using Home.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Home.Api.Controllers;

[ApiController]
[Route("api/uploads")]
[Authorize]
public class UploadsController(UploadService uploadService) : ControllerBase
{
    [HttpPost]
    [RequestSizeLimit(100_000_000)]
    public async Task<ActionResult<UploadResponse>> Upload(IFormFile file, CancellationToken ct)
    {
        if (file is null)
            return BadRequest(new { message = "File is required" });

        try
        {
            var (path, contentType) = await uploadService.SaveAsync(file, ct);
            return Ok(new UploadResponse(path, contentType));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete]
    public IActionResult Delete([FromQuery] string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return BadRequest(new { message = "Path is required" });

        uploadService.TryDelete(path);
        return NoContent();
    }
}
