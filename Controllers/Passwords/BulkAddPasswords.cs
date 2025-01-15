using System.Globalization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using mPass_server.Database;
using mPass_server.Database.Models;
using mPass_server.Utils;

namespace mPass_server.Controllers.Passwords;

[Route("passwords/bulk-add")]
[ApiController]
[Authorize]
[Tags("Passwords")]
public class BulkAddPasswords(DatabaseContext databaseContext) : ControllerBase
{
    /// <summary>
    /// Bulk add passwords (import)
    /// </summary>
    [HttpPost]
    [Consumes("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult> Post([FromBody] BulkAddPasswordsRequest request)
    {
        var email = ControllerHelper.GetEmailFromClaims(User);
        if (email == null)
            return Unauthorized("Unauthorized");

        var userData = await databaseContext.Users.FirstOrDefaultAsync(u => u.Email == email);
        if (userData == null)
            return Unauthorized("Unauthorized");

        if (!userData.EmailVerified)
            return Forbid();

        foreach (var password in request.Passwords)
        {
            await databaseContext.Passwords.AddAsync(new ServicePassword
            {
                InTrash = password.InTrash,
                Title = password.Title,
                CreatedAt = DateTime.Parse(password.CreatedAt ?? DateTime.UtcNow.ToString(CultureInfo.InvariantCulture)),
                UpdatedAt = password.UpdatedAt == null ? null : DateTime.Parse(password.UpdatedAt),
                Websites = password.Websites ?? [],
                Login = password.Login,
                Password = password.Password,
                Note = password.Note,
                Salt = password.Salt,
                Nonce = password.Nonce,
                UserId = userData.Id,
            });
        }

        await databaseContext.SaveChangesAsync();

        return Ok();
    }
}

public class BulkAddPasswordsRequest
{
    public required List<BulkAddPasswordsPassword> Passwords { get; set; }
}

public class BulkAddPasswordsPassword
{
    public string? CreatedAt { get; set; }
    public string? UpdatedAt { get; set; }
    public required bool InTrash { get; set; }
    public required string Title { get; set; }
    public List<string>? Websites { get; set; } = [];
    public string? Login { get; set; }
    public string? Password { get; set; }
    public string? Note { get; set; }
    public required string Salt { get; set; }
    public required string Nonce { get; set; }
}