namespace BusStation_API.DTO.Ticket
{
    public class CreateTicketRequestDto
    {
        public int RouteId { get; set; }
        public int NumberOfSeats { get; set; }
    }
}