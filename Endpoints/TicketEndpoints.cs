using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using BusStation_API.Data;
using BusStation_API.Entities;
using Microsoft.EntityFrameworkCore;
using static BusStation_API.DTO.TicketContracts;

namespace BusStation_API.Endpoints
{
    public static class TicketEndpoints
    {

        private static async Task<IResult> CreateTicket(CreateTicketRequest request, AppDbContext db, ClaimsPrincipal user)
        {
            var profile = GetCurrentUser(user);
            if(profile is null)
            {
                return Results.Unauthorized();
            }
                        
            var error = ValidateTicket(request.RouteId, request.BoardingId);
            if(error is not null)
                return Results.BadRequest( new { message = error } );


            var now = DateTime.UtcNow;
            var today = DateOnly.FromDateTime(now);
            var currentTime = TimeOnly.FromDateTime(now);

            var boarding = await db.Boardings.Where(x => x.Id == request.BoardingId).Include(x => x.Routes).Where(x => x.RouteId ==request.RouteId).FirstOrDefaultAsync();

            if(boarding is null || boarding.Routes is null)
                return Results.NotFound();

            if( boarding.Seat <= 0 )
                return Results.BadRequest("Dont have seats");



            Ticket ticket = new()
            {
              RouteId = request.RouteId,
              FarePaid = boarding.Routes.Price,
              PurchasedOn = DateTime.UtcNow,
              BoardingDate = boarding.BoardingDate
            };
            
            
            
            db.Add(ticket);
            await db.SaveChangesAsync();
            return Results.Created( "/tickets/", ticket);

        }

        private static int? GetCurrentUser(ClaimsPrincipal user)
        {
            var userIdClaim = user.FindFirstValue(JwtRegisteredClaimNames.Sub);
            return userIdClaim is not null && int.TryParse(userIdClaim, out var userId) ? userId : null;  
        }





        private static string? ValidateTicket(int routeId, int boardingId)
        {
            if( routeId <= 0 )
                return "You must enter a valid routeId";
            
            if( boardingId <= 0)
                return "You must enter a valid boardingId";
            
            return null;
        }
    }
}