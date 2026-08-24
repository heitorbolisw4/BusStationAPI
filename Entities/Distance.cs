namespace BusStation_API.Entities
{
    public class Distance
    {
        public int Id { get; set; }
        public int OriginCityId { get; set; }
        public int DestinationCityId { get; set; }
        public int Kilometers { get; set; }
        public City? DestinationCity { get; set; }
        public City? OriginCity { get; set; }
        public List<Route>? Routes { get; set; }
        public List<Price>? Prices { get; set; }

    }
}