using BusStation_API.Entities;

namespace BusStation_API.Interface
{
    public interface ITokenService
    {
        string GenerateToken(User user);
    }
}