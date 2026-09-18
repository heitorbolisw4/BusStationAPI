namespace BusStation_API.DTO
{
    public class TicketContracts
    {
        public record CreateTicketRequest(int BoardingId);
        public record TicketResponse(
            int Id,
            int BoardingId,
            int RouteId,
            string RouteName,
            DateOnly BoardingDate,
            TimeOnly BoardingTime,
            float FarePaid,
            DateTime PurchasedOn);
    }
}
