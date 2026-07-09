using System.Security.Claims;
using System.Text;
using BusStation_API.Data;
using BusStation_API.DTO.City;
using BusStation_API.DTO.Destination;
using BusStation_API.DTO.Distance;
using BusStation_API.DTO.Origin;
using BusStation_API.DTO.User;
using BusStation_API.Entities;
using BusStation_API.Interface;
using BusStation_API.Jwt;
using BusStation_API.Service;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));

builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("JwtSettings"));
var jwtSettings = builder.Configuration.GetSection("JwtSettings").Get<JwtSettings>();

builder.Services.AddSingleton<ITokenService, TokenService>();
builder.Services.AddSingleton<IAuthService, AuthService>();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
{

   options.TokenValidationParameters = new TokenValidationParameters
   {
       ValidateIssuer = true,
       ValidateAudience = true,
       ValidateLifetime = true,
       ValidateIssuerSigningKey = true,

       ClockSkew = TimeSpan.Zero,
       ValidAudience = jwtSettings!.Audience,
       ValidIssuer = jwtSettings!.Issuer,
       IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SecretKey)),

       NameClaimType = ClaimTypes.NameIdentifier
   }; 
});

builder.Services.AddAuthorization();


var app = builder.Build();

var user = app.MapGroup("/user").RequireAuthorization();
var cities =  app.MapGroup("/cities");
var routes = app.MapGroup("/routes");
var distances =  app.MapGroup("/distances");
var origins = app.MapGroup("/origins");
var destination = app.MapGroup("/destination");
var distance = app.MapGroup("/distance");

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
app.MapPost("/login", async (AppDbContext db, LoginRequestDto request, IAuthService authService, ITokenService tokenService) =>
{
    if(string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        return Results.BadRequest("you must fill in all the fields");

    var user = await db.Users.SingleOrDefaultAsync(u => u.Email == request.Email);
    if(user is null)
        return Results.Unauthorized();
    
    string passw =  user.Password;
    bool passwVerify = authService.PasswordVerify(request.Password, passw);
    if(!passwVerify)
        return Results.Unauthorized();
    
    var token = tokenService.GenerateToken(user);
    return Results.Ok(new {token});

});


user.MapGet("/me", async (AppDbContext db, ClaimsPrincipal user) =>
{
    var userIdClaim = user.FindFirstValue(ClaimTypes.NameIdentifier);
    if(string.IsNullOrWhiteSpace(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        return Results.Unauthorized();
    
    var profile = await db.Users.SingleOrDefaultAsync(u => u.Id == userId);
    if(profile is null)
        return Results.NotFound();

    var response = new UserResponseDto
    {
        Name = profile.Name,
        Email = profile.Email,
        Age = profile.Age
    };
    return Results.Ok(response);

});

cities.MapPost("/create", async (AppDbContext db, CreateCityRequestDto request) =>
{
    if(string.IsNullOrWhiteSpace(request.CityName) ||
        string.IsNullOrWhiteSpace(request.State) ||
        string.IsNullOrWhiteSpace(request.Acronym))
        return Results.BadRequest();

    var Exists = await db.City.AnyAsync( c => c.State == request.Acronym);
    if(Exists)
        return Results.Conflict();

    City city = new City
    {
        CityName = request.CityName,
        State = request.State,
        Acronym = request.Acronym
    };
    db.Add(city);
    await db.SaveChangesAsync();
    return Results.Created();
});
cities.MapGet("/list", async (AppDbContext db) =>
{

    var response = await db.City.Select( r => new GetCityResponseDto
    {
        Id = r.Id,
        CityName = r.CityName,
        State = r.State,
        Acronym = r.Acronym

    }).ToListAsync();

    if(response is null)
        return Results.NotFound();

    return Results.Ok(response);
});

origins.MapPost("/create", async (AppDbContext db, CreateOriginRequestDto request) =>
{
    if(string.IsNullOrWhiteSpace(request.OriginName))
        return Results.BadRequest();

    var city = await db.City.SingleAsync(c => c.Id == request.CityId);
    if(city is null)
        return Results.NotFound();
  
    var exists = await db.Origins.AnyAsync( o => o.OriginName == request.OriginName);
    if(exists)
        return Results.Conflict();


    Origin origin = new Origin
    {
        OriginName = request.OriginName,
        CityId = request.CityId

    };
    await db.AddAsync(origin);
    await db.SaveChangesAsync();
    return Results.Created();


    
});
origins.MapGet("/list", async (AppDbContext db) =>
{
    var response = await db.Origins.Select( r => new GetOriginResponseDto
    {
        Id = r.Id,
        OriginName = r.OriginName,
        CityId = r.CityId
    }).ToListAsync();

    return Results.Ok(response);
});

destination.MapPost("/create", async (AppDbContext db, CreateDestinationRequestDto request) =>
{
    if(string.IsNullOrWhiteSpace(request.DestinationName))
        return Results.BadRequest();

    bool city = await db.City.AnyAsync(c => c.Id == request.CityId);
    if(!city)
        return Results.NotFound();

    Destination destination = new Destination
    {
        DestinationName = request.DestinationName,
        CityId = request.CityId
    };
    await db.AddAsync(destination);
    await db.SaveChangesAsync();
    return Results.Created();

});
destination.MapGet("/list", async (AppDbContext db) =>
{
    var response = await db.Destinations.Select( r => new GetDestinationDestinationDto
    {
        DestinationName = r.DestinationName,
        CityId = r.CityId
    }).ToListAsync();  

    if(response is null)
        return Results.NotFound();

    return Results.Ok(response);
});

distance.MapPost("/create", async (AppDbContext db, CreateDistanceRequestDto request) =>
{
    //validar claims -> return 401

    
    // validar request -> return 400
    if(request.OriginId == 0 ||
        request.DestinationId == 0 ||
        request.Kilometers == 0)
        return Results.BadRequest();

    // validar conflito -> return 409
    bool exists = await db.Distances
        .AnyAsync(d => d.OriginId == request.OriginId &&
                        d.DestinationId == request.DestinationId);

    if(exists)
        return Results.Conflict();


    Distance distance = new Distance
    {
        OriginId = request.OriginId,
        DestinationId = request.DestinationId,
        Quilometers = request.Kilometers

    };
    await db.Distances.AddAsync(distance);
    await db.SaveChangesAsync();
    return Results.Created();



});
distance.MapGet("/list", async (AppDbContext db) =>
{
    // query de cidade relacionando com id
});


app.Run();
