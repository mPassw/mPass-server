using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using mPass_server.Database;
using mPass_server.Database.Models;
using mPass_server.Utils;

namespace mPass_server.Controllers.Passwords;

[Route("passwords/from-trash")]
[ApiController]
[Authorize]
[Tags("Passwords")]
public class MovePasswordFromTrash(DatabaseContext databaseContext) : ControllerBase
{
    /// <summary>
    /// Move password from trash
    /// </summary>
    /// <param name="id">Password ID</param>
    [HttpPost("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> Delete(int id)
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

        password.InTrash = false;
        password.UpdatedAt = DateTime.UtcNow;

        await databaseContext.SaveChangesAsync();

        return Ok();
    }
}