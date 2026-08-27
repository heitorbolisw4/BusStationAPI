using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using BusStation_API.Data;
using BusStation_API.DTO.Admin;
using BusStation_API.DTO.Boarding;
using BusStation_API.DTO.Destination;
using BusStation_API.DTO.Distance;
using BusStation_API.DTO.Origin;
using BusStation_API.DTO.Price;
using BusStation_API.DTO.Ticket;
using BusStation_API.Endpoints;
using BusStation_API.Entities;
using BusStation_API.Interface;
using BusStation_API.Jwt;
using BusStation_API.Service;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Route = BusStation_API.Entities.Route;


#region Aplication
var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));

//builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("JwtSettings"));
builder.Services.Configure<JwtUserOptions>(builder.Configuration.GetSection("JwtSettings:User"));
builder.Services.Configure<JwtAdminOptions>(builder.Configuration.GetSection("JwtSettings:Admin"));


//var jwtSettings = builder.Configuration.GetSection("JwtSettings").Get<JwtSettings>();
var adminJwt = builder.Configuration.GetSection("JwtSettings:Admin").Get<JwtAdminOptions>();
var userJwt = builder.Configuration.GetSection("JwtSettings:User").Get<JwtUserOptions>();

builder.Services.AddSingleton<TokenService>();
builder.Services.AddSingleton<ITokenService<User>, UserTokenService>();
builder.Services.AddSingleton<ITokenService<Admin>, AdminTokenService>();
builder.Services.AddSingleton<IAuthService, AuthService>();


builder.Services.Configure<Microsoft.AspNetCore.Http.Json.JsonOptions>(options => options.SerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles);
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer("UserScheme",options =>
{

   options.TokenValidationParameters = new TokenValidationParameters
   {
       ValidateIssuer = true,
       ValidIssuer = userJwt!.Issuer,
       
       ValidateAudience = true,
       ValidAudience = userJwt!.Audience,
       
       ValidateIssuerSigningKey = true,
       IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(userJwt.SecretKey)),
       
       ValidateLifetime = true,
       ClockSkew = TimeSpan.Zero,
       
       NameClaimType = ClaimTypes.NameIdentifier
   }; 
}).AddJwtBearer("AdminScheme",options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidIssuer = adminJwt!.Issuer,

        ValidateAudience = true,
        ValidAudience = adminJwt.Audience,

        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(adminJwt.SecretKey)),

        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "BusStationApi", Version = "v1"});
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
       In = ParameterLocation.Header,
       Type = SecuritySchemeType.Http,
       Scheme = "bearer",
       BearerFormat = "JWT"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        } 
    });
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("UserPolicy", policy => 
    {
        policy.AuthenticationSchemes.Add("UserScheme");
        policy.RequireAuthenticatedUser();    
    });
    options.AddPolicy("AdminPolicy", policy =>
    {
        policy.AuthenticationSchemes.Add("AdminScheme");
        policy.RequireAuthenticatedUser();
        policy.RequireClaim("adm");
    });
});



builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowSpecificOrigin", policy =>
    {
       policy.WithOrigins("http://localhost:5173").AllowAnyHeader().AllowAnyMethod(); 
    });


});

var app = builder.Build();
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseCors("AllowSpecificOrigin");
}


#endregion



#region Groups
var user = app.MapGroup("/user").RequireAuthorization("UserPolicy").WithTags("Users");
var admin = app.MapGroup("/admin").WithTags("Admins");
var cities =  app.MapGroup("/cities").RequireAuthorization("AdminPolicy").WithTags("Cities");
var routes = app.MapGroup("/routes");//.RequireAuthorization("AdminPolicy").WithTags("Routes");
var prices = app.MapGroup("/prices").RequireAuthorization("AdminPolicy").WithTags("Prices");
var origins = app.MapGroup("/origins").RequireAuthorization("AdminPolicy").WithTags("Origins");
var tickets = app.MapGroup("/tickets").RequireAuthorization().WithTags("Tickets");
var distance = app.MapGroup("/distance").RequireAuthorization("AdminPolicy").WithTags("Distances");
var distances =  app.MapGroup("/distances").RequireAuthorization("AdminPolicy").WithTags("Distances");
var boardings = app.MapGroup("/boardings").WithTags("Boardings");
var destination = app.MapGroup("/destination").RequireAuthorization("AdminPolicy").WithTags("Destinations");
#endregion



#region Admins

admin.MapPost("/create", async (AppDbContext db, AdminRequestDto request, IAuthService service) =>
{
    if(string.IsNullOrWhiteSpace(request.Name) ||
        string.IsNullOrWhiteSpace(request.Email) ||
        string.IsNullOrWhiteSpace(request.Password) )
        return Results.BadRequest();

    //valido se email existe
    var exists = await db.Admins.AnyAsync(x => x.Email ==  request.Email);
    if(exists)
        return Results.Conflict();

    // faz o hash da senha
    string doHashPassw = service.GenerateHash(request.Password);

    Admin adm = new()
    {
        Name = request.Name,
        Email = request.Email,
        Password = doHashPassw
    };
    db.Add(adm);
    await db.SaveChangesAsync();
    return Results.Created();
});

#endregion


#region Boardings


// boardings.MapGet("/search", async (AppDbContext db, int originCityId, int destinationCityId, DateOnly date) =>
// {
//     // valido request -> return 400
//     if(originCityId <= 0 || destinationCityId <= 0 || originCityId == destinationCityId)
//         return Results.BadRequest();

//     var now = DateTime.Now;
//     var today = DateOnly.FromDateTime(now);

//     // valido data -> return 400
//     if(date < today)
//         return Results.BadRequest();

//     // se a busca é para hoje, saída que já partiu não interessa ao cliente
//     var minTime = date == today ? TimeOnly.FromDateTime(now) : TimeOnly.MinValue;

//     var departures = await db.Boardings
//         .Where(b => b.BoardingDate == date
//                  && b.BoardingTime >= minTime
//                  && b.Seat > 0
//                  && b.Routes!.Distance!.Origin!.CityId == originCityId
//                  && b.Routes.Distance.Destination!.CityId == destinationCityId)
//         .OrderBy(b => b.BoardingTime)
//         .Select(b => new SearchBoardingResponseDto
//         {
//             BoardingId = b.Id,
//             RouteId = b.RouteId,
//             RouteName = b.Routes!.RouteName,

//             OriginCity = b.Routes.Distance!.Origin!.City!.CityName,
//             OriginAcronym = b.Routes.Distance.Origin.City.Acronym,
//             DestinationCity = b.Routes.Distance.Destination!.City!.CityName,
//             DestinationAcronym = b.Routes.Distance.Destination.City.Acronym,

//             Kilometers = b.Routes.Distance.Kilometers,
//             BoardingDate = b.BoardingDate,
//             BoardingTime = b.BoardingTime,
//             Seats = b.Seat,
//             Price = b.Routes.Price
//         })
//         .ToListAsync();

//     // Faltando de propósito: `&& b.Routes!.IsActive`. POST /routes/create nunca
//     // seta IsActive, então toda rota no banco está com false — o filtro
//     // devolveria lista vazia sempre. Acrescente aqui quando o create setar true.

//     // Lista vazia é busca bem-sucedida com zero resultados, não erro:
//     // 200 com [] deixa o front escrever "nenhuma saída nesse dia".
//     return Results.Ok(departures);
// });

#endregion

#region Prices
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
prices.MapPatch("/update/{id:int}", async (int id, AppDbContext db, UpdatePriceRequestDto request) =>
{
    if(request.NewPrice <= 0 || request.DistanceId <= 0)
        return Results.BadRequest();

    var price = await db.Prices.FirstOrDefaultAsync(x => x.Id == id && x.DistanceId == request.DistanceId);
    if(price is null)
        return Results.NotFound();

    price.PricePerKm = request.NewPrice;

    var route = await db.Routes.Where(x => x.DistanceId == request.DistanceId).Include(y => y.Distance).SingleOrDefaultAsync();
    if(route is null || route.Distance is null)
        return Results.NotFound();
    
    if(route.Distance.Id != request.DistanceId)
        return Results.NotFound();

    
    var newPrice = price.PricePerKm * route.Distance.Kilometers;
    route.Price = newPrice;

    await db.SaveChangesAsync();
    return Results.Created();
    
});
prices.MapGet("/list", async (AppDbContext db) =>
{
    var prices = await db.Prices.Select( x => new ListPricesRequestDto
    {
        PricePerKm = x.PricePerKm,
        DistanceId = x.DistanceId

    }).ToListAsync();
    if(prices is null)
        return Results.NotFound();


    return Results.Ok(prices);
});
//prices.MapDelete
#endregion

#region Tickets
tickets.MapPost("/create", async (AppDbContext db, CreateTicketRequestDto request, ClaimsPrincipal user) =>
{
    var userIdClaim = user.FindFirstValue(ClaimTypes.NameIdentifier);
    if(string.IsNullOrWhiteSpace(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
        return Results.Unauthorized();

    var profile = await db.Users.SingleAsync( u => u.Id == userId);
    if(profile is null)
        return Results.NotFound();
    
    if(request.RouteId <= 0 || request.BoardingId <= 0)
        return Results.BadRequest(); 

    
    
    
    var now = DateTime.UtcNow;
    var today = DateOnly.FromDateTime(now);
    var currentTime = TimeOnly.FromDateTime(now);

    // eu localizo a route e boarding
    var boarding = await db.Boardings.Where(x => x.Id == request.BoardingId)
    .Include(x => x.Routes).Where(x => x.RouteId == request.RouteId).FirstOrDefaultAsync();

    if(boarding is null || boarding.Routes is null)
        return Results.NotFound();

    
    if(boarding.Seat <= 0)
        return Results.BadRequest("Dont have seats");


    // eu monto o ticket
    Ticket ticket = new()
    {
        RouteId = request.RouteId,        
        FarePaid = boarding.Routes.Price,
        PurchasedOn = DateTime.UtcNow,
        BoardingDate = boarding.BoardingDate,
        UserId = userId
    };
    boarding.Seat -= 1;
    await db.AddAsync(ticket);
    await db.SaveChangesAsync();
    return Results.Created();

});
//tickets.MapPut
//tickets.MapGet
//tickets.MapDelete
#endregion

app.MapSwagger();


app.MapAuthEndpoints();
user.MapUserEndpoints();
boardings.MapBoardingsEndpoints();
routes.MapRouteEndpoints();
cities.MapCitiesEnpoints();
app.MapDistanceEndpoints();

app.UseAuthentication();
app.UseAuthorization();


app.Run();
