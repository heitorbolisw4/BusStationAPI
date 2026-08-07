namespace BusStation_API.DTO.Boarding
{
    public class ListBoardingResponseDto
    {
        public int BoardinId { get; set; }
        public int RouteId { get; set; }
        public int Seats { get; set; }
        public DateOnly BoardingDate { get; set; }
        public TimeOnly BoardingTime { get; set; }
    }
}