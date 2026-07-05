namespace BusStation_API.Entities
{
    public class Ticket
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public Guid DistanceId { get; set; }
        public float Price { get; set; }

        public User? User { get; set; }
        public Distance? Distance { get; set; }
    }
}