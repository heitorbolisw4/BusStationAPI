using System.Security.Claims;
using System.Text;
using BusStation_API.Data;
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

//builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("JwtSettings"));
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


app.Run();
