namespace mPass_server.Utils;

public static class RedisKeys
{
    /// <summary>
    /// Get the key for email verification
    /// </summary>
    public static string GetVerifyEmailKey(string email) => $"auth:email:verification:{email}";

    /// <summary>
    /// Get the key for updating email
    /// </summary>
    public static string GetUpdateEmailKey(string email) => $"user:{email}:update-email:code";

    /// <summary>
    /// Get the key for account unlock
    /// </summary>
    public static string GetAccountUnlockKey(string email) => $"auth:account:unlock:{email}";

    /// <summary>
    /// Get the key for login attempt
    /// </summary>
    public static string GetLoginAttemptKey(string email) => $"auth:login:attempt:{email}";

    /// <summary>
    /// Get the key for session id
    /// </summary>
    public static string GetSessionIdKey(string email, string sessionId) => $"sessions:{email}:{sessionId}";
}