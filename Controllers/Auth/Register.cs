using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using mPass_server.Database;
using mPass_server.Database.Models;
using mPass_server.Services;
using mPass_server.Utils;

namespace mPass_server.Controllers.Auth;

[Route("/auth/[controller]")]
[ApiController]
[Tags("Register")]
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
    public async Task<ActionResult<RegisterResponse>> Post([FromBody] RegisterRequest request)
    {
        if (!ControllerHelper.IsValidEmail(request.Email))
            return BadRequest("Invalid email");

        if (!ControllerHelper.IsBase64String(request.Salt))
            return BadRequest("Salt is not a valid base64 string");

        if (!ControllerHelper.IsBase64String(request.Verifier))
            return BadRequest("Verifier is not a valid base64 string");

        var emailExists = await databaseContext.Users.AnyAsync(u => u.Email == request.Email);
        if (emailExists)
            return Conflict("Email already registered");

        if (!string.IsNullOrWhiteSpace(request.Username))
        {
            var existingUser = await databaseContext.Users.FirstOrDefaultAsync(u => u.Username == request.Username);
            if (existingUser != null)
                return Conflict("Username already taken");
        }

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
            Username = request.Username,
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
}

public class RegisterRequest
{
    public required string Email { get; set; }
    public string? Username { get; set; }
    public required string Salt { get; set; }
    public required string Verifier { get; set; }
}

public class RegisterResponse
{
    public required bool EmailVerificationRequired { get; set; }
}