namespace BusStation_API.Entities
{
    public class Route
    {
        public int Id { get; set; }
        public string RouteName { get; set; } = string.Empty;
        public int Seat { get; set; }
        public float Price { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public int DistanceId { get; set; }
        public List<Ticket>? Tickets { get; set; }
        public Distance? Distance { get; set;}
        public List<Price>? Prices { get; set; }
        
        
    }
}