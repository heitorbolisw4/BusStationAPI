namespace BusStation_API.DTO.Distance
{
    public class GetDistanceResponseDto
    {
        public string Origin { get; set; } = string.Empty;
        public string Destination { get; set; } = string.Empty;
        public int Kilometers { get; set; }
    }
}