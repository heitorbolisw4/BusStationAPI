namespace BusStation_API.DTO.Distance
{
    public class GetDistanceResponseDto
    {
        public Guid Id { get; set; }
        public int OriginId { get; set; } 
        public int DestinationId { get; set; } 
        public int Kilometers { get; set; }
    }
}