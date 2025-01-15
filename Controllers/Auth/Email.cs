using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using mPass_server.Database;
using mPass_server.Services;
using mPass_server.Utils;
using StackExchange.Redis;

namespace mPass_server.Controllers.Auth;

[Route("auth/[controller]")]
[ApiController]
[Tags("Email")]
public class Email(DatabaseContext databaseContext, MailService mailService, IConnectionMultiplexer multiplexer) : ControllerBase
{
    private readonly IDatabase _redisDb = multiplexer.GetDatabase();

    /// <summary>
    /// Verify
    /// </summary>
    [HttpPost("verify")]
    [Consumes("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> VerifyEmail([FromBody] VerifyEmailRequest request)
    {
        var userData = await databaseContext.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
        if (userData == null)
            return NotFound("User not found");

        if (userData.EmailVerified)
            return BadRequest("Email already verified");

        var key = RedisKeys.GetVerifyEmailKey(request.Email);

        var code = await _redisDb.StringGetAsync(key);
        if (code != request.Code)
            return BadRequest("Invalid code");

        userData.EmailVerified = true;
        await databaseContext.SaveChangesAsync();

        await _redisDb.KeyDeleteAsync(key);

        return Ok();
    }

    /// <summary>
    /// Resend verification code
    /// </summary>
    [HttpPost("resend")]
    [Consumes("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [Tags("Email")]
    public async Task<ActionResult> ResendVerificationCode([FromBody] ResendVerificationCodeRequest request)
    {
        var userData = await databaseContext.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
        if (userData == null)
            return NotFound("User not found");

        if (userData.EmailVerified)
            return BadRequest("Email already verified");

        try
        {
            await mailService.SendEmailVerificationMessageAsync(request.Email);
            return Ok();
        }
        catch (InvalidOperationException e)
        {
            return BadRequest(e.Message);
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            return StatusCode(500);
        }
    }
}

public class VerifyEmailRequest
{
    public required string Email { get; set; }
    public required string Code { get; set; }
}

public class ResendVerificationCodeRequest
{
    public required string Email { get; set; }
}