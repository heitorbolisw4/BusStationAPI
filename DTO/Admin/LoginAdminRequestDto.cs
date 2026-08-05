namespace BusStation_API.DTO.Admin
{
    public class LoginAdminRequestDto
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}