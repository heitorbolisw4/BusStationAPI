namespace BusStation_API.DTO.Origin
{
    public class CreateOriginRequestDto
    {
        public string OriginName { get; set; } = string.Empty;
        public int CityId { get; set; }
    }
}