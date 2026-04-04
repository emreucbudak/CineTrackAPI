using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;

namespace CineTrack.API.Controllers;

[ApiController]
public abstract class BaseApiController : ControllerBase
{
    protected Guid GetUserId()
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (claim is null || !Guid.TryParse(claim, out var userId))
            throw new UnauthorizedAccessException("User ID claim not found or invalid.");
        return userId;
    }
}
