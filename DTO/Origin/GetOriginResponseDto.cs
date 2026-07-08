namespace BusStation_API.DTO.Origin
{
    public class GetOriginResponseDto
    {
        public int Id { get; set; }
        public string OriginName { get; set; } = string.Empty;
        public int CityId { get; set; }        
    }
}