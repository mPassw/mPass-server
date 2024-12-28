using System.Net.Mail;
using StackExchange.Redis;

namespace mPass_server.Services;

public class MailService(IConfiguration configuration, SmtpClient smtpClient, IConnectionMultiplexer multiplexer)
{
    private const int RetryCount = 3;

    private readonly string _mailFrom =
        configuration["Smtp:Sender"] ?? throw new InvalidOperationException("SMTP Sender is not set");

    private static string GenerateCode(int length) =>
        new Random().Next((int)Math.Pow(10, length)).ToString($"D{length}");

    private async Task SendMessageAsync(string recipient, string subject, string body, bool isHtml = false)
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
        var redisDb = multiplexer.GetDatabase();

        // since mail services are making tight limits
        // we are checking if there is already a verification code in redis
        // if it exists, that means mail service already sent a mail
        var existingCode = await redisDb.StringGetAsync($"auth:email:verification:{recipient}");
        if (!existingCode.IsNullOrEmpty)
            return;

        var code = GenerateCode(codeLenght);

        await SendMessageAsync(
            recipient,
            "mPass Email Verification",
            $"Your verification code is: {code}. This code will expire in 5 minutes.");

        await redisDb.StringSetAsync($"auth:email:verification:{recipient}", code, TimeSpan.FromMinutes(5));
    }
}