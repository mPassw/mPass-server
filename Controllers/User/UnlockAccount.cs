using Microsoft.AspNetCore.Mvc;
using mPass_server.Utils;
using StackExchange.Redis;

namespace mPass_server.Controllers.User;

[Route("account")]
[ApiController]
[Tags("User")]
public class UnlockAccount(IConnectionMultiplexer multiplexer) : ControllerBase
{
    private readonly IDatabase _redis = multiplexer.GetDatabase();

    /// <summary>
    /// Unlock account (from email link)
    /// </summary>
    /// <remarks>This endpoint is handled automatically in browser</remarks>
    [HttpGet("{email}/unlock/{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> Get(string email, string id)
    {
        Console.WriteLine($"Unlocking account for {email}");

        var key = RedisKeys.GetAccountUnlockKey(email);
        var value = await _redis.StringGetAsync(key);

        if (value.IsNullOrEmpty || value != id)
            return BadRequest("Invalid unlock code");

        await _redis.KeyDeleteAsync(key);

        return Ok("Account unlocked. You can now login.");
    }
}