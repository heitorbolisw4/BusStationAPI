namespace BusStation_API.DTO
{
    public class RouteContracts
    {
        public record CreateRouteRequest(string RouteName, int DistanceId);
        public record RouteResponse(int Id, string RouteName, int Kilometers);
    }
}