using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using BusStation_API.Entities;
using BusStation_API.Interface;
using BusStation_API.Jwt;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace BusStation_API.Service
{
    public class TokenService : ITokenService
    {
        private readonly IConfiguration _config;
        private readonly JwtSettings _jwtSettings;

        public TokenService(IConfiguration config, IOptions<JwtSettings> jwtOptions)
        {
            _config = config;
            _jwtSettings = jwtOptions.Value;
        }

        public string GenerateToken(User user)
        {
            var secretKey = _jwtSettings.SecretKey;
            var issuer = _jwtSettings.Issuer;
            var audience = _jwtSettings.Audience;
            var expirationTimeInMinutes = _jwtSettings.ExpirationTimeInMinutes;


            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>()
            {
              new(ClaimTypes.NameIdentifier, user.Id.ToString()),
              new(ClaimTypes.Email, user.Email)  
            };


            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(expirationTimeInMinutes),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
            
        }
    }
}