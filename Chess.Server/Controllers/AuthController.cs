using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Chess.Server.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    /// <summary>Returns the current user's profile extracted from Azure AD JWT claims.</summary>
    [HttpGet("me")]
    [Authorize]
    public IActionResult GetProfile()
    {
        var userId      = User.FindFirst("oid")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var displayName = User.FindFirst("name")?.Value ?? User.FindFirst(ClaimTypes.Name)?.Value;
        var email       = User.FindFirst("preferred_username")?.Value ?? User.FindFirst(ClaimTypes.Email)?.Value;

        if (userId is null) return Unauthorized();

        return Ok(new { userId, displayName, email });
    }

    /// <summary>Health-check endpoint — no auth required.</summary>
    [HttpGet("ping")]
    public IActionResult Ping() => Ok(new { status = "ok", utc = DateTime.UtcNow });
}
