using System.Net;
using System.Net.Http.Json;
using BusStation_API.E2E.Infrastructure;

namespace BusStation_API.E2E
{
    /// <summary>
    /// GET /boardings/search — a tela inicial do site (src/api/boardings.js no front).
    /// </summary>
    [Collection(E2ECollection.Name)]
    public class BoardingSearchTests
    {
        private readonly TestApi _api;

        public BoardingSearchTests(BusStationApiFactory factory)
        {
            _api = new TestApi(factory);
        }

        [Theory]
        [InlineData(0, Seed.Uberlandia)]
        [InlineData(Seed.Indianopolis, -1)]
        [InlineData(Seed.Uberlandia, Seed.Uberlandia)]
        public async Task Invalid_city_pair_returns_400(int originCityId, int destinationCityId)
        {
            var date = TestApi.UniqueFutureDate();

            var response = await _api.Anonymous().GetAsync(
                $"/boardings/search?originCityId={originCityId}&destinationCityId={destinationCityId}&date={date:yyyy-MM-dd}");

            await Expect.Status(response, HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task Past_date_returns_400()
        {
            var yesterday = DateOnly.FromDateTime(DateTime.Today).AddDays(-1);

            var response = await _api.Anonymous().GetAsync(
                $"/boardings/search?originCityId={Seed.Indianopolis}&destinationCityId={Seed.Uberlandia}&date={yesterday:yyyy-MM-dd}");

            await Expect.Status(response, HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task Missing_query_parameters_return_400()
        {
            var response = await _api.Anonymous().GetAsync("/boardings/search?originCityId=1");

            await Expect.Status(response, HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task Day_without_departures_returns_200_with_empty_list()
        {
            Assert.Empty(await _api.Search(Seed.Indianopolis, Seed.Uberlandia, TestApi.UniqueFutureDate()));
        }

        [Fact]
        public async Task Returns_only_that_direction_and_day_ordered_by_departure_time()
        {
            var date = TestApi.UniqueFutureDate();
            var outbound = await _api.CreateRoute(Seed.IndianopolisToUberlandia);
            var inbound = await _api.CreateRoute(Seed.UberlandiaToIndianopolis);

            var late = await _api.CreateBoarding(outbound.Id, date, new TimeOnly(18, 0));
            var early = await _api.CreateBoarding(outbound.Id, date, new TimeOnly(6, 15));
            await _api.CreateBoarding(outbound.Id, date.AddDays(1), new TimeOnly(9, 0)); // outro dia
            await _api.CreateBoarding(inbound.Id, date, new TimeOnly(9, 0));             // sentido contrário

            var departures = await _api.Search(Seed.Indianopolis, Seed.Uberlandia, date);

            Assert.Equal(new[] { early.Id, late.Id }, departures.Select(d => d.BoardingId));
        }

        [Fact]
        public async Task Response_has_every_field_the_frontend_card_reads()
        {
            var date = TestApi.UniqueFutureDate();
            var route = await _api.CreateRoute(Seed.UberlandiaToUberaba);
            await _api.CreateBoarding(route.Id, date, new TimeOnly(7, 45), seats: 12);

            // JSON cru: garante os nomes em camelCase que o front usa, sem depender
            // de desserialização case-insensitive mascarar uma mudança de contrato.
            var json = await _api.Anonymous().GetStringAsync(
                $"/boardings/search?originCityId={Seed.Uberlandia}&destinationCityId={Seed.Uberaba}&date={date:yyyy-MM-dd}");

            foreach(var field in new[] { "boardingId", "routeId", "routeName", "originCity", "originAcronym",
                                         "destinationCity", "destinationAcronym", "kilometers",
                                         "boardingDate", "boardingTime", "seats", "price" })
                Assert.Contains($"\"{field}\":", json);

            var departure = Assert.Single(System.Text.Json.JsonSerializer.Deserialize<List<SearchBoardingDto>>(json,
                new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web))!);
            Assert.Equal("Udia", departure.OriginAcronym);
            Assert.Equal("Ura", departure.DestinationAcronym);
            Assert.Equal(100, departure.Kilometers);
            Assert.Equal(90f, departure.Price, 0.001f); // 100 km x 0,90/km
        }

        [Fact]
        public async Task Departure_already_gone_today_is_not_offered()
        {
            // Só é determinístico longe da meia-noite; perto dela, "1 minuto atrás"
            // cai no dia anterior e o create rejeita a data.
            var now = DateTime.Now;
            if(now.TimeOfDay < TimeSpan.FromMinutes(5))
                return;

            var today = DateOnly.FromDateTime(now);
            var route = await _api.CreateRoute(Seed.UberlandiaToIndianopolis);
            var gone = await _api.CreateBoarding(route.Id, today, TimeOnly.FromDateTime(now.AddMinutes(-1)));

            var departures = await _api.Search(Seed.Uberlandia, Seed.Indianopolis, today);

            Assert.DoesNotContain(departures, d => d.BoardingId == gone.Id);
        }

        [Fact]
        public async Task Creating_a_boarding_validates_route_seats_and_date()
        {
            var route = await _api.CreateRoute(Seed.IndianopolisToUberlandia);
            var client = _api.Anonymous();
            var date = TestApi.UniqueFutureDate();

            var noSeats = await client.PostAsJsonAsync("/boardings/create",
                new { routeId = route.Id, seats = 0, boardingDate = date, boardingTime = new TimeOnly(8, 0) });
            var pastDate = await client.PostAsJsonAsync("/boardings/create",
                new { routeId = route.Id, seats = 10, boardingDate = DateOnly.FromDateTime(DateTime.Today).AddDays(-1), boardingTime = new TimeOnly(8, 0) });
            var unknownRoute = await client.PostAsJsonAsync("/boardings/create",
                new { routeId = 999_999, seats = 10, boardingDate = date, boardingTime = new TimeOnly(8, 0) });

            await Expect.Status(noSeats, HttpStatusCode.BadRequest);
            await Expect.Status(pastDate, HttpStatusCode.BadRequest);
            await Expect.Status(unknownRoute, HttpStatusCode.NotFound);
        }
    }
}
