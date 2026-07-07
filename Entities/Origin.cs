namespace BusStation_API.Entities
{
    public class Origin
    {
        public int Id { get; set; }
        public string OriginName { get; set; } = string.Empty;
        public int CityId { get; set; }
        public City? City { get; set; }
        public List<Distance>? Distances { get; set; }
    }
}