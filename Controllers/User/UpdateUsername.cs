using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using mPass_server.Database;
using mPass_server.Utils;

namespace mPass_server.Controllers.User;

[Route("@me/username/update")]
[ApiController]
[Authorize]
[Tags("User")]
public class UpdateUsername(DatabaseContext databaseContext) : ControllerBase
{
    /// <summary>
    /// Update username
    /// </summary>
    /// <remarks>Before updating the username, at least verify master password on the client side. Username is not "super important" information, so there is no email verification required</remarks>
    [HttpPatch]
    [Consumes("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult> Patch([FromBody] UpdateUsernameRequest request)
    {
        if (request.Username != null)
        {
            var existingUser = await databaseContext.Users.FirstOrDefaultAsync(u => u.Username == request.Username);
            if (existingUser != null)
                return Conflict("Username already exists");
        }

        var email = ControllerHelper.GetEmailFromClaims(User);
        if (email == null)
            return Unauthorized("Unauthorized");

        var userData = await databaseContext.Users.FirstOrDefaultAsync(u => u.Email == email);
        if (userData == null)
            return Unauthorized("Unauthorized");

        userData.Username = request.Username;
        await databaseContext.SaveChangesAsync();

        return Ok();
    }
}

public class UpdateUsernameRequest
{
    public string? Username { get; set; }
}