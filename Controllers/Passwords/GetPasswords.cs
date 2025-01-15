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
public class GetPasswords(DatabaseContext databaseContext) : ControllerBase
{
    /// <summary>
    /// Get passwords
    /// </summary>
    /// <param name="limit">Limit the number of results</param>
    /// <param name="offset">Offset the results</param>
    /// <param name="orderBy">Order by title, createdAt</param>
    /// <param name="orderDirection">Order direction (asc or desc)</param>
    /// <param name="search">Search for a specific title, website</param>
    [HttpGet]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<GetServicePasswordsResponsePassword>))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<List<GetServicePasswordsResponsePassword>>> Get(
        [FromQuery] int? limit,
        [FromQuery] int? offset,
        [FromQuery] string? orderBy,
        [FromQuery] string? orderDirection,
        [FromQuery] string? search)
    {
        var email = ControllerHelper.GetEmailFromClaims(User);
        if (email == null)
            return Unauthorized("Unauthorized");

        var userWithPasswords = await databaseContext.Users
            .Include(u => u.Passwords)
            .FirstOrDefaultAsync(u => u.Email == email);

        if (userWithPasswords == null)
            return Unauthorized("Unauthorized");

        if (userWithPasswords.Passwords == null)
            return NotFound("No passwords found");

        var query = userWithPasswords.Passwords.AsQueryable();

        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(p =>
                p.Websites != null && (p.Title.Contains(search) ||
                                       p.Websites.Any(w => w.Contains(search))));
        }

        if (!string.IsNullOrEmpty(orderBy))
        {
            if (orderBy.Equals("title", StringComparison.OrdinalIgnoreCase))
            {
                query = "desc".Equals(orderDirection, StringComparison.OrdinalIgnoreCase)
                    ? query.OrderByDescending(p => p.Title)
                    : query.OrderBy(p => p.Title);
            }
            else if (orderBy.Equals("createdAt", StringComparison.OrdinalIgnoreCase))
            {
                query = "desc".Equals(orderDirection, StringComparison.OrdinalIgnoreCase)
                    ? query.OrderByDescending(p => p.CreatedAt)
                    : query.OrderBy(p => p.CreatedAt);
            }
        }

        if (offset.HasValue) query = query.Skip(offset.Value);
        if (limit.HasValue) query = query.Take(limit.Value);

        return query.Select(p => new GetServicePasswordsResponsePassword
        {
            Id = p.Id,
            CreatedAt = p.CreatedAt,
            UpdatedAt = p.UpdatedAt,
            InTrash = p.InTrash,
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
    public required DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public required bool InTrash { get; set; }
    public required string Title { get; set; }
    public List<string>? Websites { get; set; }
    public string? Login { get; set; }
    public string? Password { get; set; }
    public string? Note { get; set; }
    public required string Salt { get; set; }
    public required string Nonce { get; set; }
}