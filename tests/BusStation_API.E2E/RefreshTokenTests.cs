using System.Net;
using System.Net.Http.Json;
using BusStation_API.Data;
using BusStation_API.E2E.Infrastructure;
using BusStation_API.Service;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace BusStation_API.E2E
{
    /// <summary>Sessão longa do cliente: login devolve refresh token, /refresh rotaciona, /logout revoga.</summary>
    [Collection(E2ECollection.Name)]
    public class RefreshTokenTests
    {
        private readonly BusStationApiFactory _factory;
        private readonly TestApi _api;

        public RefreshTokenTests(BusStationApiFactory factory)
        {
            _factory = factory;
            _api = new TestApi(factory);
        }

        private async Task<TokenPairDto> LoginNewCustomer()
        {
            var (email, password) = await _api.RegisterUser();
            var response = await _api.Anonymous().PostAsJsonAsync("/login", new { email, password });
            await Expect.Status(response, HttpStatusCode.OK);
            return (await response.Content.ReadFromJsonAsync<TokenPairDto>())!;
        }

        private Task<HttpResponseMessage> Refresh(string refreshToken) =>
            _api.Anonymous().PostAsJsonAsync("/refresh", new { refreshToken });

        [Fact]
        public async Task Login_returns_access_token_refresh_token_and_expiry()
        {
            var pair = await LoginNewCustomer();

            Assert.False(string.IsNullOrWhiteSpace(pair.Token));
            Assert.False(string.IsNullOrWhiteSpace(pair.RefreshToken));
            Assert.True(pair.ExpiresIn > 0);
        }

        [Fact]
        public async Task Refresh_returns_a_new_pair_whose_access_token_works()
        {
            var first = await LoginNewCustomer();

            var response = await Refresh(first.RefreshToken);
            await Expect.Status(response, HttpStatusCode.OK);
            var second = (await response.Content.ReadFromJsonAsync<TokenPairDto>())!;

            Assert.NotEqual(first.RefreshToken, second.RefreshToken);
            await Expect.Status(await _api.WithToken(second.Token).GetAsync("/tickets/list"), HttpStatusCode.OK);
        }

        [Fact]
        public async Task Reusing_a_rotated_refresh_token_returns_401_and_revokes_every_session_of_the_user()
        {
            var first = await LoginNewCustomer();
            var second = (await (await Refresh(first.RefreshToken)).Content.ReadFromJsonAsync<TokenPairDto>())!;

            // Alguém apresenta o token antigo (cópia vazada): recusa e derruba a família inteira.
            await Expect.Status(await Refresh(first.RefreshToken), HttpStatusCode.Unauthorized);
            await Expect.Status(await Refresh(second.RefreshToken), HttpStatusCode.Unauthorized);
        }

        [Theory]
        [InlineData("")]
        [InlineData("nao-existe")]
        public async Task Unknown_or_empty_refresh_token_returns_401(string refreshToken)
        {
            await Expect.Status(await Refresh(refreshToken), HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task Expired_refresh_token_returns_401()
        {
            var pair = await LoginNewCustomer();

            using(var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var hash = RefreshTokenService.Hash(pair.RefreshToken);
                await db.RefreshTokens.Where(t => t.TokenHash == hash)
                    .ExecuteUpdateAsync(s => s.SetProperty(t => t.ExpiresAt, DateTime.UtcNow.AddMinutes(-1)));
            }

            await Expect.Status(await Refresh(pair.RefreshToken), HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task Logout_revokes_the_refresh_token_and_is_idempotent()
        {
            var pair = await LoginNewCustomer();
            var client = _api.Anonymous();

            await Expect.Status(await client.PostAsJsonAsync("/logout", new { refreshToken = pair.RefreshToken }), HttpStatusCode.NoContent);
            await Expect.Status(await client.PostAsJsonAsync("/logout", new { refreshToken = pair.RefreshToken }), HttpStatusCode.NoContent);
            await Expect.Status(await client.PostAsJsonAsync("/logout", new { refreshToken = "nao-existe" }), HttpStatusCode.NoContent);

            await Expect.Status(await Refresh(pair.RefreshToken), HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task Refresh_token_is_stored_only_as_a_hash()
        {
            var pair = await LoginNewCustomer();

            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            Assert.False(await db.RefreshTokens.AnyAsync(t => t.TokenHash == pair.RefreshToken));
            Assert.True(await db.RefreshTokens.AnyAsync(t => t.TokenHash == RefreshTokenService.Hash(pair.RefreshToken)));
        }
    }
}
