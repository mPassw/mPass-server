using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using mPass_server.Database;
using mPass_server.Utils;

namespace mPass_server.Controllers.User;

[Route("@me")]
[ApiController]
[Authorize]
[Tags("User")]
public class GetMe(DatabaseContext databaseContext) : ControllerBase
{
    /// <summary>
    /// Get current user
    /// </summary>
    [HttpGet]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(GetMeResponse))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<GetMeResponse>> Get()
    {
        var email = ControllerHelper.GetEmailFromClaims(User);
        if (email == null)
            return Unauthorized();

        var userData = await databaseContext.Users.FirstOrDefaultAsync(u => u.Email == email);
        if (userData == null)
            return Unauthorized();

        return new GetMeResponse
        {
            CreatedAt = userData.CreatedAt,
            LastLogin = userData.LastLogin,
            Email = userData.Email,
            EmailVerified = userData.EmailVerified,
            Admin = userData.Admin
        };
    }
}

public class GetMeResponse
{
    public required DateTime CreatedAt { get; set; }
    public DateTime? LastLogin { get; set; }
    public required string Email { get; set; }
    public required bool EmailVerified { get; set; }
    public required bool Admin { get; set; }
}