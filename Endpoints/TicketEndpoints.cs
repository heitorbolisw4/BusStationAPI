using System.Security.Claims;
using BusStation_API.Data;
using BusStation_API.Entities;
using Microsoft.EntityFrameworkCore;
using static BusStation_API.DTO.TicketContracts;

namespace BusStation_API.Endpoints
{
    public static class TicketEndpoints
    {
        public static IEndpointRouteBuilder MapTicketEndpoints(this IEndpointRouteBuilder app)
        {
            var group = app.MapGroup("/");
            group.MapPost("/create", CreateTicket);
            group.MapGet("/list", ListTickets);
            group.MapGet("/list/{id:int}", GetTicket);
            return app;
        }

        private static async Task<IResult> CreateTicket(CreateTicketRequest request, AppDbContext db, ClaimsPrincipal user)
        {
            var userId = GetCurrentUser(user);
            if(userId is null)
                return Results.Unauthorized();

            var error = ValidateTicket(request.BoardingId);
            if(error is not null)
                return Results.BadRequest( new { message = error } );

            // AsNoTracking: o Seat lido aqui é só para montar a resposta; quem decide se há
            // vaga é o UPDATE atômico abaixo, nunca este valor (que pode já estar velho).
            var boarding = await WithTrip(db.Boardings.AsNoTracking()).FirstOrDefaultAsync(b => b.Id == request.BoardingId);
            if(boarding is null || boarding.Routes is null)
                return Results.NotFound();

            await using var transaction = await db.Database.BeginTransactionAsync();

            // Decremento atômico (BUG-030): o banco só baixa a vaga se ainda houver alguma.
            // Com "ler, subtrair e salvar", compras simultâneas liam o mesmo Seat e todas passavam.
            var reserved = await db.Boardings
                .Where(b => b.Id == boarding.Id && b.Seat > 0)
                .ExecuteUpdateAsync(s => s.SetProperty(b => b.Seat, b => b.Seat - 1));
            if(reserved == 0)
                return Results.BadRequest( new { message = "Dont have seats" } );

            Ticket ticket = new()
            {
                BoardingId = boarding.Id,
                UserId = userId.Value,
                FarePaid = boarding.Routes.Price,
                PurchasedOn = DateTime.UtcNow,
                BoardingDate = boarding.BoardingDate
            };

            db.Add(ticket);
            await db.SaveChangesAsync();
            await transaction.CommitAsync();

            ticket.Boarding = boarding;
            return Results.Created( "/tickets/", ToResponse(ticket));
        }

        private static async Task<IResult> ListTickets(AppDbContext db, ClaimsPrincipal user)
        {
            var userId = GetCurrentUser(user);
            if(userId is null)
                return Results.Unauthorized();

            var tickets = await WithTrip(db.Tickets)
                .Where(t => t.UserId == userId)
                .Select(t => ToResponse(t))
                .ToListAsync();

            return Results.Ok(tickets);
        }

        private static async Task<IResult> GetTicket(int id, AppDbContext db, ClaimsPrincipal user)
        {
            var userId = GetCurrentUser(user);
            if(userId is null)
                return Results.Unauthorized();

            var ticket = await WithTrip(db.Tickets)
                .SingleOrDefaultAsync(t => t.Id == id && t.UserId == userId);

            if(ticket is null || ticket.Boarding is null)
                return Results.NotFound();

            return Results.Ok(ToResponse(ticket));
        }

        // Embarque → rota → distância → cidades: tudo que a resposta de passagem mostra.
        private static IQueryable<Boarding> WithTrip(IQueryable<Boarding> boardings) =>
            boardings
                .Include(b => b.Routes).ThenInclude(r => r!.Distance).ThenInclude(d => d!.OriginCity)
                .Include(b => b.Routes).ThenInclude(r => r!.Distance).ThenInclude(d => d!.DestinationCity);

        private static IQueryable<Ticket> WithTrip(IQueryable<Ticket> tickets) =>
            tickets
                .Include(t => t.Boarding).ThenInclude(b => b!.Routes).ThenInclude(r => r!.Distance).ThenInclude(d => d!.OriginCity)
                .Include(t => t.Boarding).ThenInclude(b => b!.Routes).ThenInclude(r => r!.Distance).ThenInclude(d => d!.DestinationCity);

        private static TicketResponse ToResponse(Ticket ticket)
        {
            var boarding = ticket.Boarding!;
            var distance = boarding.Routes?.Distance;
            return new TicketResponse(
                ticket.Id,
                ticket.BoardingId,
                boarding.RouteId,
                boarding.Routes?.RouteName ?? string.Empty,
                ticket.BoardingDate,
                boarding.BoardingTime,
                ticket.FarePaid,
                ticket.PurchasedOn,
                distance?.OriginCity?.CityName ?? string.Empty,
                distance?.DestinationCity?.CityName ?? string.Empty
            );
        }

        private static int? GetCurrentUser(ClaimsPrincipal user)
        {
            var userIdClaim = user.FindFirstValue(ClaimTypes.NameIdentifier);
            return userIdClaim is not null && int.TryParse(userIdClaim, out var userId) ? userId : null;
        }

        private static string? ValidateTicket(int boardingId)
        {
            if( boardingId <= 0)
                return "You must enter a valid boardingId";

            return null;
        }
    }
}
