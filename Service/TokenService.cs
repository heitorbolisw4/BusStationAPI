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
    public class TokenService
    {

        public string GenerateToken(IEnumerable<Claim> claims, string issuer, string audience, string key, TimeSpan expirationTimeInMinutes)
        {

            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
            var creds = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: DateTime.UtcNow.Add(expirationTimeInMinutes),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
            
        }
    }

}