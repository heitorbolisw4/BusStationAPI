namespace BusStation_API.Entities
{
    public class City
    {
        public int Id { get; set; }
        public string CityName { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;
        public string Acronym { get; set; } = string.Empty;
        public List<Origin>? Origins { get; set; }
        public List<Destination>? Destinations { get; set; }
    }
}