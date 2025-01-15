using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using mPass_server.Database;
using mPass_server.Utils;
using StackExchange.Redis;

namespace mPass_server.Controllers.User.UpdateEmail;

[Route("@me/email/update")]
[ApiController]
[Authorize]
[Tags("User")]
public class SetNewEmail(DatabaseContext databaseContext, IConnectionMultiplexer multiplexer) : ControllerBase
{
    private readonly IDatabase _redis = multiplexer.GetDatabase();

    /// <summary>
    /// Update email
    /// </summary>
    /// <remarks>Set new email. User should be logged out after this operation!</remarks>
    [HttpPost]
    [Consumes("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult> Post([FromBody] SetNewEmailRequest request)
    {
        var email = ControllerHelper.GetEmailFromClaims(User);
        if (email == null)
            return Unauthorized("Unauthorized");

        if (email == request.Email)
            return BadRequest("The new email address cannot be the same as the current email address.");

        if (!await IsCodeValidAsync(email, request.Code))
            return BadRequest("Invalid code");

        if (await IsEmailTakenAsync(request.Email))
            return Conflict("Email already taken");

        var userData = await databaseContext.Users.FirstOrDefaultAsync(u => u.Email == email);
        if (userData == null)
            return Unauthorized("Unauthorized");

        if (!ControllerHelper.IsBase64String(request.Salt))
            return BadRequest("Salt is not a valid base64 string");

        if (!ControllerHelper.IsBase64String(request.Verifier))
            return BadRequest("Verifier is not a valid base64 string");

        userData.Email = request.Email;
        userData.Salt = request.Salt;
        userData.Verifier = request.Verifier;
        await databaseContext.SaveChangesAsync();

        return Ok();
    }

    private async Task<bool> IsEmailTakenAsync(string email) =>
        await databaseContext.Users.AnyAsync(u => u.Email == email);

    private async Task<bool> IsCodeValidAsync(string email, string code)
    {
        var key = RedisKeys.GetUpdateEmailKey(email);
        var storedCode = await _redis.StringGetAsync(key);

        if (storedCode != code) return false;

        await _redis.KeyDeleteAsync(key);
        return true;
    }
}

public class SetNewEmailRequest
{
    public required string Email { get; set; }
    public required string Code { get; set; }
    public required string Salt { get; set; }
    public required string Verifier { get; set; }
}