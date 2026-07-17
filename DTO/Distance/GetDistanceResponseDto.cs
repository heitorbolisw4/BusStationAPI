namespace BusStation_API.DTO.Distance
{
    public class GetDistanceResponseDto
    {
        public int Id { get; set; }
        public int OriginId { get; set; } 
        public int DestinationId { get; set; } 
        public int Kilometers { get; set; }
    }
}