using System.Collections.Concurrent;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using mPass_server.Database;
using mPass_server.Services;
using mPass_server.Utils;
using Org.BouncyCastle.Crypto.Agreement.Srp;
using Org.BouncyCastle.Crypto.Digests;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.Security;
using StackExchange.Redis;

namespace mPass_server.Controllers.Auth;

[Route("auth/[controller]")]
[ApiController]
[Tags("Login")]
public class Login(DatabaseContext databaseContext, JwtService jwtService, IConnectionMultiplexer multiplexer, MailService mailService) : ControllerBase
{
    // 2048-bit safe prime
    private readonly BigInteger _n = new(
        "AC6BDB41324A9A9BF166DE5E1389582FAF72B6651987EE07FC319294" +
        "3DB56050A37329CBB4A099ED8193E0757767A13DD52312AB4B03310D" +
        "CD7F48A9DA04FD50E8083969EDB767B0CF6095179A163AB3661A05FB" +
        "D5FAAAE82918A9962F0B93B855F97993EC975EEAA80D740ADBF4FF74" +
        "7359D041D5C33EA71D281E446B14773BCA97B43A23FB801676BD207A" +
        "436C6481F1D2B9078717461A5B9D32E688F87748544523B524B0D57D" +
        "5EA77A2775D2ECFA032CFBDBF52FB3786160279004E57AE6AF874E73" +
        "03CE53299CCC041C7BC308D82A5698F3A8D0C38271AE35F8E9DBFBB6" +
        "94B5C803D89F7AE435DE236D525F54759B65E372FCD68EF20FA7111F" +
        "9E4AFF73", 16
    );
    private readonly BigInteger _g = new("2", 16);

    private readonly IDatabase _redis = multiplexer.GetDatabase();

    private const int MaxLoginAttempts = 3;

    private async Task<bool> IsAuthLocked(string email)
    {
        var key = RedisKeys.GetAccountUnlockKey(email);
        var value = await _redis.StringGetAsync(key);

        return !value.IsNullOrEmpty;
    }

    private async Task<int> AddLoginFailedAttempt(string email, string baseUrl)
    {
        var key = RedisKeys.GetLoginAttemptKey(email);
        var value = await _redis.StringGetAsync(key);
        int attempts;

        if (value.IsNullOrEmpty)
        {
            await _redis.StringSetAsync(key, 1);
            attempts = 1;
        }
        else
        {
            attempts = (int)await _redis.StringIncrementAsync(key);
        }

        // Send account locked message if attempts reach MaxLoginAttempts
        if (attempts == MaxLoginAttempts)
        {
            await mailService.SendAccountLockedMessageAsync(email, baseUrl);
        }

        return attempts;
    }

    private async Task RemoveLoginFailedAttempt(string email)
    {
        var key = RedisKeys.GetLoginAttemptKey(email);
        await _redis.KeyDeleteAsync(key);
    }

    private async Task SaveSessionId(string email, string sessionId)
    {
        var key = RedisKeys.GetSessionIdKey(email, sessionId);
        await _redis.StringSetAsync(key, "", TimeSpan.FromMinutes(30));
    }

    /// <summary>
    /// Step 1
    /// </summary>
    /// <remarks>Get salt</remarks>
    [HttpPost("request-salt")]
    [Produces("application/json")]
    [Consumes("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(LoginStep1Response))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<LoginStep1Response>> Step1([FromBody] LoginStep1Request request)
    {
        if (await IsAuthLocked(request.Email))
            return StatusCode(429, "Account is temporarily locked. Check your email to unlock.");

        if (TempStore.ServerInstanceStorage.TryGetValue(request.Email, out var server))
            TempStore.ServerInstanceStorage.TryRemove(request.Email, out _);

        var userData = await databaseContext.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
        if (userData == null)
            return NotFound("User not found");

        return new LoginStep1Response
        {
            Salt = userData.Salt
        };
    }

    /// <summary>
    /// Step 2
    /// </summary>
    /// <remarks>Get verifier, B</remarks>
    [HttpPost("send-credentials")]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(LoginStep2Response))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Step2([FromBody] LoginStep2Request request)
    {
        try
        {
            var userData = await databaseContext.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
            if (userData == null)
                return BadRequest("Invalid credentials");

            var groupParameters = new Srp6GroupParameters(_n, _g);

            var server = new Srp6Server();
            await Task.Run(() => server.Init(groupParameters,
                new BigInteger(Encoding.UTF8.GetString(Convert.FromBase64String(userData.Verifier)), 16),
                new Sha256Digest(),
                new SecureRandom()));

            var b = await Task.Run(() => server.GenerateServerCredentials()) ?? throw new Exception();

            _ = await Task.Run(() => server.CalculateSecret(new BigInteger(request.A, 16))) ??
                    throw new Exception();

            TempStore.ServerInstanceStorage[request.Email] = server;

            return Ok(new LoginStep2Response
            {
                B = b.ToString()
            });
        }
        catch (Exception)
        {
            TempStore.ServerInstanceStorage.TryRemove(request.Email, out _);
            var attempts = await AddLoginFailedAttempt(request.Email, $"{Request.Scheme}://{Request.Host.Host}");
            if (attempts == MaxLoginAttempts)
            {
                return BadRequest("Account is locked. Check your email to unlock.");
            }
            var remainingAttempts = Math.Max(0, MaxLoginAttempts - attempts);
            return BadRequest($"Invalid credentials. You have {remainingAttempts} attempts remaining.");
        }
    }

    /// <summary>
    /// Step 3
    /// </summary>
    /// <remarks>Get M2, JWT Token</remarks>
    [HttpPost("verify-proof")]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(LoginStep3Response))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Step3([FromBody] LoginStep3Request request)
    {
        try
        {
            var userData = await databaseContext.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
            if (userData == null)
                return BadRequest("Invalid credentials");

            if (!TempStore.ServerInstanceStorage.TryGetValue(request.Email, out var server))
                throw new Exception();

            var isClientProofValid =
                await Task.Run(() => server.VerifyClientEvidenceMessage(new BigInteger(request.M1, 16)));
            if (!isClientProofValid)
                throw new Exception();

            var serverProof = await Task.Run(() => server.CalculateServerEvidenceMessage()) ?? throw new Exception();

            userData.LastLogin = DateTime.UtcNow;
            await databaseContext.SaveChangesAsync();

            await RemoveLoginFailedAttempt(request.Email);

            var sessionId = Guid.NewGuid().ToString();
            await SaveSessionId(request.Email, sessionId);

            return Ok(new LoginStep3Response
            {
                M2 = serverProof.ToString(),
                Token = jwtService.CreateToken(userData.Email, sessionId)
            });
        }
        catch (Exception)
        {
            var attempts = await AddLoginFailedAttempt(request.Email, $"{Request.Scheme}://{Request.Host.Host}");
            if (attempts == MaxLoginAttempts)
            {
                return BadRequest("Account is locked. Check your email to unlock.");
            }
            var remainingAttempts = Math.Max(0, MaxLoginAttempts - attempts);
            return BadRequest($"Invalid credentials. You have {remainingAttempts} attempts remaining.");
        }
        finally
        {
            TempStore.ServerInstanceStorage.TryRemove(request.Email, out _);
        }
    }
}

public class LoginStep1Request
{
    public required string Email { get; set; }
}

public class LoginStep1Response
{
    public required string Salt { get; set; }
}

public class LoginStep2Request
{
    public required string Email { get; set; }
    public required string A { get; set; }
}

public class LoginStep2Response
{
    public required string B { get; set; }
}

public class LoginStep3Request
{
    public required string Email { get; set; }
    public required string M1 { get; set; }
}

public class LoginStep3Response
{
    public required string M2 { get; set; }
    public required string Token { get; set; }
}

public static class TempStore
{
    public static readonly ConcurrentDictionary<string, Srp6Server> ServerInstanceStorage = new();
}