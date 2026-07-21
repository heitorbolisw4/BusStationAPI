namespace BusStation_API.DTO.User
{
    public class UpdateUserRequestDto
    {
        public string Name { get; set; } = string.Empty;
        public int Age { get; set; }
        public string Email { get; set; } = string.Empty;
    }
}