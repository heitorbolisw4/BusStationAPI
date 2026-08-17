namespace BusStation_API.DTO
{
    public record RegisterRequest(string Name, string Email, string Password, int Age);
    public record LoginRequest(string Email, string Password);

    //admin
    public record AdminRegisterRequest(string Name, string Email, string Password);
    public record AdminLoginRequest(string Email, string Password);
}