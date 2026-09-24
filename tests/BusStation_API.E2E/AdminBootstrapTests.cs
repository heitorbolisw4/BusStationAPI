using System.Net;
using System.Net.Http.Json;
using BusStation_API.Data;
using BusStation_API.E2E.Infrastructure;
using BusStation_API.Service;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace BusStation_API.E2E
{
    /// <summary>
    /// Seed do primeiro admin (BOOTSTRAP_ADMIN_EMAIL / BOOTSTRAP_ADMIN_PASSWORD).
    /// Chama o mesmo AdminBootstrapper que o startup usa, mas depois das migrations:
    /// no host de teste o startup roda antes do banco ser recriado.
    /// </summary>
    [Collection(E2ECollection.Name)]
    public class AdminBootstrapTests
    {
        private readonly BusStationApiFactory _factory;
        private readonly TestApi _api;

        public AdminBootstrapTests(BusStationApiFactory factory)
        {
            _factory = factory;
            _api = new TestApi(factory);
        }

        [Fact]
        public async Task Seed_creates_the_first_admin_only_when_there_is_none()
        {
            var bootstrapper = _factory.Services.GetRequiredService<AdminBootstrapper>();
            using(var scope = _factory.Services.CreateScope())
                await scope.ServiceProvider.GetRequiredService<AppDbContext>().Admins.ExecuteDeleteAsync();

            // sem as duas variáveis, não faz nada
            Assert.False(await bootstrapper.SeedFirstAdminAsync("root@e2e.test", null));
            Assert.False(await bootstrapper.SeedFirstAdminAsync(null, "root-pass-123"));

            var email = $"{TestApi.Unique("root")}@e2e.test";
            Assert.True(await bootstrapper.SeedFirstAdminAsync(email, "root-pass-123"));

            // já existe admin: um segundo boot (ou outra senha na variável) não cria nem altera nada
            Assert.False(await bootstrapper.SeedFirstAdminAsync($"{TestApi.Unique("other")}@e2e.test", "other-pass-123"));
            using(var scope = _factory.Services.CreateScope())
                Assert.Equal(1, await scope.ServiceProvider.GetRequiredService<AppDbContext>().Admins.CountAsync());

            // o admin semeado entra pelo login normal, com a senha da variável
            Assert.False(string.IsNullOrEmpty(await _api.AdminLogin(email, "root-pass-123")));
            await Expect.Status(
                await _api.Anonymous().PostAsJsonAsync("/admin/login", new { email, password = "wrong-pass" }),
                HttpStatusCode.Unauthorized);
        }
    }
}
