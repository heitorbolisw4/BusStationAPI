namespace BusStation_API.Entities
{
    public class Price
    {
        public int Id { get; set; }
        public float PricePerKm { get; set; }
        public int DistanceId { get; set; }
        public Distance? Distance { get; set; }


        public static string? CreateValidPrice(float pricePerKm, int distanceId)
        {
            if( pricePerKm <= 0)
            {
                return "You must enter a valid Price Per Km";
            }
            if( distanceId <= 0)
            {
                return "You must enter a valid DistanceId";
            }
            return null;
        }
    }
}