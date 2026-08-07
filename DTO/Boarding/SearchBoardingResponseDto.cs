namespace BusStation_API.DTO.Boarding
{
    /// <summary>
    /// Uma linha do painel de saídas do site. Junta Boarding + Route + Distance
    /// + City para que o front monte o card com UMA requisição.
    /// BoardingId e RouteId existem aqui porque são exatamente o corpo do
    /// POST /tickets/create — sem eles o cliente vê a saída e não consegue comprar.
    /// </summary>
    public class SearchBoardingResponseDto
    {
        public int BoardingId { get; set; }
        public int RouteId { get; set; }
        public string RouteName { get; set; } = string.Empty;

        public string OriginCity { get; set; } = string.Empty;
        public string OriginAcronym { get; set; } = string.Empty;
        public string DestinationCity { get; set; } = string.Empty;
        public string DestinationAcronym { get; set; } = string.Empty;

        public int Kilometers { get; set; }
        public DateOnly BoardingDate { get; set; }
        public TimeOnly BoardingTime { get; set; }
        public int Seats { get; set; }

        // Preço de venda já congelado na Route (km x PricePerKm no momento
        // da criação). PricePerKm não aparece aqui de propósito: é
        // configuração de admin, não interessa ao cliente.
        public float Price { get; set; }
    }
}
