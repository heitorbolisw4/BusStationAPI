namespace BusStation_API.DTO.User
{
    public class UpdatePasswRequestDto
    {
        public string Password { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
        
    }
}