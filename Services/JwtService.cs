using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace mPass_server.Services;

public class JwtService(IConfiguration configuration)
{
    private readonly string? _securityKey = configuration["Auth:SecurityKey"];
    private readonly string? _issuer = configuration["Auth:Issuer"];

    public string CreateToken(string email, string sessionId)
    {
        if (string.IsNullOrEmpty(_securityKey))
            throw new InvalidOperationException("Auth Security Key is not set");

        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_securityKey)),
            SecurityAlgorithms.HmacSha256);

        var tokenDescriptor = CreateTokenDescriptor(email, sessionId, credentials);

        return new JsonWebTokenHandler().CreateToken(tokenDescriptor);
    }

    private SecurityTokenDescriptor CreateTokenDescriptor(string email, string sessionId, SigningCredentials credentials)
    {
        return new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(
            [
                new Claim(JwtRegisteredClaimNames.Sub, email),
                new Claim(JwtRegisteredClaimNames.Jti, sessionId)
            ]),
            Expires = DateTime.UtcNow.AddMinutes(30),
            SigningCredentials = credentials,
            Issuer = _issuer ?? "mPass"
        };
    }
}