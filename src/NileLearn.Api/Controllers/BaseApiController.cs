using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;

namespace NileLearn.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public abstract class BaseApiController : ControllerBase
{
    protected Guid CurrentUserId
    {
        get
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier)
                            ?? User.FindFirstValue("sub");

            return Guid.TryParse(userIdStr, out var id) ? id : Guid.Empty;
        }
    }

    protected string ClientIpAddress =>
        HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";

    protected string ClientUserAgent =>
        Request.Headers["User-Agent"].ToString() ?? "Unknown";
}
