using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using mPass_server.Database;
using mPass_server.Utils;

namespace mPass_server.Controllers.Passwords;

[Route("passwords")]
[ApiController]
[Authorize]
[Tags("Passwords")]
public class GetServicePasswords(DatabaseContext databaseContext) : ControllerBase
{
    /// <summary>
    /// Get all passwords
    /// </summary>
    [HttpGet]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<GetServicePasswordsResponsePassword>))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<List<GetServicePasswordsResponsePassword>>> Get()
    {
        var email = ControllerHelper.GetEmailFromClaims(User);
        if (email == null)
            return Unauthorized();

        var userData = await databaseContext.Users.FirstOrDefaultAsync(u => u.Email == email);
        if (userData == null)
            return Unauthorized();

        var passwords = await databaseContext.Users
            .Include(u => u.Passwords)
            .FirstOrDefaultAsync(u => u.Id == userData.Id);

        if (passwords?.Passwords == null)
            return NotFound();

        return passwords.Passwords.Select(p => new GetServicePasswordsResponsePassword
        {
            Id = p.Id,
            Title = p.Title,
            Websites = p.Websites,
            Login = p.Login,
            Password = p.Password,
            Note = p.Note,
            Salt = p.Salt,
            Nonce = p.Nonce
        }).ToList();
    }
}

public class GetServicePasswordsResponsePassword
{
    public required int Id { get; set; }
    public required string Title { get; set; }
    public List<string>? Websites { get; set; }
    public string? Login { get; set; }
    public string? Password { get; set; }
    public string? Note { get; set; }
    public required string Salt { get; set; }
    public required string Nonce { get; set; }
}