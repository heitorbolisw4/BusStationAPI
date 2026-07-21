namespace BusStation_API.Entities
{
    public class Price
    {
        public int Id { get; set; }
        public float PricePerKm { get; set; }
        public int DistanceId { get; set; }
        public Distance? Distance { get; set; }
    }
}