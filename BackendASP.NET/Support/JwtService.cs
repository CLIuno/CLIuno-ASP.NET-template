using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace BackendASPNET.Support;

public class JwtService
{
    private readonly SymmetricSecurityKey _accessKey;
    private readonly SymmetricSecurityKey _refreshKey;

    public JwtService()
    {
        _accessKey = Key(Environment.GetEnvironmentVariable("JWT_SECRET_KEY")
            ?? "dev-jwt-secret-change-me-please-32chars");
        _refreshKey = Key(Environment.GetEnvironmentVariable("REFRESH_JWT_SECRET_KEY")
            ?? "dev-refresh-secret-change-me-32chars!");
    }

    private static SymmetricSecurityKey Key(string secret) =>
        new(Encoding.UTF8.GetBytes(secret.PadRight(32, '#')));

    public string Sign(long userId, TimeSpan ttl, bool refresh)
    {
        var token = new JwtSecurityToken(
            claims: [new Claim(JwtRegisteredClaimNames.Sub, userId.ToString())],
            expires: DateTime.UtcNow.Add(ttl),
            signingCredentials: new SigningCredentials(
                refresh ? _refreshKey : _accessKey, SecurityAlgorithms.HmacSha256));
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public long? Verify(string token, bool refresh)
    {
        try
        {
            var principal = new JwtSecurityTokenHandler().ValidateToken(
                token,
                new TokenValidationParameters
                {
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    IssuerSigningKey = refresh ? _refreshKey : _accessKey,
                    ClockSkew = TimeSpan.FromSeconds(30),
                },
                out _);
            var sub = principal.FindFirstValue(JwtRegisteredClaimNames.Sub)
                ?? principal.FindFirstValue(ClaimTypes.NameIdentifier);
            return sub is null ? null : long.Parse(sub);
        }
        catch
        {
            return null;
        }
    }
}
