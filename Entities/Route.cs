namespace BusStation_API.Entities
{
    public class Route
    {
        // Fonte única do limite: usada no mapeamento da coluna e na validação do endpoint.
        public const int RouteNameMaxLength = 100;

        public int Id { get; set; }
        public string RouteName { get; set; } = string.Empty;
        public float Price { get; set; }
        public int DistanceId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public List<Boarding>? Boardings { get; set; }
        public Distance? Distance { get; set;}
        public bool IsActive { get; set; }
        
        
    }
}