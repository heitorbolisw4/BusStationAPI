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
            return app;
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