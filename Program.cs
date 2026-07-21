using System.Security.Claims;
using System.Text;
using System.Text.Json.Serialization;
using BusStation_API.Data;
using BusStation_API.DTO.City;
using BusStation_API.DTO.Destination;
using BusStation_API.DTO.Distance;
using BusStation_API.DTO.Origin;
using BusStation_API.DTO.Price;
using BusStation_API.DTO.Route;
using BusStation_API.DTO.Ticket;
using BusStation_API.DTO.User;
using BusStation_API.Entities;
using BusStation_API.Interface;
using BusStation_API.Jwt;
using BusStation_API.Service;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Route = BusStation_API.Entities.Route;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));

builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("JwtSettings"));
var jwtSettings = builder.Configuration.GetSection("JwtSettings").Get<JwtSettings>();

builder.Services.AddSingleton<ITokenService, TokenService>();
builder.Services.AddSingleton<IAuthService, AuthService>();
builder.Services.Configure<Microsoft.AspNetCore.Http.Json.JsonOptions>(options => options.SerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles);
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
var tickets = app.MapGroup("/tickets").RequireAuthorization().WithTags("Tickets");
var prices = app.MapGroup("/prices");

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

    User user = new()
    {
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
    if (string.IsNullOrWhiteSpace(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
        return Results.Unauthorized();

    var profile = await db.Users.SingleOrDefaultAsync(u => u.Id == userId);
    if (profile is null)
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
    await db.AddAsync(city);
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
    if(request.CityId <= 0)
        return Results.BadRequest();

    var city = await db.City.SingleAsync(c => c.Id == request.CityId);
    if(city is null)
        return Results.NotFound();
  
    var exists = await db.Origins.AnyAsync( o => o.CityId == request.CityId);
    if(exists)
        return Results.Conflict();


    Origin origin = new Origin
    {
        CityId = request.CityId
    };
    await db.AddAsync(origin);
    await db.SaveChangesAsync();
    return Results.Created();


    
});
origins.MapGet("/list", async (AppDbContext db) =>
{
    var response = await db.Origins.Include(r => r.City).Select( r => new GetOriginResponseDto
    {
        Id = r.Id,
        CityAcronym = r.City!.Acronym,
        CityId = r.CityId
    }).ToListAsync();

    return Results.Ok(response);
});

destination.MapPost("/create", async (AppDbContext db, CreateDestinationRequestDto request) =>
{
    if(request.CityId <= 0)
        return Results.BadRequest();

    var city = await db.City.SingleAsync(c => c.Id == request.CityId);
    if(city is null)
        return Results.NotFound();


    var exists = await db.Destinations.AnyAsync( d => d.CityId == request.CityId);
    if(exists)
        return Results.Conflict();

    Destination destination = new Destination
    {
        CityId = request.CityId
    };

    await db.AddAsync(destination);
    await db.SaveChangesAsync();
    return Results.Created();

});
destination.MapGet("/list", async (AppDbContext db) =>
{
    var response = await db.Destinations.Include(r => r.City).Select( r => new GetDestinationDestinationDto
    {
        Id = r.Id,
        CityAcronym = r.City!.Acronym,
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
    if(request.OriginId <= 0 ||
        request.DestinationId <= 0 ||
        request.Kilometers <= 0)
        return Results.BadRequest();


    var origin = await db.Origins.AnyAsync(o => o.Id == request.OriginId);
    var destination = await db.Destinations.AnyAsync(d => d.Id == request.DestinationId);
    if(!destination || !origin)
        return Results.NotFound();

    if(request.OriginId == request.DestinationId)
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
        Kilometers = request.Kilometers

    };
    await db.Distances.AddAsync(distance);
    await db.SaveChangesAsync();
    return Results.Created();



});
distance.MapGet("/list", async (AppDbContext db) =>
{
    var distance = await db.Distances.Select(d => new GetDistanceResponseDto
    {
        Id = d.Id,
        OriginId = d.OriginId,
        DestinationId = d.DestinationId,
        Kilometers = d.Kilometers

    }).ToListAsync();
    if(distance is null)
        return Results.NotFound();

    return Results.Ok(distance);
});

routes.MapPost("/create", async (AppDbContext db, CreateRouteRequestDto request) =>
{
    // validar request -> return 400
    if(string.IsNullOrWhiteSpace(request.RouteName) ||
        request.Seat <= 0  ||
        request.DistanceId <= 0 )
        return Results.BadRequest();


    // validar conflict -> return 409
    var exists = await db.Routes.AnyAsync(r => r.RouteName == request.RouteName && r.DistanceId == request.DistanceId);
    if(exists)
        return Results.Conflict();

    
    var query = await db.Distances.Where(d => d.Id == request.DistanceId).Include(p => p.Prices).SingleOrDefaultAsync();
    if(query is null || query.Prices is null)
        return Results.NotFound();

    var selectedPrice = query.Prices.OrderByDescending(p => p.Id).FirstOrDefault();
    if(selectedPrice is null)
        return Results.NotFound();

    
    var price = query.Kilometers * selectedPrice.PricePerKm; 
    Route route = new()
    {
        RouteName = request.RouteName,
        DistanceId = request.DistanceId,
        Seat = request.Seat,
        Price = price,
        CreatedAt = DateTime.UtcNow
    };
    await db.AddAsync(route);
    await db.SaveChangesAsync();
    return Results.Created();

});
routes.MapGet("/list", async (AppDbContext db) =>
{

    var routes = await db.Routes.Select(r => new GetRouteResponseDto
    {
        RouteName = r.RouteName,
        DistanceId = r.DistanceId,
        Kilometers = r.Distance!.Kilometers,
        
        

   }).ToListAsync(); 
    if(routes is null)
        return Results.NotFound();

    return Results.Ok(routes);
});
routes.MapGet("/list/{id:int}", async (int id, AppDbContext db) =>
{
    var route = await db.Routes.Where(r => r.Id == id).Select(r => new GetRouteResponseDto
    {
        RouteName = r.RouteName,
        Kilometers = r.Distance!.Kilometers
    }).ToListAsync();

});
routes.MapPatch("/update/{id:int}", async (int id, AppDbContext db, UpdateRouteRequestDto request) =>
{
    if(request.Price <= 0)
        return Results.BadRequest();

    // var route = await db.Routes.Where(r => r.Id == id).Include(p => p.Prices).SingleOrDefaultAsync();
    // if(route is null || route.Distance is null || route.Prices is null)
    //     return Results.NotFound();

    // var price = route.Distance.Kilometers * route.Prices.PricePerKm;

    // route.Price = price;
    await db.SaveChangesAsync();
    return Results.Ok();
});

prices.MapPost("/create", async (AppDbContext db, CreatePriceRequestDto request) =>
{

    if(request.DistanceId <= 0 || request.PricePerKm <= 0)
        return Results.BadRequest();


    var distance = await db.Distances.AnyAsync(r => r.Id == request.DistanceId);
    if(!distance)
        return Results.NotFound();


    Price price = new()
    {
      DistanceId = request.DistanceId,  
      PricePerKm = request.PricePerKm
    };

    await db.AddAsync(price);
    await db.SaveChangesAsync();
    return Results.Created();



});


tickets.MapPost("/create", async (AppDbContext db, CreateTicketRequestDto request, ClaimsPrincipal user) =>
{
    var userIdClaim = user.FindFirstValue(ClaimTypes.NameIdentifier);
    if(string.IsNullOrWhiteSpace(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
        return Results.Unauthorized();

    var profile = await db.Users.SingleAsync( u => u.Id == userId);
    if(profile is null)
        return Results.NotFound();


    
    if(request.RouteId <= 0)
        return Results.BadRequest(); 

    var route = await db.Routes.SingleOrDefaultAsync( r => r.Id == request.RouteId);
    if(route is null)
        return Results.NotFound();

    Ticket ticket = new()
    {
      UserId = userId,
      RouteId = request.RouteId,
      PurchasedOn = DateTime.UtcNow,  
    };
    await db.Tickets.AddAsync(ticket);
    await db.SaveChangesAsync();
    return Results.Created($"/api/tickets/{ticket}", ticket);


});
app.Run();
