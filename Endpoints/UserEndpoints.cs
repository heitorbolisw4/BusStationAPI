using System.Security.Claims;
using BusStation_API.Data;
using BusStation_API.Entities;
using BusStation_API.Interface;
using Microsoft.EntityFrameworkCore;
using static BusStation_API.DTO.UserContracts;

namespace BusStation_API.Endpoints
{
    public static class UserEndpoints
    {
        public static IEndpointRouteBuilder MapUserEndpoints(this IEndpointRouteBuilder app)
        {
            var group = app.MapGroup("/me").RequireAuthorization();
            group.MapGet("/", GetProfile);
            group.MapPut("/password", UpdatePassword);
            group.MapPut("/email", UpdateEmail);

            return app;
        }

        private static async Task<IResult> GetProfile(ClaimsPrincipal user, AppDbContext db)
        {
            var profile = await GetCurrentUser(user, db);
            if(profile is null)
                return Results.Unauthorized();

            return Results.Ok(ToProfile(profile));
        }
        private static async Task<IResult> UpdatePassword(ClaimsPrincipal user, UpdatePasswordRequest request, AppDbContext db, IAuthService service)
        {
            var profile = await GetCurrentUser(user, db);
            if( profile is null )
                return Results.Unauthorized();

            if(!service.PasswordVerify(request.Password, profile.Password))
                return Results.BadRequest( new { message = "Current password is incorrect. " } );

            if(string.IsNullOrWhiteSpace(request.NewPassword) || request.NewPassword.Length < 6)
                return Results.BadRequest( new { message = "New password must be at least 6 characters long" } );

            profile.Password =  service.GenerateHash( request.NewPassword );

            await db.SaveChangesAsync();
            return Results.NoContent();

        }
        private static async Task<IResult> UpdateEmail(ClaimsPrincipal user, UpdateEmailRequest request, AppDbContext db, IAuthService service)
        {
            var profile = await GetCurrentUser(user, db);
            if( profile is null )
                return Results.Unauthorized();

            if(!service.PasswordVerify(request.Password, profile.Password))
                return Results.BadRequest( new { message = "Password is incorrect " } );

            if(string.IsNullOrWhiteSpace(request.Email) || !request.Email.Contains('@'))
                return Results.BadRequest( new { message = " A valid email is required " } );

            if(await db.Users.AnyAsync(e => e.Email == request.Email && e.Id != profile.Id))
                return Results.BadRequest(new { message = "a User with this email already exists."});


            profile.Email = request.Email;
            await db.SaveChangesAsync();
            return Results.NoContent();
            
        }
        private static async Task<User?> GetCurrentUser(ClaimsPrincipal user, AppDbContext db)
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if(userId is null || !int.TryParse(userId, out var id))
                return null;

            return await db.Users.FindAsync(id);
        }
        private static UserProfileResponse ToProfile(User user)
        {
            return new UserProfileResponse(user.Id, user.Name, user.Email, user.Age);
        }
    }
}