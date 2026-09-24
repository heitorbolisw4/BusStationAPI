using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using BusStation_API.Configuration;
using BusStation_API.Data;
using BusStation_API.Endpoints;
using BusStation_API.Entities;
using BusStation_API.Interface;
using BusStation_API.Jwt;
using BusStation_API.Service;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Route = BusStation_API.Entities.Route;


#region Aplication
var builder = WebApplication.CreateBuilder(args);

// Fail-fast: sem connection string / chaves JWT (ou CORS fora de dev) a API nem sobe.
StartupConfig.EnsureRequiredSettings(builder.Configuration, builder.Environment);
builder.WebHost.UsePortFromEnvironment(builder.Configuration);

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
builder.Services.AddSingleton<AdminBootstrapper>();
builder.Services.AddHostedService<AdminBootstrapHostedService>();


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
        // Os dois schemes: token de User autentica pelo UserScheme, mas não tem a claim
        // "adm" -> 403 (autenticado, sem permissão). Sem token nenhum -> 401.
        policy.AuthenticationSchemes.Add("AdminScheme");
        policy.AuthenticationSchemes.Add("UserScheme");
        policy.RequireAuthenticatedUser();
        policy.RequireClaim("adm");
    });
});



// CORS lido da config final (Cors:AllowedOrigins) na hora de montar as options, e não
// aqui no bootstrap, para que o teste E2E consiga trocar as origens por host.
builder.Services.AddCors();
builder.Services.AddOptions<CorsOptions>().Configure<IConfiguration, IHostEnvironment>((options, config, env) =>
    options.AddPolicy(StartupConfig.CorsPolicyName, policy =>
        policy.WithOrigins(StartupConfig.AllowedOrigins(config, env)).AllowAnyHeader().AllowAnyMethod()));

builder.Services.AddHealthChecks().AddDbContextCheck<AppDbContext>("postgres", tags: ["ready"]);

// O Render termina o TLS no proxy e repassa HTTP com X-Forwarded-*. O IP do proxy não é
// fixo, então as listas de proxies conhecidos são limpas: só o proxy do provedor
// alcança o container.
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

var app = builder.Build();
app.UseForwardedHeaders();

// Swagger ligado em Development e Staging (ambiente de estudo, ajuda a debugar);
// desligado só com ASPNETCORE_ENVIRONMENT=Production. Ver ARCHITECTURE.md.
if (!app.Environment.IsProduction())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseCors(StartupConfig.CorsPolicyName);


#endregion



#region Groups
var user = app.MapGroup("/user").RequireAuthorization("UserPolicy").WithTags("Users");
// Grupos de catálogo fecham tudo por padrão (AdminPolicy); leitura pública é
// liberada endpoint a endpoint com AllowAnonymous() (GET /cities/list, GET /boardings/search).
var cities =  app.MapGroup("/cities").RequireAuthorization("AdminPolicy").WithTags("Cities");
var routes = app.MapGroup("/routes").RequireAuthorization("AdminPolicy").WithTags("Routes");
var prices = app.MapGroup("/prices").RequireAuthorization("AdminPolicy").WithTags("Prices");
var tickets = app.MapGroup("/tickets").RequireAuthorization("UserPolicy").WithTags("Tickets");
var distances =  app.MapGroup("/distances").RequireAuthorization("AdminPolicy").WithTags("Distances");
var boardings = app.MapGroup("/boardings").RequireAuthorization("AdminPolicy").WithTags("Boardings");
#endregion



// /health é liveness (não toca no banco): é o que o Render consulta periodicamente, e se
// batesse no Postgres o Neon nunca suspenderia e consumiria as horas de compute do free.
// /health/ready inclui o banco, para checagem manual e smoke test.
app.MapHealthChecks("/health", new HealthCheckOptions { Predicate = _ => false }).AllowAnonymous();
app.MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = c => c.Tags.Contains("ready") }).AllowAnonymous();


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
