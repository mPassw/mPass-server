using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using mPass_server.Utils;
using StackExchange.Redis;

namespace mPass_server.Controllers.User;

[Route("@me/[controller]")]
[ApiController]
[Authorize]
[Tags("User")]
public class DeauthorizeSessions(IConnectionMultiplexer multiplexer) : ControllerBase
{
    private readonly IDatabase _redisDb = multiplexer.GetDatabase();

    /// <summary>
    /// Deauthorize all sessions for the current user
    /// </summary>
    /// <remarks>User must be logged out after this request</remarks>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult> Post()
    {
        var email = ControllerHelper.GetEmailFromClaims(User);
        if (email == null)
            return Unauthorized("Unauthorized");

        var sessionKeyPattern = RedisKeys.GetSessionIdKey(email, "*");

        var server = multiplexer.GetServer(multiplexer.GetEndPoints().First());

        var keysToDelete = new List<RedisKey>();
        await foreach(var key in server.KeysAsync(pattern: sessionKeyPattern))
            keysToDelete.Add(key);

        if (keysToDelete.Count != 0)
            await _redisDb.KeyDeleteAsync(keysToDelete.ToArray());

        return Ok();
    }
}