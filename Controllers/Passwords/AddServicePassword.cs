using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using mPass_server.Database;
using mPass_server.Database.Models;
using mPass_server.Utils;

namespace mPass_server.Controllers.Passwords;

[Route("passwords/new")]
[ApiController]
[Authorize]
[Tags("Passwords")]
public class AddServicePassword(DatabaseContext databaseContext) : ControllerBase
{
    /// <summary>
    /// Add new password
    /// </summary>
    [HttpPost]
    [Consumes("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult> Post([FromBody] AddServicePasswordRequest request)
    {
        if (request.Login == null && request.Password == null)
            return BadRequest("Login or Password is required");

        var email = ControllerHelper.GetEmailFromClaims(User);
        if (email == null)
            return Unauthorized();

        var userData = await databaseContext.Users.FirstOrDefaultAsync(u => u.Email == email);
        if (userData == null)
            return Unauthorized();

        if (!userData.EmailVerified)
            return Forbid("Email is not verified");

        await databaseContext.Passwords.AddAsync(new ServicePassword
        {
            Title = request.Title,
            CreatedAt = DateTime.UtcNow,
            Websites = request.Websites ?? [],
            Login = request.Login,
            Password = request.Password,
            Note = request.Note,
            Salt = request.Salt,
            Nonce = request.Nonce,
            UserId = userData.Id,
        });
        await databaseContext.SaveChangesAsync();

        return Ok();
    }
}

public class AddServicePasswordRequest
{
    public required string Title { get; set; }
    public List<string>? Websites { get; set; }
    public string? Login { get; set; }
    public string? Password { get; set; }
    public string? Note { get; set; }
    public required string Salt { get; set; }
    public required string Nonce { get; set; }
}