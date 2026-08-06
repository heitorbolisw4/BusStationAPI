using BusStation_API.Entities;

namespace BusStation_API.DTO.Price
{
    public class ListPricesRequestDto
    {
        public int DistanceId { get; set; }
        public float PricePerKm { get; set; }
    }
}