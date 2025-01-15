using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using mPass_server.Database;
using mPass_server.Utils;

namespace mPass_server.Controllers.Passwords;

[Route("passwords/delete")]
[ApiController]
[Authorize]
[Tags("Passwords")]
public class DeletePassword(DatabaseContext databaseContext) : ControllerBase
{
    /// <summary>
    /// Delete password
    /// </summary>
    /// <param name="id">Password ID</param>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
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

        databaseContext.Passwords.Remove(password);

        await databaseContext.SaveChangesAsync();

        return Ok();
    }
}