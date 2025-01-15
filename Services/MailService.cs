using System.Net.Mail;
using mPass_server.Utils;
using StackExchange.Redis;

namespace mPass_server.Services;

public class MailService(IConfiguration configuration, SmtpClient smtpClient, IConnectionMultiplexer multiplexer)
{
    private readonly IDatabase _redis = multiplexer.GetDatabase();

    private const int RetryCount = 3;

    private readonly string _mailFrom =
        configuration["Smtp:Sender"] ?? throw new InvalidOperationException("SMTP Sender is not set");

    private static string GenerateCode(int length) =>
        new Random().Next((int)Math.Pow(10, length)).ToString($"D{length}");

    private async Task<bool> IsCodeAlreadySentAsync(string key) =>
        await _redis.KeyExistsAsync(key);

    public async Task SendMessageAsync(string recipient, string subject, string body, bool isHtml = false)
    {
        var mailMessage = new MailMessage
        {
            From = new MailAddress(_mailFrom),
            Subject = subject,
            Body = body,
            IsBodyHtml = isHtml
        };
        mailMessage.To.Add(recipient);

        for (var i = 0; i < RetryCount; i++)
        {
            try
            {
                await smtpClient.SendMailAsync(mailMessage);
                return;
            }
            catch (Exception e)
            {
                if (i == RetryCount - 1)
                {
                    throw new Exception($"Failed to send email after {RetryCount} retries", e);
                }

                Console.WriteLine($"Error sending email (attempt {i + 1}/{RetryCount}): {e.Message}");
                await Task.Delay(1000);
            }
        }
    }

    public async Task SendEmailVerificationMessageAsync(string recipient, int codeLenght = 6)
    {
        var key = RedisKeys.GetVerifyEmailKey(recipient);

        if (await IsCodeAlreadySentAsync(key))
            throw new InvalidOperationException("Verification code can only be sent once every 5 minutes");

        var code = GenerateCode(codeLenght);

        await SendMessageAsync(
            recipient,
            "mPass Email Verification",
            $"Your verification code is: {code}. This code will expire in 5 minutes.");

        await _redis.StringSetAsync(key, code, TimeSpan.FromMinutes(5));
    }

    public async Task SendUpdateEmailVerificationCodeAsync(string recipient, int codeLenght = 6)
    {
        var key = RedisKeys.GetUpdateEmailKey(recipient);

        if (await IsCodeAlreadySentAsync(key))
            throw new InvalidOperationException("Verification code can only be sent once every 5 minutes");

        var code = GenerateCode(codeLenght);

        await SendMessageAsync(
            recipient,
            "mPass Email Update Verification",
            $"Your verification code is: {code}. This code will expire in 5 minutes.");

        await _redis.StringSetAsync(key, code, TimeSpan.FromMinutes(5));
    }

    // this function is a mess, but im not gonna change it :)
    public async Task SendAccountLockedMessageAsync(string recipient, string baseUrl)
    {
        var id = Guid.NewGuid().ToString("N");
        var unlockUrl = $"{baseUrl}/account/{recipient}/unlock/{id}";

        await _redis.StringSetAsync(RedisKeys.GetAccountUnlockKey(recipient), id);
        await _redis.KeyDeleteAsync(RedisKeys.GetLoginAttemptKey(recipient));

        // Construct the HTML body
        var htmlBody = $"""
                        <html>
                        <head>
                            <title>mPass Account Locked</title>
                            </head>
                            <body>
                                <p>Your account has been locked due to multiple failed login attempts.</p>
                                <p>Please click the following link to unlock your account:</p>
                                <p><a href='{unlockUrl}'>{unlockUrl}</a></p>
                            </body>
                        </html>
                        """;

        await SendMessageAsync(
            recipient,
            "mPass Account Locked",
            htmlBody,
            true);
    }
}