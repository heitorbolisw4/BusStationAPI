namespace BusStation_API.DTO.Price
{
    public class UpdatePriceRequestDto
    {
        public int DistanceId { get; set; }
        public float NewPrice { get; set; }
    }
}