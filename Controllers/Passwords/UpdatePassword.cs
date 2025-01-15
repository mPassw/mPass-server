using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using mPass_server.Database;
using mPass_server.Utils;

namespace mPass_server.Controllers.Passwords;

[Route("passwords/update")]
[ApiController]
[Authorize]
[Tags("Passwords")]
public class UpdatePassword(DatabaseContext databaseContext) : ControllerBase
{
    /// <summary>
    /// Update password
    /// </summary>
    [HttpPatch("{id:int}")]
    [Consumes("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult> Patch([FromBody] UpdateServicePasswordRequest request, int id)
    {
        if (request.Login == null && request.Password == null && request.Note == null)
        {
            return BadRequest("At least one of the following fields must be provided: Login, Password, Note");
        }

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

        if (request.Title != null)
            password.Title = request.Title;
        if (request.Websites != null || request.Websites?.Count > 0)
            password.Websites = request.Websites;
        if (request.Login != null)
            password.Login = request.Login;
        if (request.Password != null)
            password.Password = request.Password;
        if (request.Note != null)
            password.Note = request.Note;

        password.UpdatedAt = DateTime.UtcNow;

        await databaseContext.SaveChangesAsync();

        return Ok();
    }
}

public class UpdateServicePasswordRequest
{
    public string? Title {get; set;}
    public List<string>? Websites {get; set;}
    public string? Login { get; set; }
    public string? Password { get; set; }
    public string? Note { get; set; }
}