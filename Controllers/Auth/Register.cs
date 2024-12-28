using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using mPass_server.Database;
using mPass_server.Database.Models;
using mPass_server.Services;

namespace mPass_server.Controllers.Auth;

[Route("/auth/[controller]")]
[ApiController]
public class Register(MailService mailService, DatabaseContext databaseContext)
    : ControllerBase
{
    /// <summary>
    /// Register
    /// </summary>
    [HttpPost]
    [Produces("application/json")]
    [Consumes("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [Tags("Register")]
    public async Task<ActionResult<RegisterResponse>> Post([FromBody] RegisterRequest request)
    {
        if (!IsValidEmail(request.Email))
            return BadRequest("Invalid email");

        var emailExists = await databaseContext.Users.AnyAsync(u => u.Email == request.Email);
        if (emailExists)
            return Conflict("Email already exists");

        var usersCount = await databaseContext.Users.CountAsync();

        if (usersCount > 0)
            try
            {
                await mailService.SendEmailVerificationMessageAsync(request.Email);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return StatusCode(500);
            }

        await databaseContext.Users.AddAsync(new Database.Models.User
        {
            Admin = usersCount == 0,
            CreatedAt = DateTime.UtcNow,
            Email = request.Email,
            EmailVerified = usersCount == 0,
            Salt = request.Salt,
            Verifier = request.Verifier
        });
        await databaseContext.SaveChangesAsync();

        return new RegisterResponse
        {
            EmailVerificationRequired = usersCount != 0
        };
    }

    private static bool IsValidEmail(string email) =>
        new EmailAddressAttribute().IsValid(email);
}

public class RegisterRequest
{
    public required string Email { get; set; }
    public required string Salt { get; set; }
    public required string Verifier { get; set; }
}

public class RegisterResponse
{
    public required bool EmailVerificationRequired { get; set; }
}