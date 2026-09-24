using System.Net;
using System.Net.Http.Json;
using BusStation_API.E2E.Infrastructure;

namespace BusStation_API.E2E
{
    /// <summary>Compra e consulta de passagens — regras de acesso e de estoque de vagas.</summary>
    [Collection(E2ECollection.Name)]
    public class TicketTests
    {
        private readonly TestApi _api;

        public TicketTests(BusStationApiFactory factory)
        {
            _api = new TestApi(factory);
        }

        [Theory]
        [InlineData("POST", "/tickets/create")]
        [InlineData("GET", "/tickets/list")]
        [InlineData("GET", "/tickets/list/1")]
        public async Task Anonymous_request_is_rejected_with_401(string method, string path)
        {
            var request = new HttpRequestMessage(new HttpMethod(method), path);
            if(method == "POST")
                request.Content = JsonContent.Create(new { boardingId = 1 });

            var response = await _api.Anonymous().SendAsync(request);

            await Expect.Status(response, HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task Buying_for_an_unknown_boarding_returns_404_and_invalid_id_returns_400()
        {
            var customer = await _api.NewCustomer();

            var unknown = await customer.PostAsJsonAsync("/tickets/create", new { boardingId = 999_999 });
            var invalid = await customer.PostAsJsonAsync("/tickets/create", new { boardingId = 0 });

            await Expect.Status(unknown, HttpStatusCode.NotFound);
            await Expect.Status(invalid, HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task Customer_cannot_see_another_customers_ticket()
        {
            var route = await _api.CreateRoute(Seed.IndianopolisToUberlandia);
            var boarding = await _api.CreateBoarding(route.Id, TestApi.UniqueFutureDate(), new TimeOnly(10, 0));
            var owner = await _api.NewCustomer();
            var stranger = await _api.NewCustomer();

            var purchase = await owner.PostAsJsonAsync("/tickets/create", new { boardingId = boarding.Id });
            await Expect.Status(purchase, HttpStatusCode.Created);
            var ticket = (await purchase.Content.ReadFromJsonAsync<TicketDto>())!;

            // 404 e não 403: não confirmar para um estranho que aquele id existe
            await Expect.Status(await stranger.GetAsync($"/tickets/list/{ticket.Id}"), HttpStatusCode.NotFound);
            Assert.Empty((await stranger.GetFromJsonAsync<List<TicketDto>>("/tickets/list"))!);
        }

        [Fact]
        public async Task Fare_paid_is_frozen_at_purchase_time()
        {
            var admin = await _api.NewAdmin();
            var leg = await _api.CreateLeg(admin, kilometers: 80, pricePerKm: 0.25f);
            var route = await _api.CreateRoute(leg.Distance.Id);
            var boarding = await _api.CreateBoarding(route.Id, TestApi.UniqueFutureDate(), new TimeOnly(11, 0));
            var customer = await _api.NewCustomer();

            var purchase = await customer.PostAsJsonAsync("/tickets/create", new { boardingId = boarding.Id });
            await Expect.Status(purchase, HttpStatusCode.Created);
            var ticket = (await purchase.Content.ReadFromJsonAsync<TicketDto>())!;
            Assert.Equal(20f, ticket.FarePaid, 0.001f); // 80 km x 0,25/km

            // Reajuste do preço/km e recálculo da rota depois da venda
            await Expect.Status(
                await admin.PostAsJsonAsync("/prices/create", new { distanceId = leg.Distance.Id, pricePerKm = 2.0f }),
                HttpStatusCode.Created);
            await Expect.Status(
                await admin.PatchAsJsonAsync($"/routes/update/{route.Id}", new { price = 1f }),
                HttpStatusCode.OK);

            var stored = await customer.GetFromJsonAsync<TicketDto>($"/tickets/list/{ticket.Id}");
            Assert.Equal(ticket.FarePaid, stored!.FarePaid);
        }

        [Fact]
        public async Task Admin_token_cannot_buy_tickets()
        {
            var admin = await _api.NewAdmin();

            var response = await admin.PostAsJsonAsync("/tickets/create", new { boardingId = 1 });

            Assert.True(response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden,
                $"esperado 401/403, veio {(int)response.StatusCode}");
        }
    }
}
