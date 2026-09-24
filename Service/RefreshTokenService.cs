using System.Security.Cryptography;
using System.Text;
using BusStation_API.Data;
using BusStation_API.Entities;
using BusStation_API.Jwt;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace BusStation_API.Service
{
    /// <summary>
    /// Emissão, rotação e revogação de refresh tokens de usuário.
    ///
    /// Rotação com detecção de reuso: todo refresh revoga o token usado e emite outro.
    /// Se um token já revogado for apresentado de novo, alguém está usando uma cópia
    /// (vazou, ou o cliente fez dois refresh em paralelo com o mesmo token) e todas as
    /// sessões ativas do usuário são revogadas, obrigando novo login.
    /// </summary>
    public class RefreshTokenService
    {
        public const int DefaultLifetimeMinutes = 60 * 24 * 7;

        private readonly AppDbContext _db;
        private readonly TimeSpan _lifetime;
        private readonly ILogger<RefreshTokenService> _logger;

        public RefreshTokenService(AppDbContext db, IOptions<JwtUserOptions> options, ILogger<RefreshTokenService> logger)
        {
            _db = db;
            _logger = logger;
            var minutes = options.Value.RefreshExpirationTimeInMinutes;
            _lifetime = TimeSpan.FromMinutes(minutes > 0 ? minutes : DefaultLifetimeMinutes);
        }

        /// <returns>O valor em claro, que só existe na resposta ao cliente.</returns>
        public async Task<string> IssueAsync(int userId)
        {
            var raw = WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(64));
            var now = DateTime.UtcNow;

            _db.RefreshTokens.Add(new RefreshToken
            {
                UserId = userId,
                TokenHash = Hash(raw),
                CreatedAt = now,
                ExpiresAt = now.Add(_lifetime)
            });
            await _db.SaveChangesAsync();

            return raw;
        }

        /// <summary>
        /// Troca um refresh token válido por um novo. Devolve null (e o cliente recebe 401)
        /// se o token não existe, expirou ou já foi usado.
        /// </summary>
        public async Task<(User User, string RefreshToken)?> RotateAsync(string raw)
        {
            if(string.IsNullOrWhiteSpace(raw))
                return null;

            var hash = Hash(raw);
            var stored = await _db.RefreshTokens.Include(t => t.User).SingleOrDefaultAsync(t => t.TokenHash == hash);
            if(stored?.User is null)
                return null;

            var now = DateTime.UtcNow;

            if(stored.RevokedAt is not null)
            {
                await RevokeAllForUserAsync(stored.UserId, now);
                _logger.LogWarning("Refresh token reutilizado para o usuário {UserId}; todas as sessões foram revogadas.", stored.UserId);
                return null;
            }

            if(stored.ExpiresAt <= now)
                return null;

            // Revogação atômica: se dois refresh com o mesmo token chegarem juntos, só um
            // consegue marcar RevokedAt. O outro cai aqui como reuso.
            var revoked = await _db.RefreshTokens
                .Where(t => t.Id == stored.Id && t.RevokedAt == null)
                .ExecuteUpdateAsync(s => s.SetProperty(t => t.RevokedAt, now));
            if(revoked == 0)
            {
                await RevokeAllForUserAsync(stored.UserId, now);
                _logger.LogWarning("Refresh token usado em paralelo para o usuário {UserId}; todas as sessões foram revogadas.", stored.UserId);
                return null;
            }

            return (stored.User, await IssueAsync(stored.UserId));
        }

        /// <summary>Logout: revoga o token informado. Token desconhecido não é erro.</summary>
        public async Task RevokeAsync(string raw)
        {
            if(string.IsNullOrWhiteSpace(raw))
                return;

            var hash = Hash(raw);
            await _db.RefreshTokens
                .Where(t => t.TokenHash == hash && t.RevokedAt == null)
                .ExecuteUpdateAsync(s => s.SetProperty(t => t.RevokedAt, DateTime.UtcNow));
        }

        private Task<int> RevokeAllForUserAsync(int userId, DateTime now) =>
            _db.RefreshTokens
                .Where(t => t.UserId == userId && t.RevokedAt == null)
                .ExecuteUpdateAsync(s => s.SetProperty(t => t.RevokedAt, now));

        // O token já tem 512 bits aleatórios, então SHA-256 puro basta: não há o que
        // proteger contra dicionário, ao contrário de senha (que usa BCrypt).
        public static string Hash(string raw) =>
            Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(raw)));
    }
}
