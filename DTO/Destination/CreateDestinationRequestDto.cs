namespace BusStation_API.DTO.Destination
{
    public class CreateDestinationRequestDto
    {
        public string DestinationName { get; set; } = string.Empty;
        public int CityId { get; set; }
    }
}