namespace BusStation_API.Entities
{
    public class Destination
    {
        public int Id { get; set; }
        public int CityId { get; set; }
        public City? City { get; set; }
        public List<Distance>? Distances { get; set; }
    }
}