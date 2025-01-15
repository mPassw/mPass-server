using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.IdentityModel.JsonWebTokens;

namespace mPass_server.Utils;

public static class ControllerHelper
{
    /// <summary>
    /// Get email from claims (jwt token)
    /// </summary>
    public static string? GetEmailFromClaims(ClaimsPrincipal claimsPrincipal) =>
        claimsPrincipal.FindFirst(ClaimTypes.NameIdentifier)?.Value;

    public static string? GetSessionIdFromClaims(ClaimsPrincipal claimsPrincipal) =>
        claimsPrincipal.FindFirst(JwtRegisteredClaimNames.Jti)?.Value;

    /// <summary>
    /// Check if email is valid
    /// </summary>
    public static bool IsValidEmail(string email) =>
        new EmailAddressAttribute().IsValid(email);

    /// <summary>
    /// Check if string is a valid base64 string
    /// </summary>
    public static bool IsBase64String(string base64)
    {
        if (string.IsNullOrEmpty(base64))
            return false;

        try
        {
            _ = Convert.FromBase64String(base64);
            return true;
        }
        catch (FormatException)
        {
            return false;
        }
    }
}