using System.Security.Claims;

namespace mPass_server.Utils;

public class ControllerHelper
{
    /// <summary>
    /// Get email from claims (jwt token)
    /// </summary>
    public static string? GetEmailFromClaims(ClaimsPrincipal claimsPrincipal) =>
        claimsPrincipal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
}