namespace BusStation_API.Entities
{
    public class Distance
    {
        public int Id { get; set; }
        public int OriginId { get; set; }
        public int DestinationId { get; set; }
        public int Kilometers { get; set; }

        public Destination? Destination { get; set; }
        public Origin? Origin { get; set; }
        public List<Route>? Routes { get; set; }
        public List<Price>? Prices { get; set; }

    }
}