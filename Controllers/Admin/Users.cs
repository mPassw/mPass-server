using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using mPass_server.Database;
using mPass_server.Utils;

namespace mPass_server.Controllers.Admin;

[Route("admin/users")]
[ApiController]
[Authorize]
[Tags("Admin")]
public class Users(DatabaseContext databaseContext) : ControllerBase
{
    /// <summary>
    /// Get all users
    /// </summary>
    [HttpGet]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(GetUsersResponse[]))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<GetUsersResponse[]>> GetUsers()
    {
        var email = ControllerHelper.GetEmailFromClaims(User);
        if (email == null)
            return Unauthorized("Unauthorized");

        var userData = await databaseContext.Users.FirstOrDefaultAsync(u => u.Email == email);
        if (userData is not { Admin: true })
            return Unauthorized("Unauthorized");

        var users = await databaseContext.Users.Include(user => user.Passwords).ToListAsync();

        return users.Select(user => new GetUsersResponse
        {
            Id = user.Id,
            CreatedAt = user.CreatedAt,
            LastLogin = user.LastLogin,
            Email = user.Email,
            Username = user.Username,
            PasswordsCount = user.Passwords?.Count ?? 0,
            EmailVerified = user.EmailVerified,
            Admin = user.Admin
        }).ToArray();
    }

    /// <summary>
    /// Get user by id
    /// </summary>
    /// <param name="id">User ID</param>
    [HttpGet("{id:int}")]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(GetUsersResponse))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<GetUsersResponse>> GetUser(int id)
    {
        var user = await databaseContext.Users.Include(user => user.Passwords).FirstOrDefaultAsync(user => user.Id == id);
        if (user == null)
            return NotFound("User not found");

        return new GetUsersResponse
        {
            Id = user.Id,
            CreatedAt = user.CreatedAt,
            LastLogin = user.LastLogin,
            Email = user.Email,
            Username = user.Username,
            PasswordsCount = user.Passwords?.Count ?? 0,
            EmailVerified = user.EmailVerified,
            Admin = user.Admin
        };
    }

    /// <summary>
    /// Toggle user role
    /// </summary>
    /// <remarks>Toggles user role between admin and user</remarks>
    /// <param name="id">User ID</param>
    [HttpPatch("{id:int}/role/toggle")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult> ToggleRole(int id)
    {
        var user = await databaseContext.Users.FirstOrDefaultAsync(user => user.Id == id);
        if (user == null)
            return NotFound("User not found");

        user.Admin = !user.Admin;
        await databaseContext.SaveChangesAsync();

        return Ok();
    }

    /// <summary>
    /// Toggle email verification status
    /// </summary>
    /// <param name="id">User ID</param>
    [HttpPatch("{id:int}/email/toggle-verification")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult> ToggleEmailVerification(int id)
    {
        var user = await databaseContext.Users.FirstOrDefaultAsync(user => user.Id == id);
        if (user == null)
            return NotFound("User not found");

        user.EmailVerified = !user.EmailVerified;
        await databaseContext.SaveChangesAsync();

        return Ok();
    }
}

public class GetUsersResponse
{
    public required int Id { get; set; }
    public required DateTime CreatedAt { get; set; }
    public DateTime? LastLogin { get; set; }
    public required string Email { get; set; }
    public string? Username { get; set; }
    public required int PasswordsCount { get; set; }
    public required bool EmailVerified { get; set; }
    public required bool Admin { get; set; }
}