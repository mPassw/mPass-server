using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using mPass_server.Database;
using mPass_server.Utils;
using StackExchange.Redis;

namespace mPass_server.Middleware;

/// <summary>
/// Middleware to validate the session id. If request is made to admin routes, it also checks if the user is an admin.
/// </summary>
public class ValidateSession(RequestDelegate next, IConnectionMultiplexer multiplexer, IServiceScopeFactory serviceScopeFactory)
{
    private readonly IDatabase _redis = multiplexer.GetDatabase();
    private readonly DatabaseContext _databaseContext = serviceScopeFactory.CreateScope().ServiceProvider.GetRequiredService<DatabaseContext>();

    public async Task InvokeAsync(HttpContext context)
    {
        var endpoint = context.GetEndpoint();

        if (endpoint != null && RequiresAuthorization(endpoint))
        {
            var email = ControllerHelper.GetEmailFromClaims(context.User);
            var sessionId = ControllerHelper.GetSessionIdFromClaims(context.User);
            if (email == null || sessionId == null)
            {
                await ReturnUnauthorized(context);
                return;
            }

            if (!await SessionExists(email, sessionId))
            {
                await ReturnCustomUnauthorized(context);
                return;
            }

            if (context.Request.Path.StartsWithSegments("/admin"))
            {
                if (!await IsAdmin(email))
                {
                    await ReturnUnauthorized(context);
                    return;
                }
            }
        }

        await next(context);
    }

    private static bool RequiresAuthorization(Endpoint endpoint)
    {
        var authorizeRouteAttribute = endpoint.Metadata.GetMetadata<AuthorizeAttribute>();
        return authorizeRouteAttribute != null;
    }

    private async Task<bool> SessionExists(string email, string sessionId)
    {
        return await _redis.KeyExistsAsync(RedisKeys.GetSessionIdKey(email, sessionId));
    }

    private async Task<bool> IsAdmin(string email)
    {
        var userData = await _databaseContext.Users.FirstOrDefaultAsync(u => u.Email == email);
        return userData is { Admin: true };
    }

    private static async Task ReturnUnauthorized(HttpContext context)
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        await context.Response.WriteAsync("Unauthorized");
    }

    private static async Task ReturnCustomUnauthorized(HttpContext context)
    {
        context.Response.StatusCode = 498;
        await context.Response.WriteAsync("Invalid session id");
    }
}