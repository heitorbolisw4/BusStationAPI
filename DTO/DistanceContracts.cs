namespace BusStation_API.DTO
{
    public class DistanceContracts
    {
        public record CreateDistanceRequest(int OriginCityId, int DestinationCityId, int Kilometers);
    }
}