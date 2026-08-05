using BusStation_API.Entities;

namespace BusStation_API.Interface
{
    public interface ITokenService<TPrincipal>
    {
        //string GenerateToken(User user);
        string GenerateToken(TPrincipal principal);
    }
}