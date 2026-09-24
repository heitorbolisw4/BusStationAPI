using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using BusStation_API.Data;
using BusStation_API.Entities;
using BusStation_API.Interface;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace BusStation_API.E2E.Infrastructure
{
    /// <summary>
    /// Dados que as migrations semeiam (AppDbContext.HasData). Os testes contam com eles
    /// em vez de recriar cidade/distância/preço em todo cenário.
    /// </summary>
    public static class Seed
    {
        public const int Indianopolis = 1;
        public const int Uberlandia = 2;
        public const int Araguari = 3;
        public const int Uberaba = 4;

        // Distance 1: Indianopolis -> Uberlandia, 60 km, preço/km 0,50 => passagem 30,00
        public const int IndianopolisToUberlandia = 1;
        // Distance 2: sentido contrário (distância é direcional — D-05)
        public const int UberlandiaToIndianopolis = 2;
        // Distance 5: Uberlandia -> Uberaba, 100 km, preço/km 0,90 => passagem 90,00
        public const int UberlandiaToUberaba = 5;
    }

    /// <summary>
    /// Atalhos para montar o cenário pela própria API (e não inserindo direto no banco),
    /// para que o "arrange" de cada teste passe pelo mesmo caminho que o usuário real.
    /// </summary>
    public class TestApi
    {
        private static int _dayOffset = 10;
        private readonly BusStationApiFactory _factory;

        public TestApi(BusStationApiFactory factory)
        {
            _factory = factory;
        }

        public HttpClient Anonymous() => _factory.CreateClient();

        public HttpClient WithToken(string token)
        {
            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return client;
        }

        public static string Unique(string prefix) => $"{prefix}{Guid.NewGuid().ToString("N")[..8]}";

        // Cada chamada devolve um dia diferente no futuro: a busca filtra por data, então
        // dois testes nunca enxergam os embarques um do outro.
        public static DateOnly UniqueFutureDate() =>
            DateOnly.FromDateTime(DateTime.Today).AddDays(Interlocked.Increment(ref _dayOffset));

        public async Task<(string Email, string Password)> RegisterUser(int age = 25)
        {
            var email = $"{Unique("user")}@e2e.test";
            const string password = "secret123";

            var response = await Anonymous().PostAsJsonAsync("/register",
                new { name = "E2E User", email, password, age });
            await Expect.Status(response, HttpStatusCode.Created);

            return (email, password);
        }

        public async Task<string> Login(string email, string password)
        {
            var response = await Anonymous().PostAsJsonAsync("/login", new { email, password });
            await Expect.Status(response, HttpStatusCode.OK);
            return (await response.Content.ReadFromJsonAsync<TokenResponse>())!.Token;
        }

        public async Task<HttpClient> NewCustomer()
        {
            var (email, password) = await RegisterUser();
            return WithToken(await Login(email, password));
        }

        /// <summary>
        /// Cria o admin pela API, mas emite o token direto pelo ITokenService da aplicação:
        /// POST /admin/login está quebrado (BUG-013) e não pode ser pré-requisito de todo
        /// teste administrativo.
        /// </summary>
        public async Task<HttpClient> NewAdmin()
        {
            var email = $"{Unique("admin")}@e2e.test";
            var response = await Anonymous().PostAsJsonAsync("/admin/create",
                new { name = "E2E Admin", email, password = "admin123" });
            await Expect.Status(response, HttpStatusCode.Created);

            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var admin = await db.Admins.SingleAsync(a => a.Email == email);
            var token = scope.ServiceProvider.GetRequiredService<ITokenService<Admin>>().GenerateToken(admin);

            return WithToken(token);
        }

        public async Task<CityDto> CreateCity()
        {
            var cityName = Unique("City ");
            // Acronym tem índice único e no máximo 5 caracteres
            var acronym = Guid.NewGuid().ToString("N")[..5].ToUpperInvariant();
            var client = Anonymous();

            var response = await client.PostAsJsonAsync("/cities/create", new { cityName, state = "MG", acronym });
            await Expect.Status(response, HttpStatusCode.Created);

            var cities = await client.GetFromJsonAsync<List<CityDto>>("/cities/list");
            return cities!.Single(c => c.CityName == cityName);
        }

        /// <summary>
        /// Monta um trecho novo (2 cidades + distância + preço/km) só para o teste que pediu,
        /// para cenários que mexem em preço não contaminarem os trechos semeados.
        /// </summary>
        public async Task<(CityDto Origin, CityDto Destination, DistanceDto Distance)> CreateLeg(
            HttpClient admin, int kilometers, float pricePerKm)
        {
            var origin = await CreateCity();
            var destination = await CreateCity();

            var created = await admin.PutAsJsonAsync("/distances/create",
                new { originCityId = origin.Id, destinationCityId = destination.Id, kilometers });
            await Expect.Status(created, HttpStatusCode.Created);

            var distances = await admin.GetFromJsonAsync<List<DistanceDto>>("/distances/list");
            var distance = distances!.Single(d => d.OriginCityId == origin.Id && d.DestinationCityId == destination.Id);

            var price = await admin.PostAsJsonAsync("/prices/create", new { distanceId = distance.Id, pricePerKm });
            await Expect.Status(price, HttpStatusCode.Created);

            return (origin, destination, distance);
        }

        public async Task<RouteDto> CreateRoute(int distanceId)
        {
            var routeName = Unique("R-");
            var client = Anonymous();

            var response = await client.PostAsJsonAsync("/routes/create", new { routeName, distanceId });
            await Expect.Status(response, HttpStatusCode.Created);

            // POST /routes/create não devolve o recurso criado; o cliente precisa listar.
            var routes = await client.GetFromJsonAsync<List<RouteDto>>("/routes/list");
            return routes!.Single(r => r.RouteName == routeName);
        }

        public async Task<BoardingCreatedDto> CreateBoarding(int routeId, DateOnly date, TimeOnly time, int seats = 40)
        {
            var response = await Anonymous().PostAsJsonAsync("/boardings/create",
                new { routeId, seats, boardingDate = date, boardingTime = time });
            await Expect.Status(response, HttpStatusCode.Created);
            return (await response.Content.ReadFromJsonAsync<BoardingCreatedDto>())!;
        }

        public async Task<List<SearchBoardingDto>> Search(int originCityId, int destinationCityId, DateOnly date)
        {
            var response = await Anonymous().GetAsync(
                $"/boardings/search?originCityId={originCityId}&destinationCityId={destinationCityId}&date={date:yyyy-MM-dd}");
            await Expect.Status(response, HttpStatusCode.OK);
            return (await response.Content.ReadFromJsonAsync<List<SearchBoardingDto>>())!;
        }
    }

    public static class Expect
    {
        /// <summary>
        /// Assert de status que, ao falhar, mostra o corpo da resposta — sem isso um
        /// 500 vira só "Expected 201, got 500" e a investigação começa no escuro.
        /// </summary>
        public static async Task Status(HttpResponseMessage response, HttpStatusCode expected)
        {
            if(response.StatusCode == expected)
                return;

            var body = await response.Content.ReadAsStringAsync();
            Assert.Fail(
                $"{response.RequestMessage?.Method} {response.RequestMessage?.RequestUri?.PathAndQuery}: " +
                $"esperado {(int)expected} {expected}, veio {(int)response.StatusCode} {response.StatusCode}.\n" +
                $"Corpo: {(body.Length > 2000 ? body[..2000] + "..." : body)}");
        }
    }
}
