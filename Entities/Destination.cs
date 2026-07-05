namespace BusStation_API.Entities
{
    public class Destination
    {
        public int Id { get; set; }
        public string DestinationName { get; set; } = string.Empty;
        public List<Distance>? Distances { get; set; }
    }
}