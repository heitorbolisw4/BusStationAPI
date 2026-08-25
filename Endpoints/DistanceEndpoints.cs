using BusStation_API.Data;
using BusStation_API.Entities;
using Microsoft.EntityFrameworkCore;
using static BusStation_API.DTO.DistanceContracts;

namespace BusStation_API.Endpoints
{
    public static class DistanceEndpoints
    {
        public static IEndpointRouteBuilder MapDistanceEndpoints(this IEndpointRouteBuilder app)
        {
            var group = app.MapGroup("/distances");
            group.MapPut("/create", CreateDistance);
            group.MapGet("/list", ListDistances);
            return app;
        }
        
        private static async Task<IResult> CreateDistance(CreateDistanceRequest request, AppDbContext db)
        {
            var error = ValidateDistance(request.OriginCityId, request.DestinationCityId, request.Kilometers);
            if(error is not null)
                return Results.BadRequest( new { message = error } );

            var origin = await db.City.FirstOrDefaultAsync(x => x.Id == request.OriginCityId );
            var destination = await db.City.FirstOrDefaultAsync(x => x.Id == request.DestinationCityId); 
            if(origin is null || destination is null)
                return Results.NotFound();
            
            if(origin.Id == destination.Id)
                return Results.Conflict();

            Distance distance = new()
            {
              OriginCityId = request.OriginCityId,
              DestinationCityId = request.DestinationCityId,
              Kilometers = request.Kilometers
            };
            db.Add(distance);
            await db.SaveChangesAsync();
            return Results.Created();

        }

        private static async Task<IResult> ListDistances(AppDbContext db)
        {
            var response = await db.Distances.Select(x => ToResponse(x)).ToListAsync();
            return Results.Ok(response);
        }
        private static DistanceResponse ToResponse(Distance distance)
        {
            return new DistanceResponse(distance.Id, distance.OriginCityId, distance.DestinationCityId, distance.Kilometers);
        }



        private static string? ValidateDistance(int originId, int destinationId, int kilometers)
        {
            if( originId <= 0 || destinationId <= 0)
                return "You must provide a valid origin and destination!";

            if( kilometers <= 0 )
                return "You must enter a valid mileage";

            return null;
        }
    }
}