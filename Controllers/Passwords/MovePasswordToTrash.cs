using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using mPass_server.Database;
using mPass_server.Database.Models;
using mPass_server.Utils;

namespace mPass_server.Controllers.Passwords;

[Route("passwords/to-trash")]
[ApiController]
[Authorize]
[Tags("Passwords")]
public class MovePasswordToTrash(DatabaseContext databaseContext) : ControllerBase
{
    /// <summary>
    /// Move password to trash
    /// </summary>
    /// <param name="id">Password ID</param>
    /// <response code="200">Password moved to trash</response>
    [HttpPost("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> Post(int id)
    {
        var email = ControllerHelper.GetEmailFromClaims(User);
        if (email == null)
            return Unauthorized("Unauthorized");

        var userData = await databaseContext.Users.FirstOrDefaultAsync(u => u.Email == email);
        if (userData == null)
            return Unauthorized("Unauthorized");

        var password = await databaseContext.Users
            .Include(u => u.Passwords)
            .SelectMany(u => u.Passwords!)
            .FirstOrDefaultAsync(p => p.Id == id && p.UserId == userData.Id);

        if (password == null)
            return NotFound("Password not found");

        await databaseContext.Trash.AddAsync(new Trash
        {
            DeletedAt = DateTime.UtcNow,
            CreatedAt = password.CreatedAt,
            UpdatedAt = password.UpdatedAt,
            Title = password.Title,
            Websites = password.Websites,
            Login = password.Login,
            Password = password.Password,
            Note = password.Note,
            Salt = password.Salt,
            Nonce = password.Nonce,
            UserId = password.UserId
        });

        databaseContext.Passwords.Remove(password);

        await databaseContext.SaveChangesAsync();

        return Ok();
    }
}