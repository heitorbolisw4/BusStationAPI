namespace BusStation_API.Entities
{
    public class Route
    {
        public int Id { get; set; }
        public string RouteName { get; set; } = string.Empty;
        public float Price { get; set; }
        public int DistanceId { get; set; }
        public int BoardingId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public Boarding? Boarding { get; set; }
        public Distance? Distance { get; set;}
        public bool IsActive { get; set; }
        
        
    }
}