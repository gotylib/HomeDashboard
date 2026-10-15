using Home.Api.Dtos;
using Home.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace Home.Api.Controllers;

[ApiController]
[Route("api/weather")]
public class WeatherController(WeatherService weather) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<WeatherResponse>> Get(
        [FromQuery] string? city,
        [FromQuery] double? lat,
        [FromQuery] double? lon,
        CancellationToken ct)
    {
        try
        {
            var result = await weather.GetAsync(city, lat, lon, ct);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (HttpRequestException ex)
        {
            return StatusCode(StatusCodes.Status502BadGateway, new { message = ex.Message });
        }
    }
}
