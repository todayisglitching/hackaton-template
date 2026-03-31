using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace testASP.Services;

public sealed class JwtTokenService
{
    private readonly byte[] _jwtKey;

    public JwtTokenService(string secret)
    {
        _jwtKey = Encoding.ASCII.GetBytes(secret);
    }

    public string CreateToken(int userId, TimeSpan lifetime)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[] { new Claim("id", userId.ToString()) }),
            Expires = DateTime.UtcNow.Add(lifetime),
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(_jwtKey), SecurityAlgorithms.HmacSha256Signature)
        };

        return tokenHandler.WriteToken(tokenHandler.CreateToken(tokenDescriptor));
    }
}
