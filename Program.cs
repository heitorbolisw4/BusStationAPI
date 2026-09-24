using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using BusStation_API.Data;
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
var cities =  app.MapGroup("/cities");//RequireAuthorization("AdminPolicy").WithTags("Cities");
var routes = app.MapGroup("/routes");//.RequireAuthorization("AdminPolicy").WithTags("Routes");
var prices = app.MapGroup("/prices").RequireAuthorization("AdminPolicy").WithTags("Prices");
var tickets = app.MapGroup("/tickets").RequireAuthorization("UserPolicy").WithTags("Tickets");
var distances =  app.MapGroup("/distances").RequireAuthorization("AdminPolicy").WithTags("Distances");
var boardings = app.MapGroup("/boardings").WithTags("Boardings");
#endregion



app.MapSwagger();


app.MapAuthEndpoints();
user.MapUserEndpoints();
boardings.MapBoardingsEndpoints();
routes.MapRouteEndpoints();
cities.MapCitiesEnpoints();
distances.MapDistanceEndpoints();
prices.MapPriceEndpoints();
tickets.MapTicketEndpoints();

app.UseAuthentication();
app.UseAuthorization();


app.Run();

// Expõe o Program gerado pelos top-level statements para o WebApplicationFactory dos testes E2E.
public partial class Program { }
