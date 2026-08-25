using BusStation_API.Data;
using BusStation_API.Entities;
using Microsoft.EntityFrameworkCore;
using Route = BusStation_API.Entities.Route;
using static BusStation_API.DTO.RouteContracts;

namespace BusStation_API.Endpoints
{
    public static class RouteEndpoints
    {
        public static IEndpointRouteBuilder MapRouteEndpoints(this IEndpointRouteBuilder app)
        {
            var group = app.MapGroup("/");
            group.MapPost("/create", CreateRoute);
            group.MapGet("/list", RouteList);
            return app;
        }

        private static async Task<IResult> CreateRoute(CreateRouteRequest request, AppDbContext db)
        {
            var error = ValidateRoute(request.RouteName, request.DistanceId);
            if(error is not null)
                return Results.BadRequest( new { message = error });


            if(await db.Routes.AnyAsync(x => x.DistanceId == request.DistanceId && x.RouteName == request.RouteName))
            {
                return Results.Conflict();
            }
            
            
            var query = await db.Distances.Where(x => x.Id == request.DistanceId).Include(p => p.Prices).SingleOrDefaultAsync();
            if(query is null)
            {
                return Results.NotFound();
            }

            var selectedPrice = query.Prices?.OrderByDescending(x => x.Id).FirstOrDefault();
            if(selectedPrice is null)
            {
                return Results.NotFound();
            }
            
            
            var price = query.Kilometers * selectedPrice.PricePerKm;
            
            Route route = new()
            {
                RouteName = request.RouteName,
                DistanceId = request.DistanceId,
                Price = price,
                CreatedAt = DateTime.UtcNow
            };
            db.Add(route);
            await db.SaveChangesAsync();
            return Results.Created();
        }


        private static async Task<IResult> RouteList(AppDbContext db)
        {
            var response = await db.Routes.Include(x => x.Distance).Select(x => ToResponse(x)).ToListAsync();

            return Results.Ok( response );
            
        }
        private static RouteResponse ToResponse(Route route)
        {
            
            return new RouteResponse(route.Id, route.RouteName, route.Distance!.Kilometers);
        }




        private  static string? ValidateRoute(string routeName, int distanceId)
        {
            if (string.IsNullOrWhiteSpace(routeName) || routeName.Length < 5 )
            {
                return "You must type a valid Route Name";
            }
            if(distanceId <= 0)
            {
                return "Enter a valid distance Id";
            }
            return null;
        }
    }
}