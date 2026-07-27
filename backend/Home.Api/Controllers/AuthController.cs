using System.Security.Claims;
using Home.Api.Dtos;
using Home.Api.Options;
using Home.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Home.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(
    AuthService authService,
    IOptions<JwtOptions> jwtOptions) : ControllerBase
{
    public const string CookieName = "home_auth";

    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request, CancellationToken ct)
    {
        var user = await authService.ValidateAsync(request.Username, request.Password, ct);
        if (user is null)
            return Unauthorized(new { message = "Invalid username or password" });

        var token = authService.CreateToken(user);
        var jwt = jwtOptions.Value;

        Response.Cookies.Append(CookieName, token, new CookieOptions
        {
            HttpOnly = true,
            Secure = Request.IsHttps,
            SameSite = SameSiteMode.Lax,
            Expires = DateTimeOffset.UtcNow.AddHours(jwt.ExpireHours),
            Path = "/"
        });

        return Ok(new LoginResponse(user.Username));
    }

    [Authorize]
    [HttpPost("logout")]
    public IActionResult Logout()
    {
        Response.Cookies.Delete(CookieName, new CookieOptions { Path = "/" });
        return NoContent();
    }

    [Authorize]
    [HttpGet("me")]
    public ActionResult<MeResponse> Me()
    {
        var name = User.FindFirstValue(ClaimTypes.Name) ?? User.Identity?.Name ?? "";
        return Ok(new MeResponse(name));
    }
}
