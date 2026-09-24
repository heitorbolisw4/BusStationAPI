using System.Net;
using System.Net.Http.Json;
using BusStation_API.E2E.Infrastructure;

namespace BusStation_API.E2E
{
    [Collection(E2ECollection.Name)]
    public class CityTests
    {
        private readonly TestApi _api;

        public CityTests(BusStationApiFactory factory)
        {
            _api = new TestApi(factory);
        }

        [Fact]
        public async Task Seeded_cities_are_listed_for_anonymous_visitors()
        {
            var cities = await _api.Anonymous().GetFromJsonAsync<List<CityDto>>("/cities/list");

            Assert.Contains(cities!, c => c is { Id: Seed.Indianopolis, CityName: "Indianopolis", Acronym: "Indi" });
            Assert.Contains(cities!, c => c is { Id: Seed.Uberaba, CityName: "Uberaba", Acronym: "Ura" });
        }

        [Fact]
        public async Task City_lifecycle_create_update_delete()
        {
            var city = await _api.CreateCity();
            var client = _api.Anonymous();
            var renamed = TestApi.Unique("Renamed ");

            var update = await client.PutAsJsonAsync($"/cities/update/{city.Id}",
                new { cityName = renamed, state = "SP", acronym = city.Acronym });
            await Expect.Status(update, HttpStatusCode.NoContent);

            var listed = (await client.GetFromJsonAsync<List<CityDto>>("/cities/list"))!.Single(c => c.Id == city.Id);
            Assert.Equal(renamed, listed.CityName);
            Assert.Equal("SP", listed.State);

            await Expect.Status(await client.DeleteAsync($"/cities/delete/{city.Id}"), HttpStatusCode.NoContent);
            Assert.DoesNotContain((await client.GetFromJsonAsync<List<CityDto>>("/cities/list"))!, c => c.Id == city.Id);
            await Expect.Status(await client.DeleteAsync($"/cities/delete/{city.Id}"), HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task Create_rejects_blank_fields_and_duplicate_name()
        {
            var city = await _api.CreateCity();
            var client = _api.Anonymous();

            var blank = await client.PostAsJsonAsync("/cities/create", new { cityName = "", state = "MG", acronym = "XX" });
            var duplicate = await client.PostAsJsonAsync("/cities/create",
                new { cityName = city.CityName, state = "MG", acronym = "ZZ9" });

            await Expect.Status(blank, HttpStatusCode.BadRequest);
            await Expect.Status(duplicate, HttpStatusCode.Conflict);
        }

        [Fact]
        public async Task City_used_by_a_distance_cannot_be_deleted()
        {
            var response = await _api.Anonymous().DeleteAsync($"/cities/delete/{Seed.Uberlandia}");

            await Expect.Status(response, HttpStatusCode.Conflict);
        }

        [Fact(Skip = "BUG-001: checagem de duplicidade não olha Acronym — índice único estoura 500")]
        public async Task Create_with_an_acronym_already_in_use_returns_409()
        {
            var city = await _api.CreateCity();

            var response = await _api.Anonymous().PostAsJsonAsync("/cities/create",
                new { cityName = TestApi.Unique("Other "), state = "MG", acronym = city.Acronym });

            await Expect.Status(response, HttpStatusCode.Conflict);
        }
    }
}
