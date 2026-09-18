using BusStation_API.Data;
using BusStation_API.Entities;
using Microsoft.EntityFrameworkCore;
using static BusStation_API.DTO.PriceContracts;

namespace BusStation_API.Endpoints
{
    public static class PriceEndpoints
    {
        public static IEndpointRouteBuilder MapPriceEndpoints(this IEndpointRouteBuilder app)
        {
            var group = app.MapGroup("/");
            group.MapPost("/create", CreatePrice);
            group.MapPatch("/update/{id:int}", UpdatePrice);
            group.MapGet("/list", ListPrices);
            return app;
        }

        private static async Task<IResult> CreatePrice(CreatePriceRequest request, AppDbContext db)
        {
            var error = Price.CreateValidPrice(request.PricePerKm, request.DistanceId);
            if(error is not null)
                return Results.BadRequest( new { message = error } );

            var distanceExists = await db.Distances.AnyAsync(d => d.Id == request.DistanceId);
            if(!distanceExists)
                return Results.NotFound();

            Price price = new()
            {
                DistanceId = request.DistanceId,
                PricePerKm = request.PricePerKm
            };

            await db.AddAsync(price);
            await db.SaveChangesAsync();
            return Results.Created();
        }

        private static async Task<IResult> UpdatePrice(int id, UpdatePriceRequest request, AppDbContext db)
        {
            var error = Price.CreateValidPrice(request.NewPrice, request.DistanceId);
            if(error is not null)
                return Results.BadRequest( new { message = error } );

            var price = await db.Prices.FirstOrDefaultAsync(x => x.Id == id && x.DistanceId == request.DistanceId);
            if(price is null)
                return Results.NotFound();

            price.PricePerKm = request.NewPrice;

            var route = await db.Routes.Where(x => x.DistanceId == request.DistanceId).Include(y => y.Distance).SingleOrDefaultAsync();
            if(route is null || route.Distance is null)
                return Results.NotFound();

            route.Price = price.PricePerKm * route.Distance.Kilometers;

            await db.SaveChangesAsync();
            return Results.Ok();
        }

        private static async Task<IResult> ListPrices(AppDbContext db)
        {
            var response = await db.Prices.Select(x => ToResponse(x)).ToListAsync();
            return Results.Ok(response);
        }

        private static PriceResponse ToResponse(Price price)
        {
            return new PriceResponse(price.Id, price.DistanceId, price.PricePerKm);
        }
    }
}
