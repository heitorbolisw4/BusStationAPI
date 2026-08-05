namespace BusStation_API.Jwt
{
    public class JwtAdminOptions
    {
        public string SecretKey { get; set; } = string.Empty;
        public string Issuer { get; set; } = string.Empty;
        public string Audience { get; set; } = string.Empty;
        public int ExpirationTimeInMinutes { get; set; }
    }
}