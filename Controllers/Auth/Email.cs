using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using mPass_server.Database;
using mPass_server.Services;
using StackExchange.Redis;

namespace mPass_server.Controllers.Auth;

[Route("auth/[controller]")]
[ApiController]
public class Email(DatabaseContext databaseContext, MailService mailService, IConnectionMultiplexer multiplexer) : ControllerBase
{
    /// <summary>
    /// Verify
    /// </summary>
    [HttpPost("verify")]
    [Consumes("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [Tags("Email")]
    public async Task<ActionResult> VerifyEmail([FromBody] VerifyEmailRequest request)
    {
        var userData = await databaseContext.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
        if (userData == null)
            return NotFound("User not found");

        if (userData.EmailVerified)
            return BadRequest("Email already verified");

        var redisDb = multiplexer.GetDatabase();

        var code = await redisDb.StringGetAsync("auth:email:verification:" + request.Email);
        if (code != request.Code)
            return BadRequest("Invalid code");

        userData.EmailVerified = true;
        await databaseContext.SaveChangesAsync();

        await redisDb.KeyDeleteAsync("auth:email:verification:" + request.Email);

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
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            return StatusCode(500);
        }

        return Ok();
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