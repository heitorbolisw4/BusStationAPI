namespace BusStation_API.Entities
{
    public class Boarding
    {
        public int Id { get; set; }
        public int TicketId { get; set; }
        public int Seat { get; set; }
        public DateTime BoardingDate { get; set; }
        public List<Route>? Routes { get; set; }
        public Ticket? Ticket { get; set; }
    }
}