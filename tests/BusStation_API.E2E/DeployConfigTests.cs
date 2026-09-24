using System.Net;
using BusStation_API.Configuration;
using BusStation_API.E2E.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.Internal;

namespace BusStation_API.E2E
{
    /// <summary>
    /// Configuração de deploy (CHORE-022): health check, CORS, Swagger e fail-fast.
    /// </summary>
    [Collection(E2ECollection.Name)]
    public class DeployConfigTests
    {
        private const string ViteDev = "http://localhost:5173";
        private readonly BusStationApiFactory _factory;
        private readonly TestApi _api;

        public DeployConfigTests(BusStationApiFactory factory)
        {
            _factory = factory;
            _api = new TestApi(factory);
        }

        [Fact]
        public async Task Health_liveness_returns_200()
        {
            var response = await _api.Anonymous().GetAsync("/health");

            await Expect.Status(response, HttpStatusCode.OK);
            Assert.Equal("Healthy", await response.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task Health_ready_returns_200_when_postgres_is_reachable()
        {
            var response = await _api.Anonymous().GetAsync("/health/ready");

            await Expect.Status(response, HttpStatusCode.OK);
            Assert.Equal("Healthy", await response.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task Swagger_json_is_served_outside_production()
        {
            await Expect.Status(await _api.Anonymous().GetAsync("/swagger/v1/swagger.json"), HttpStatusCode.OK);
        }

        [Fact]
        public async Task Development_without_config_allows_only_the_local_vite_origin()
        {
            var client = _api.Anonymous();

            var allowed = await client.SendAsync(Preflight(ViteDev));
            var denied = await client.SendAsync(Preflight("https://evil.example"));

            AssertAllowed(allowed, ViteDev);
            Assert.False(denied.Headers.Contains("Access-Control-Allow-Origin"));
        }

        [Fact]
        public async Task Configured_origins_are_allowed_and_everything_else_is_not()
        {
            // valor único separado por vírgula, como fica cadastrado no painel do Render
            using var configured = _factory.WithWebHostBuilder(b =>
                b.UseSetting("Cors:AllowedOrigins", "https://busstation.vercel.app, https://outro.example/"));
            var client = configured.CreateClient();

            AssertAllowed(await client.SendAsync(Preflight("https://busstation.vercel.app")), "https://busstation.vercel.app");
            AssertAllowed(await client.SendAsync(Preflight("https://outro.example")), "https://outro.example");

            // com origem configurada, o fallback de dev deixa de valer
            foreach(var origin in new[] { ViteDev, "https://preview-123.vercel.app" })
            {
                var denied = await client.SendAsync(Preflight(origin));
                Assert.False(denied.Headers.Contains("Access-Control-Allow-Origin"), $"{origin} não deveria ser liberada");
            }

            // request simples (não-preflight) também não recebe o header
            var simple = new HttpRequestMessage(HttpMethod.Get, "/cities/list");
            simple.Headers.Add("Origin", "https://evil.example");
            var response = await client.SendAsync(simple);
            await Expect.Status(response, HttpStatusCode.OK);
            Assert.False(response.Headers.Contains("Access-Control-Allow-Origin"));
        }

        [Fact]
        public void Startup_fails_fast_listing_every_missing_setting()
        {
            var empty = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["JwtSettings:User:SecretKey"] = "curta",
            }).Build();

            var ex = Assert.Throws<InvalidOperationException>(() =>
                StartupConfig.EnsureRequiredSettings(empty, Env(Environments.Staging)));

            Assert.Contains("ConnectionStrings__DefaultConnection", ex.Message);
            Assert.Contains("JwtSettings__User__SecretKey precisa ter pelo menos 32 bytes", ex.Message);
            Assert.Contains("JwtSettings__Admin__SecretKey", ex.Message);
            Assert.Contains("Cors__AllowedOrigins", ex.Message);
        }

        [Fact]
        public void Complete_production_config_passes_the_startup_check()
        {
            var key = new string('k', 32);
            var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = "Host=db;Database=x;Username=u;Password=p",
                ["JwtSettings:User:SecretKey"] = key,
                ["JwtSettings:Admin:SecretKey"] = key,
                ["Cors:AllowedOrigins:0"] = "https://a.example",
                ["Cors:AllowedOrigins:1"] = "https://b.example",
            }).Build();

            StartupConfig.EnsureRequiredSettings(config, Env(Environments.Production));
            Assert.Equal(new[] { "https://a.example", "https://b.example" },
                StartupConfig.AllowedOrigins(config, Env(Environments.Production)));
        }

        private static HttpRequestMessage Preflight(string origin)
        {
            var request = new HttpRequestMessage(HttpMethod.Options, "/boardings/search");
            request.Headers.Add("Origin", origin);
            request.Headers.Add("Access-Control-Request-Method", "GET");
            request.Headers.Add("Access-Control-Request-Headers", "authorization,content-type");
            return request;
        }

        private static void AssertAllowed(HttpResponseMessage response, string origin)
        {
            Assert.True(response.Headers.TryGetValues("Access-Control-Allow-Origin", out var values),
                $"preflight de {origin} sem Access-Control-Allow-Origin (status {(int)response.StatusCode})");
            Assert.Equal(origin, Assert.Single(values));
            Assert.True(response.Headers.Contains("Access-Control-Allow-Methods"));
            Assert.True(response.Headers.Contains("Access-Control-Allow-Headers"));
        }

        private static IHostEnvironment Env(string name) => new HostingEnvironment { EnvironmentName = name };
    }
}
