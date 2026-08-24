using BusStation_API.Data;
using static BusStation_API.DTO.BoardingContracts;

namespace BusStation_API.Endpoints
{
    public class BoardingEndpoints
    {




        private static async Task<IResult> CreateBoarding(CreateBoardingRequest request, AppDbContext db)
        {
            var error = ValidateBoarding(request.RouteId, request.Seats, request.BoardingDate);
            if(error is not null)
                return Results.BadRequest( new { message = error } );

            return Results.Ok();
        }







        private static string? ValidateBoarding(int routeId, int seats, DateOnly boardingDate)
        {
            var today = DateOnly.FromDateTime(DateTime.Now);
            if( routeId <= 0 )
                return "You must provide a valid route!";
            if( seats <= 0 )
                return "You must provide a valid number of seats";

            if( boardingDate < today )
                return "You must provide a valid date";

            return null;
        }
    }
}