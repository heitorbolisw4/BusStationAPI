namespace BusStation_API.DTO.Distance
{
    public class CreateDistanceRequestDto
    {
        public int OriginId { get; set; }
        public int DestinationId { get; set; }
        public int Kilometers { get; set; }
    }
}