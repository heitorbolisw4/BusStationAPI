namespace BusStation_API.DTO
{
    public class PriceContracts
    {
        public record CreatePriceRequest(int DistanceId, float PricePerKm);
        public record UpdatePriceRequest(int DistanceId, float NewPrice);
        public record PriceResponse(int Id, int DistanceId, float PricePerKm);
    }
}
