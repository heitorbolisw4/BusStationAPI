using BusStation_API.Entities;

namespace BusStation_API.Interface
{
    interface IAuthService
    {
        string GenerateHash(string InputPassword);
        bool PasswordVerify(string InputPassword, string PasswordDb); 
    }
}