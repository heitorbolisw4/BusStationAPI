using BusStation_API.Data;
using BusStation_API.DTO;
using BusStation_API.Entities;
using BusStation_API.Interface;
using Microsoft.EntityFrameworkCore;

namespace BusStation_API.Endpoints
{
    public static class AuthEndpoints
    {
        public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
        {
            app.MapPost("/register", Register);
            app.MapPost("/login", Login);

            app.MapPost("/admin/create", AdminRegister);
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

        private static async Task<IResult> Login(LoginRequest request, AppDbContext db, IAuthService service, ITokenService<User> tokenService)
        {
            var user = await db.Users.SingleOrDefaultAsync(u => u.Email == request.Email);
            if(user is null || !service.PasswordVerify(request.Password, user.Password))
                return Results.Unauthorized();

            var token = tokenService.GenerateToken(user);
            return Results.Ok(new {token});
        }


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
            if(admin is null || service.PasswordVerify(request.Password, admin.Password))
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
