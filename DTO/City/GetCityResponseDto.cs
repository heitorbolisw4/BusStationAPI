namespace BusStation_API.DTO.City
{
    public class GetCityResponseDto
    {
        public int Id { get; set; }
        public string CityName { get; set; } = string.Empty;
        public string State { get; set; }  = string.Empty;
        public string Acronym { get; set; } = string.Empty;
    }
}