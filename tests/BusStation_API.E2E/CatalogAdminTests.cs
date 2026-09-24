using System.Net;
using System.Net.Http.Json;
using BusStation_API.E2E.Infrastructure;

namespace BusStation_API.E2E
{
    /// <summary>
    /// Lado da operação: montar o catálogo (cidade → distância → preço/km → rota → embarque)
    /// e quem pode mexer em quê.
    /// </summary>
    [Collection(E2ECollection.Name)]
    public class CatalogAdminTests
    {
        private readonly TestApi _api;

        public CatalogAdminTests(BusStationApiFactory factory)
        {
            _api = new TestApi(factory);
        }

        [Fact]
        public async Task Admin_builds_a_new_leg_and_it_becomes_searchable_with_the_computed_fare()
        {
            var admin = await _api.NewAdmin();
            var leg = await _api.CreateLeg(admin, kilometers: 150, pricePerKm: 0.40f);

            var route = await _api.CreateRoute(leg.Distance.Id);
            Assert.Equal(150, route.Kilometers);

            var date = TestApi.UniqueFutureDate();
            var boarding = await _api.CreateBoarding(route.Id, date, new TimeOnly(22, 0), seats: 44);

            var departure = Assert.Single(await _api.Search(leg.Origin.Id, leg.Destination.Id, date));
            Assert.Equal(boarding.Id, departure.BoardingId);
            Assert.Equal(leg.Origin.CityName, departure.OriginCity);
            Assert.Equal(leg.Destination.Acronym, departure.DestinationAcronym);
            Assert.Equal(44, departure.Seats);
            Assert.Equal(60f, departure.Price, 0.001f); // 150 km x 0,40/km
        }

        [Fact]
        public async Task Route_uses_the_most_recent_price_per_km_of_its_distance()
        {
            var admin = await _api.NewAdmin();
            var leg = await _api.CreateLeg(admin, kilometers: 10, pricePerKm: 1f);
            await Expect.Status(
                await admin.PostAsJsonAsync("/prices/create", new { distanceId = leg.Distance.Id, pricePerKm = 3f }),
                HttpStatusCode.Created);

            var route = await _api.CreateRoute(leg.Distance.Id);
            var date = TestApi.UniqueFutureDate();
            await _api.CreateBoarding(route.Id, date, new TimeOnly(9, 0));

            Assert.Equal(30f, Assert.Single(await _api.Search(leg.Origin.Id, leg.Destination.Id, date)).Price, 0.001f);
        }

        [Fact]
        public async Task Route_creation_validates_name_distance_and_duplicates()
        {
            var client = await _api.Admin();
            var route = await _api.CreateRoute(Seed.IndianopolisToUberlandia);

            var shortName = await client.PostAsJsonAsync("/routes/create", new { routeName = "abc", distanceId = Seed.IndianopolisToUberlandia });
            var unknownDistance = await client.PostAsJsonAsync("/routes/create", new { routeName = TestApi.Unique("R-"), distanceId = 999_999 });
            var duplicate = await client.PostAsJsonAsync("/routes/create", new { routeName = route.RouteName, distanceId = Seed.IndianopolisToUberlandia });

            await Expect.Status(shortName, HttpStatusCode.BadRequest);
            await Expect.Status(unknownDistance, HttpStatusCode.NotFound);
            await Expect.Status(duplicate, HttpStatusCode.Conflict);

            await Expect.Status(await client.GetAsync($"/routes/list/{route.Id}"), HttpStatusCode.OK);
            await Expect.Status(await client.GetAsync("/routes/list/999999"), HttpStatusCode.NotFound);
        }

        [Theory]
        [InlineData("GET", "/distances/list")]
        [InlineData("PUT", "/distances/create")]
        [InlineData("GET", "/prices/list")]
        [InlineData("POST", "/prices/create")]
        [InlineData("POST", "/admin/create")]
        [InlineData("POST", "/cities/create")]
        [InlineData("PUT", "/cities/update/1")]
        [InlineData("DELETE", "/cities/delete/1")]
        [InlineData("POST", "/routes/create")]
        [InlineData("GET", "/routes/list")]
        [InlineData("GET", "/routes/list/1")]
        [InlineData("PATCH", "/routes/update/1")]
        [InlineData("POST", "/boardings/create")]
        public async Task Admin_only_endpoints_reject_anonymous_and_customer_tokens(string method, string path)
        {
            var customer = await _api.NewCustomer();

            var asAnonymous = await _api.Anonymous().SendAsync(new HttpRequestMessage(new HttpMethod(method), path));
            var asCustomer = await customer.SendAsync(new HttpRequestMessage(new HttpMethod(method), path));

            await Expect.Status(asAnonymous, HttpStatusCode.Unauthorized);
            // AdminPolicy autentica pelos dois schemes: o cliente é reconhecido (token
            // válido do UserScheme), mas não tem a claim "adm". Autenticado sem permissão: 403.
            await Expect.Status(asCustomer, HttpStatusCode.Forbidden);
        }

        [Fact]
        public async Task Admin_token_opens_the_catalog_write_endpoints()
        {
            var admin = await _api.Admin();
            var city = await _api.CreateCity();

            var route = await _api.CreateRoute(Seed.IndianopolisToUberlandia);
            await Expect.Status(await admin.GetAsync($"/routes/list/{route.Id}"), HttpStatusCode.OK);
            await Expect.Status(await admin.PatchAsJsonAsync($"/routes/update/{route.Id}", new { price = 1f }), HttpStatusCode.OK);
            await _api.CreateBoarding(route.Id, TestApi.UniqueFutureDate(), new TimeOnly(12, 0));

            await Expect.Status(
                await admin.PutAsJsonAsync($"/cities/update/{city.Id}", new { cityName = TestApi.Unique("Adm "), state = "GO", acronym = city.Acronym }),
                HttpStatusCode.NoContent);
            await Expect.Status(await admin.DeleteAsync($"/cities/delete/{city.Id}"), HttpStatusCode.NoContent);
        }

        [Theory]
        [InlineData("/cities/list")]
        [InlineData("/boardings/search?originCityId=1&destinationCityId=2&date=2099-01-01")]
        public async Task Public_catalog_reads_work_without_a_token(string path)
        {
            await Expect.Status(await _api.Anonymous().GetAsync(path), HttpStatusCode.OK);

            // e continuam abertos para quem está logado como cliente
            var customer = await _api.NewCustomer();
            await Expect.Status(await customer.GetAsync(path), HttpStatusCode.OK);
        }

        [Fact]
        public async Task Distance_and_price_creation_validate_input()
        {
            var admin = await _api.NewAdmin();

            var zeroKm = await admin.PutAsJsonAsync("/distances/create",
                new { originCityId = Seed.Indianopolis, destinationCityId = Seed.Uberaba, kilometers = 0 });
            var sameCity = await admin.PutAsJsonAsync("/distances/create",
                new { originCityId = Seed.Uberaba, destinationCityId = Seed.Uberaba, kilometers = 10 });
            var unknownCity = await admin.PutAsJsonAsync("/distances/create",
                new { originCityId = 999_999, destinationCityId = Seed.Uberaba, kilometers = 10 });
            var negativePrice = await admin.PostAsJsonAsync("/prices/create",
                new { distanceId = Seed.IndianopolisToUberlandia, pricePerKm = -1f });
            var priceForUnknownDistance = await admin.PostAsJsonAsync("/prices/create",
                new { distanceId = 999_999, pricePerKm = 1f });

            await Expect.Status(zeroKm, HttpStatusCode.BadRequest);
            await Expect.Status(sameCity, HttpStatusCode.Conflict);
            await Expect.Status(unknownCity, HttpStatusCode.NotFound);
            await Expect.Status(negativePrice, HttpStatusCode.BadRequest);
            await Expect.Status(priceForUnknownDistance, HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task Admin_create_rejects_blank_fields_and_duplicate_email()
        {
            var client = await _api.NewAdmin();
            var email = $"{TestApi.Unique("admin")}@e2e.test";

            var blank = await client.PostAsJsonAsync("/admin/create", new { name = "", email, password = "admin123" });
            var created = await client.PostAsJsonAsync("/admin/create", new { name = "Adm", email, password = "admin123" });
            var duplicate = await client.PostAsJsonAsync("/admin/create", new { name = "Adm 2", email, password = "admin123" });

            await Expect.Status(blank, HttpStatusCode.BadRequest);
            await Expect.Status(created, HttpStatusCode.Created);
            await Expect.Status(duplicate, HttpStatusCode.Conflict);
        }

        [Fact(Skip = "BUG-015: PriceEndpoints.UpdatePrice usa SingleOrDefaultAsync nas rotas da distância — com 2+ rotas estoura 500")]
        public async Task Updating_price_per_km_of_a_distance_with_two_routes_does_not_crash()
        {
            var admin = await _api.NewAdmin();
            var leg = await _api.CreateLeg(admin, kilometers: 50, pricePerKm: 1f);
            await _api.CreateRoute(leg.Distance.Id);
            await _api.CreateRoute(leg.Distance.Id);
            var prices = await admin.GetFromJsonAsync<List<PriceDto>>("/prices/list");
            var price = prices!.Single(p => p.DistanceId == leg.Distance.Id);

            var response = await admin.PatchAsJsonAsync($"/prices/update/{price.Id}",
                new { distanceId = leg.Distance.Id, newPrice = 2f });

            Assert.NotEqual(HttpStatusCode.InternalServerError, response.StatusCode);
        }
    }
}
