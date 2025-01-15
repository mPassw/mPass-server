using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using mPass_server.Database;
using mPass_server.Services;
using mPass_server.Utils;

namespace mPass_server.Controllers.Admin;

[Route("admin/smtp")]
[ApiController]
[Authorize]
[Tags("Admin")]
public class SmtpTestEmail(MailService mailService) : ControllerBase
{
    private const string Subject = "mPass test email";
    private const string Body = "This is a test email from your mPass server. If you received this email, your SMTP settings are correct!";

    /// <summary>
    /// Send test email
    /// </summary>
    /// <param name="request">Email to send the test email to</param>
    /// <response code="200">Email sent successfully</response>
    [HttpPost("test")]
    [Consumes("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult> Post([FromBody] SmtpTestEmailRequest request)
    {
        var email = ControllerHelper.GetEmailFromClaims(User);
        if (email == null)
            return Unauthorized("Unauthorized");

        try
        {
            await mailService.SendMessageAsync(request.Email, Subject, Body);
            return Ok();
        }
        catch (Exception)
        {
            return BadRequest("Failed to send email");
        }
    }
}

public class SmtpTestEmailRequest
{
    public required string Email { get; set; }
}