namespace BusStation_API.Entities
{
    public class Ticket
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int BoardingId { get; set; }
        public DateTime PurchasedOn { get; set; }
        public DateOnly BoardingDate { get; set; }
        public float FarePaid { get; set; }
        public User? User { get; set; }
        public Boarding? Boarding { get; set; }
    }
}