using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using mPass_server.Database;
using mPass_server.Utils;

namespace mPass_server.Controllers.Passwords;

[Route("passwords/bulk-delete")]
[ApiController]
[Authorize]
[Tags("Passwords")]
public class BulkDeletePasswords(DatabaseContext databaseContext) : ControllerBase
{
    /// <summary>
    /// Delete all passwords
    /// </summary>
    /// <remarks>This route does not require any verification. So, it is recommended to at least validate master password on the client before calling this route.</remarks>
    [HttpDelete]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult> Delete()
    {
        var email = ControllerHelper.GetEmailFromClaims(User);
        if (email == null)
            return Unauthorized("Unauthorized");

        var userData = await databaseContext.Users.FirstOrDefaultAsync(u => u.Email == email);
        if (userData == null)
            return Unauthorized("Unauthorized");

        var passwords = await databaseContext.Users.Include(u => u.Passwords)
            .SelectMany(u => u.Passwords!)
            .Where(p => p.UserId == userData.Id)
            .ToListAsync();

        if (passwords.Count == 0)
            return BadRequest("User has no passwords");

        databaseContext.Passwords.RemoveRange(passwords);
        await databaseContext.SaveChangesAsync();

        return Ok();
    }
}