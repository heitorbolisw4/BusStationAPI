namespace BusStation_API.DTO.Boarding
{
    public class CreateBoardingRequestDto
    {
        public int RouteId { get; set; }
        public int Seats { get; set; }
        public DateOnly BoardingDate { get; set; }
        public TimeOnly BoardingTime { get; set; }
    }
}