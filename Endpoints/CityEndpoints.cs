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