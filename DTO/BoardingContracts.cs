namespace BusStation_API.DTO
{
    public class BoardingContracts
    {
        public record CreateBoardingRequest(int RouteId, int Seats, DateOnly BoardingDate, TimeOnly BoardingTime);

        // Uma linha do painel de saídas do site. Junta Boarding + Route + Distance + City
        // para que o front monte o card com UMA requisição. BoardingId é o corpo do
        // POST /tickets/create. PricePerKm fica de fora de propósito: é config de admin.
        public record SearchBoardingResponse(
            int BoardingId,
            int RouteId,
            string RouteName,
            string OriginCity,
            string OriginAcronym,
            string DestinationCity,
            string DestinationAcronym,
            int Kilometers,
            DateOnly BoardingDate,
            TimeOnly BoardingTime,
            int Seats,
            float Price);
    }
}