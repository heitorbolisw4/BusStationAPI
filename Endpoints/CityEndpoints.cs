using BusStation_API.Data;
using BusStation_API.DTO;
using BusStation_API.Entities;
using Microsoft.EntityFrameworkCore;

namespace BusStation_API.Endpoints
{

    public static class CityEndpoints
    {
        public static IEndpointRouteBuilder MapCitiesEnpoints(this IEndpointRouteBuilder app)
        {
            app.MapPost("/create", Create);
            app.MapGet("/list", List);
            app.MapPut("/update/{id:int}", Update);
            app.MapDelete("/delete/{id:int}", Delete);

            return app;
        }
        private static async Task<IResult> Create(CreateCityRequest request, AppDbContext db)
        {
            var error = ValidateCity(request.CityName, request.State, request.Acronym);
            if(error is not null)
                return Results.BadRequest(new { message = error});

            var exists = await db.City.AnyAsync(c => c.CityName == request.CityName);
            if(exists)
                return Results.Conflict();

            City city = new()
            {
                CityName = request.CityName,
                State = request.State,
                Acronym = request.Acronym
            };

            await db.AddAsync(city);
            await db.SaveChangesAsync();
            return Results.Created();
        }
        private static async Task<IResult> Update(int id, UpdateCityRequest request, AppDbContext db)
        {
            var city = await db.City.SingleOrDefaultAsync(c => c.Id == id);
            if(city is null)
                return Results.NotFound();

            var error = ValidateCity(request.CityName, request.State, request.Acronym);
            if(error is not null)
                return Results.BadRequest(new { message = error });

            // verifico se a city ja existe
            var exists = await db.City.AnyAsync(x => x.CityName == request.CityName && x.State == request.State);
            if(exists)
                return Results.Conflict();

            city.CityName = request.CityName;
            city.State = request.State;
            city.Acronym = request.Acronym;
            await db.SaveChangesAsync();
            return Results.NoContent();


        }

        private static async Task<IResult> Delete(int id, AppDbContext db)
        {
            var city = await db.City.FirstOrDefaultAsync(x =>x.Id == id);
            if(city is null)
                return Results.NotFound();


            // verifico se city é uma origem e um destino
            var exists =    await db.Origins.AnyAsync(x => x.CityId == id) ||
                            await db.Destinations.AnyAsync(x => x.CityId == id);

            if(exists)
                return Results.Conflict();

            db.Remove(city);
            await db.SaveChangesAsync();
            return Results.NoContent();

        }


        private static async Task<IResult> List(AppDbContext db)
        {
            var response = await db.City.Select(r => ToResponse(r)).ToListAsync();
            return Results.Ok(response);

        }
        private static CityResponse ToResponse(City city)
        {
            return new CityResponse(city.Id, city.CityName, city.State, city.Acronym);
        }


        private static string? ValidateCity(string cityName, string state, string cityAcronym)
        {
            if(string.IsNullOrWhiteSpace(cityName) || 
            string.IsNullOrWhiteSpace(state) || 
            string.IsNullOrWhiteSpace(cityAcronym))
                return "All fields must be filled in";

            return null;
        }
    }
}