using BusStation_API.Data;
using BusStation_API.DTO.User;
using BusStation_API.Entities;
using BusStation_API.Interface;
using BusStation_API.Service;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<ITokenService, TokenService>();

var app = builder.Build();

app.MapPost("/register", async (AppDbContext db, RegisterRequestDto request, IAuthService service) =>
{
    if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        return Results.BadRequest("You must fill in all the fields");

    if(request.Age < 18)
        return Results.BadRequest("You must be of legal age");

    bool emailExists = await db.Users.AnyAsync(u => u.Email == request.Email);
    if(emailExists)
        return Results.Conflict("The Email already exists");

    string doHashPassw = service.GenerateHash(request.Password);

    User user = new User
    {
        Id = Guid.NewGuid(),
        Name = request.Name,
        Age = request.Age,
        Email = request.Email,
        Password = doHashPassw
    };
    db.Users.Add(user);
    await db.SaveChangesAsync();

    return Results.Created();
});

app.Run();
