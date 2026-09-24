namespace BusStation_API.Entities
{
    /// <summary>
    /// Sessão de longa duração de um usuário. O valor entregue ao cliente nunca é salvo:
    /// só o hash SHA-256 (TokenHash), para que um vazamento do banco não entregue sessões.
    /// Cada refresh revoga o token usado e emite outro (rotação).
    /// </summary>
    public class RefreshToken
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public User? User { get; set; }

        public string TokenHash { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime ExpiresAt { get; set; }
        public DateTime? RevokedAt { get; set; }
    }
}
