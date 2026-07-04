using BusStation_API.Entities;
using BusStation_API.Interface;

namespace BusStation_API.Service
{
    public class AuthService : IAuthService
    {
        public string GenerateHash(string InputPassword)
        {
            var hash  = BCrypt.Net.BCrypt.HashPassword(InputPassword);
            return hash;

        }
        public bool PasswordVerify(string InputPassword, string PasswordDb)
        {
            return BCrypt.Net.BCrypt.Verify(InputPassword, PasswordDb);
        }


    }
}