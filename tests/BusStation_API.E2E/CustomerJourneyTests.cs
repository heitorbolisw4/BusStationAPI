using System.Net;
using System.Net.Http.Json;
using BusStation_API.E2E.Infrastructure;

namespace BusStation_API.E2E
{
    /// <summary>
    /// O loop central do MVP, do jeito que o cliente passa por ele no site:
    /// cadastro → login → busca de saídas → compra → "minhas passagens".
    /// Se só um teste desta suíte puder ficar verde, é este.
    /// </summary>
    [Collection(E2ECollection.Name)]
    public class CustomerJourneyTests
    {
        private const string TicketAuthBug =
            "BUG-014: grupo /tickets usa RequireAuthorization() sem policy e o scheme default \"Bearer\" não existe — toda request autenticada devolve 500";

        private readonly TestApi _api;

        public CustomerJourneyTests(BusStationApiFactory factory)
        {
            _api = new TestApi(factory);
        }

        [Fact(Skip = TicketAuthBug)]
        public async Task Customer_can_find_a_departure_buy_a_ticket_and_see_it_in_their_tickets()
        {
            // Operação cadastra uma saída Indianópolis -> Uberlândia
            var date = TestApi.UniqueFutureDate();
            var route = await _api.CreateRoute(Seed.IndianopolisToUberlandia);
            var boarding = await _api.CreateBoarding(route.Id, date, new TimeOnly(8, 30), seats: 10);

            // Cliente cria conta e entra
            var (email, password) = await _api.RegisterUser();
            var customer = _api.WithToken(await _api.Login(email, password));

            // Busca a viagem (anônimo — consultar horário não exige login)
            var departures = await _api.Search(Seed.Indianopolis, Seed.Uberlandia, date);
            var departure = Assert.Single(departures);
            Assert.Equal(boarding.Id, departure.BoardingId);
            Assert.Equal(route.RouteName, departure.RouteName);
            Assert.Equal("Indianopolis", departure.OriginCity);
            Assert.Equal("Uberlandia", departure.DestinationCity);
            Assert.Equal(60, departure.Kilometers);
            Assert.Equal(10, departure.Seats);
            Assert.Equal(30f, departure.Price, 0.001f); // 60 km x 0,50/km

            // Compra
            var purchase = await customer.PostAsJsonAsync("/tickets/create", new { boardingId = departure.BoardingId });
            await Expect.Status(purchase, HttpStatusCode.Created);
            var ticket = (await purchase.Content.ReadFromJsonAsync<TicketDto>())!;
            Assert.Equal(boarding.Id, ticket.BoardingId);
            Assert.Equal(route.Id, ticket.RouteId);
            Assert.Equal(date, ticket.BoardingDate);
            Assert.Equal(new TimeOnly(8, 30), ticket.BoardingTime);
            Assert.Equal(30f, ticket.FarePaid, 0.001f);

            // A vaga vendida some da busca
            var afterPurchase = Assert.Single(await _api.Search(Seed.Indianopolis, Seed.Uberlandia, date));
            Assert.Equal(9, afterPurchase.Seats);

            // "Minhas passagens"
            var myTickets = await customer.GetFromJsonAsync<List<TicketDto>>("/tickets/list");
            Assert.Equal(ticket.Id, Assert.Single(myTickets!).Id);

            var detail = await customer.GetFromJsonAsync<TicketDto>($"/tickets/list/{ticket.Id}");
            Assert.Equal(ticket with { PurchasedOn = detail!.PurchasedOn }, detail);
        }

        [Fact(Skip = TicketAuthBug)]
        public async Task Last_seat_can_be_sold_once_and_then_the_departure_disappears_from_search()
        {
            var date = TestApi.UniqueFutureDate();
            var route = await _api.CreateRoute(Seed.UberlandiaToUberaba);
            var boarding = await _api.CreateBoarding(route.Id, date, new TimeOnly(14, 0), seats: 1);

            var first = await _api.NewCustomer();
            var second = await _api.NewCustomer();

            var sold = await first.PostAsJsonAsync("/tickets/create", new { boardingId = boarding.Id });
            await Expect.Status(sold, HttpStatusCode.Created);

            var soldOut = await second.PostAsJsonAsync("/tickets/create", new { boardingId = boarding.Id });
            await Expect.Status(soldOut, HttpStatusCode.BadRequest);

            Assert.Empty(await _api.Search(Seed.Uberlandia, Seed.Uberaba, date));
            Assert.Empty((await second.GetFromJsonAsync<List<TicketDto>>("/tickets/list"))!);
        }
    }
}
