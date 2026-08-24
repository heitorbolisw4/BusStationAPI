namespace BusStation_API.Entities
{
    public class City
    {
        public int Id { get; set; }
        public string CityName { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;
        public string Acronym { get; set; } = string.Empty;
        public List<Distance>? OriginCities { get; set; }
        public List<Distance>? DestinationCities { get; set; }
    }
}