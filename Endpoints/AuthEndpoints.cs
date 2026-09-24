using BusStation_API.Data;
using BusStation_API.DTO;
using BusStation_API.Entities;
using BusStation_API.Interface;
using BusStation_API.Service;
using Microsoft.EntityFrameworkCore;

namespace BusStation_API.Endpoints
{
    public static class AuthEndpoints
    {
        public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
        {
            app.MapPost("/register", Register);
            app.MapPost("/login", Login);
            app.MapPost("/refresh", Refresh);
            app.MapPost("/logout", Logout);

            // Só admin cria admin. O primeiro vem do seed (AdminBootstrapper, BOOTSTRAP_ADMIN_*).
            app.MapPost("/admin/create", AdminRegister).RequireAuthorization("AdminPolicy");
            app.MapPost("/admin/login", AdminLogin);


            return app;
        }
        private static async Task<IResult> Register(RegisterRequest request, AppDbContext db, IAuthService service)
        {
            var error = ValidateRegistration(request.Name, request.Email, request.Password, request.Age);
            if(error is not null )
                return Results.BadRequest(new { message = error } );

            if( await db.Users.AnyAsync(u => u.Email == request.Email))
                return Results.Conflict(new { message = "A User with this email already exists" });
            
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

        }

        private static async Task<IResult> Login(LoginRequest request, AppDbContext db, IAuthService service, UserTokenService tokenService, RefreshTokenService refreshTokens)
        {
            var user = await db.Users.SingleOrDefaultAsync(u => u.Email == request.Email);
            if(user is null || !service.PasswordVerify(request.Password, user.Password))
                return Results.Unauthorized();

            var refreshToken = await refreshTokens.IssueAsync(user.Id);
            return Results.Ok(TokenPair(user, tokenService, refreshToken));
        }

        private static async Task<IResult> Refresh(RefreshRequest request, UserTokenService tokenService, RefreshTokenService refreshTokens)
        {
            var rotated = await refreshTokens.RotateAsync(request.RefreshToken);
            if(rotated is null)
                return Results.Unauthorized();

            var (user, refreshToken) = rotated.Value;
            return Results.Ok(TokenPair(user, tokenService, refreshToken));
        }

        // Sempre 204: logout é idempotente e não revela se o token existia.
        private static async Task<IResult> Logout(RefreshRequest request, RefreshTokenService refreshTokens)
        {
            await refreshTokens.RevokeAsync(request.RefreshToken);
            return Results.NoContent();
        }

        private static TokenPairResponse TokenPair(User user, UserTokenService tokenService, string refreshToken) =>
            new(tokenService.GenerateToken(user), refreshToken, (int)tokenService.AccessTokenLifetime.TotalSeconds);


        // admin Endpoints
        private static async Task<IResult> AdminRegister(AdminRegisterRequest request, AppDbContext db, IAuthService service)
        {
            if(string.IsNullOrWhiteSpace(request.Name) ||
                string.IsNullOrWhiteSpace(request.Email) ||
                string.IsNullOrWhiteSpace(request.Password) )
                return Results.BadRequest();

            if(await db.Admins.AnyAsync(a => a.Email == request.Email))
                return Results.Conflict();

            string doHashPassw = service.GenerateHash(request.Password);
            Admin admin = new()
            {
                Name = request.Name,
                Email = request.Email,
                Password = doHashPassw
            };

            db.Admins.Add(admin);
            await db.SaveChangesAsync();
            return Results.Created();
        }

        private static async Task<IResult> AdminLogin(AdminLoginRequest request, AppDbContext db, IAuthService service, ITokenService<Admin> tokenService)
        {
            var admin = await db.Admins.SingleOrDefaultAsync(a => a.Email == request.Email);
            if(admin is null || !service.PasswordVerify(request.Password, admin.Password))
                return Results.Unauthorized();

            var token = tokenService.GenerateToken(admin);
            return Results.Ok(new {token});
        }

        private static string? ValidateRegistration(string name, string email, string password, int age)
        {
            if(string.IsNullOrWhiteSpace(name))
                return "Name is required.";

            if(string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
                return "A Valid email is required";

            if(string.IsNullOrWhiteSpace(password) || password.Length < 6 )
                return "Password must be at least 6 characters long.";

            if( age < 18)
                return "You must be an adult bro";


            return null;
        }
    }
}
