namespace BusStation_API.DTO.Price
{
    public class CreatePriceRequestDto
    {
        public int DistanceId { get; set; }
        public float PricePerKm { get; set; }
    }
}