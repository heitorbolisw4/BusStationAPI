namespace BusStation_API.Entities
{
    public class Boarding
    {
        public int Id { get; set; }
        public int RouteId { get; set; }
        public int Seat { get; set; }
        public DateOnly BoardingDate { get; set; }
        public TimeOnly BoardingTime { get; set; }
        public Route? Routes { get; set; }

    }
}