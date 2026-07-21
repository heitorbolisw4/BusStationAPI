namespace BusStation_API.Entities
{
    public class Destination
    {
        public int Id { get; set; }
        public int CityId { get; set; }
        public string CityAcronym { get; set; } = string.Empty;
        public City? City { get; set; }
        public List<Distance>? Distances { get; set; }
    }
}