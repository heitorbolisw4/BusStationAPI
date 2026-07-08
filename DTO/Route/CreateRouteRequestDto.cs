namespace BusStation_API.DTO.Route
{
    public class CreateRouteRequestDto
    {
        public string RouteName { get; set; } = string.Empty;
        public int Killometers { get; set; }
    }
}