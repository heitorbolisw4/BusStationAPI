namespace BusStation_API.Entities
{
    public class Distance
    {
        public Guid Id { get; set; }
        public int OriginId { get; set; }
        public int DestinationId { get; set; }
        public int Quilometers { get; set; }

        public Destination? Destination { get; set; }
        public Origin? Origin { get; set; }
    }
}