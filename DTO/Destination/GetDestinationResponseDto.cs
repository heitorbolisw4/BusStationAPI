namespace BusStation_API.DTO.Destination
{
    public class GetDestinationDestinationDto
    {
        public int Id { get; set; }
        public string CityAcronym { get; set; } = string.Empty;
        public int CityId { get; set; }
    }
}