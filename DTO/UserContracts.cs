namespace BusStation_API.DTO
{
    public class UserContracts
    {
        public record UserProfileResponse(int Id, string Name, string Email, int Age);
        public record UpdateUserRequest(string Name, string Email, int Age);
        public record UpdatePasswordRequest(string Password, string NewPassword);

        public record UpdateEmailRequest(string Email, string Password);
    }
}
