namespace BusStation_API.DTO
{
    public class TicketContracts
    {
        public record CreateTicketRequest(int RouteId, int BoardingId);
    }
}