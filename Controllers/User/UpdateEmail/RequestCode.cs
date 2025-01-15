using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using mPass_server.Services;
using mPass_server.Utils;

namespace mPass_server.Controllers.User.UpdateEmail;

[Route("@me/email/update/request-code")]
[ApiController]
[Authorize]
[Tags("User")]
public class RequestCode(MailService mailService) : ControllerBase
{
    /// <summary>
    /// Update email (request code)
    /// </summary>
    /// <remarks>Request a verification code to update email</remarks>
    /// <response code="200">Code sent to email</response>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult> Post()
    {
        var email = ControllerHelper.GetEmailFromClaims(User);
        if (email == null)
            return Unauthorized("Unauthorized");

        try
        {
            await mailService.SendUpdateEmailVerificationCodeAsync(email);
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