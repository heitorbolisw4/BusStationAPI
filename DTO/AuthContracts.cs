namespace BusStation_API.DTO
{
    public record RegisterRequest(string Name, string Email, string Password, int Age);
    public record LoginRequest(string Email, string Password);
    public record RefreshRequest(string RefreshToken);
    // ExpiresIn: segundos até o access token expirar, para o cliente renovar antes.
    public record TokenPairResponse(string Token, string RefreshToken, int ExpiresIn);

    //admin
    public record AdminRegisterRequest(string Name, string Email, string Password);
    public record AdminLoginRequest(string Email, string Password);

    //user
    public record DriverProfileResponse(string Name, string Email, int Age);
}