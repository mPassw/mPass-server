using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace mPass_server.Services;

public class JwtService(IConfiguration configuration)
{
    private readonly string? _securityKey = configuration["Auth:SecurityKey"];
    private readonly string? _issuer = configuration["Auth:Issuer"];

    public string CreateToken(string email)
    {
        if (string.IsNullOrEmpty(_securityKey))
            throw new InvalidOperationException("Auth Secret key is not set");

        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_securityKey));

        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(
            [
                new Claim(JwtRegisteredClaimNames.Sub, email)
            ]),
            Expires = DateTime.UtcNow.AddMinutes(30),
            SigningCredentials = credentials,
            Issuer = _issuer ?? "mPass"
        };

        var handler = new JsonWebTokenHandler();

        return handler.CreateToken(tokenDescriptor);
    }
}