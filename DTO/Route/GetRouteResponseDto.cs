namespace BusStation_API.DTO.Route
{
    public class GetRouteResponseDto
    {
        public string RouteName { get; set; } = string.Empty;
        public int DistanceId { get; set; }
        public int Kilometers { get; set; }
    }
}