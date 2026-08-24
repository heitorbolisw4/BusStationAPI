namespace BusStation_API.DTO
{
    public class BoardingContracts
    {
        public record CreateBoardingRequest(int RouteId, int Seats, DateOnly BoardingDate, TimeOnly BoardingTime);
    }
}