using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using BusStation_API.Entities;
using BusStation_API.Interface;
using Microsoft.IdentityModel.Tokens;

namespace BusStation_API.Service
{
    public class TokenService : ITokenService
    {
        private readonly IConfiguration _config;

        public TokenService(IConfiguration config)
        {
            _config = config;
        }

        public string GenerateToken(User user)
        {
            var jwtSettings = _config.GetSection("JwtSettings");
            var secretKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtSettings["SecretKey"] ?? string.Empty));

            var claims = new List<Claim>()
            {
              new(ClaimTypes.NameIdentifier, user.Id.ToString()),
              new(ClaimTypes.Email, user.Email)  
            };
            var ExpirationTimeInMinutes = jwtSettings.GetValue<int>("ExpirationTimeInMinutes");


            var token = new JwtSecurityToken(
                issuer: jwtSettings.GetValue<string>("Issuer"),
                audience: jwtSettings.GetValue<string>("Audience"),
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(ExpirationTimeInMinutes),
                signingCredentials: new SigningCredentials(secretKey, SecurityAlgorithms.HmacSha256)
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
            
        }
    }
}