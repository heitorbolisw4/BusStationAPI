using BusStation_API.Data;
using BusStation_API.Entities;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using static BusStation_API.DTO.BoardingContracts;

namespace BusStation_API.Endpoints
{
    public static class BoardingEndpoints
    {
        public static IEndpointRouteBuilder MapBoardingsEndpoints(this IEndpointRouteBuilder app)
        {
            var group = app.MapGroup("/");
            group.MapPost("/create", CreateBoarding);
            // público: busca de horários é a tela inicial do site, anônima
            group.MapGet("/search", SearchBoardings).AllowAnonymous();
            return app;
        }

        private static async Task<IResult> SearchBoardings(int originCityId, int destinationCityId, DateOnly date, AppDbContext db)
        {
            if(originCityId <= 0 || destinationCityId <= 0 || originCityId == destinationCityId)
                return Results.BadRequest( new { message = "You must provide two different valid cities" } );

            var now = DateTime.Now;
            var today = DateOnly.FromDateTime(now);
            if(date < today)
                return Results.BadRequest( new { message = "You must provide a valid date" } );

            // se a busca é para hoje, saída que já partiu não interessa ao cliente
            var minTime = date == today ? TimeOnly.FromDateTime(now) : TimeOnly.MinValue;

            // Faltando de propósito: `&& b.Routes!.IsActive`. POST /routes/create nunca
            // seta IsActive, então toda rota no banco está com false — o filtro
            // devolveria lista vazia sempre. Acrescente aqui quando o create setar true.
            var departures = await db.Boardings
                .Where(b => b.BoardingDate == date
                         && b.BoardingTime >= minTime
                         && b.Seat > 0
                         && b.Routes!.Distance!.OriginCityId == originCityId
                         && b.Routes.Distance.DestinationCityId == destinationCityId)
                .OrderBy(b => b.BoardingTime)
                .Select(b => new SearchBoardingResponse(
                    b.Id,
                    b.RouteId,
                    b.Routes!.RouteName,
                    b.Routes.Distance!.OriginCity!.CityName,
                    b.Routes.Distance.OriginCity.Acronym,
                    b.Routes.Distance.DestinationCity!.CityName,
                    b.Routes.Distance.DestinationCity.Acronym,
                    b.Routes.Distance.Kilometers,
                    b.BoardingDate,
                    b.BoardingTime,
                    b.Seat,
                    b.Routes.Price))
                .ToListAsync();

            // Lista vazia é busca bem-sucedida com zero resultados, não erro:
            // 200 com [] deixa o front escrever "nenhuma saída nesse dia".
            return Results.Ok(departures);
        }


        private static async Task<IResult> CreateBoarding(CreateBoardingRequest request, AppDbContext db)
        {
            var error = ValidateBoarding(request.RouteId, request.Seats, request.BoardingDate);
            if(error is not null)
                return Results.BadRequest( new { message = error } );

            // validar se Route existe
            var route = await db.Routes.AnyAsync(x => x.Id == request.RouteId);
            if (!route)
            {
                return Results.NotFound();
            }            

            Boarding boarding = new()
            {
                RouteId = request.RouteId,
                Seat = request.Seats,
                BoardingDate = request.BoardingDate,
                BoardingTime = request.BoardingTime
            };
            

            await db.AddAsync(boarding);
            await db.SaveChangesAsync();

            return Results.Created( "/boardings/", boarding);
        }
        





        private static string? ValidateBoarding(int routeId, int seats, DateOnly boardingDate)
        {
            var today = DateOnly.FromDateTime(DateTime.Now);
            if( routeId <= 0 )
                return "You must provide a valid route!";
            if( seats <= 0 )
                return "You must provide a valid number of seats";

            if( boardingDate < today )
                return "You must provide a valid date";

            return null;
        }
    }
}