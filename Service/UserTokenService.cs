using System.Security.Claims;
using BusStation_API.Entities;
using BusStation_API.Interface;
using BusStation_API.Jwt;
using Microsoft.Extensions.Options;

namespace BusStation_API.Service
{
    public class UserTokenService : ITokenService<User>
    {
        private readonly TokenService _tokenService;
        private readonly JwtUserOptions _options;

        public UserTokenService(TokenService tokenService, IOptions<JwtUserOptions> options)
        {
            _tokenService = tokenService;
            _options = options.Value;
        }

        public string GenerateToken(User user)
        {
            var claims = new List<Claim>()
            {
                new(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new(ClaimTypes.Email, user.Email)
            };

            return _tokenService.GenerateToken(claims,
             _options.Issuer,
              _options.Audience,
               _options.SecretKey,
                TimeSpan.FromMinutes(5));
        }
    }

}