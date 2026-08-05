using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using BusStation_API.Entities;
using BusStation_API.Interface;
using BusStation_API.Jwt;
using Microsoft.Extensions.Options;

namespace BusStation_API.Service
{
    public class AdminTokenService : ITokenService<Admin>
    {
        private readonly TokenService _tokenService;
        private readonly JwtAdminOptions _options;

        public AdminTokenService(TokenService generator, IOptions<JwtAdminOptions> options)
        {
            _tokenService = generator;
            _options = options.Value;
        }

        public string GenerateToken(Admin Admin)
        {
            var claims = new List<Claim>()
            {
                new(JwtRegisteredClaimNames.Sub, Admin.Id.ToString()),
                new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new("adm", Admin.AccessLevel.ToString())
            };
            return _tokenService.GenerateToken(claims,
             _options.Issuer,
              _options.Audience,
               _options.SecretKey,
                TimeSpan.FromMinutes(5));
        }


    }
}