namespace BusStation_API.Entities
{
    public class Route
    {
        public int Id { get; set; }
        public string RouteName { get; set; } = string.Empty;
        public Guid DistanceId { get; set; }
        public List<Ticket>? Tickets { get; set; }
        public Distance? Distance { get; set; }
    }
}