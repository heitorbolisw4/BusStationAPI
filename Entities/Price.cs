namespace BusStation_API.Entities
{
    public class Price
    {
        public int Id { get; set; }
        public float PricePerKm { get; set; }
        public int RouteId { get; set; }
        public Route? Route { get; set; }
    }
}