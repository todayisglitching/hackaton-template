using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Text;

namespace testASP.Services;

public static class JwtHelpers
{
    public static int? TryGetUserIdFromToken(string token, string secret)
    {
        try
        {
            var handler = new JwtSecurityTokenHandler();
            var validation = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(secret)),
                ValidateIssuer = false,
                ValidateAudience = false
            };

            var principal = handler.ValidateToken(token, validation, out _);
            var idClaim = principal.Claims.First(x => x.Type == "id").Value;
            return int.Parse(idClaim);
        }
        catch
        {
            return null;
        }
    }
}
