namespace BusStation_API.E2E.Infrastructure
{
    // Espelho do JSON que a API devolve, escrito do ponto de vista do cliente (o front).
    // Propositalmente NÃO reaproveita os records de BusStation_API.DTO: se alguém
    // renomear um campo na API, o teste E2E tem que quebrar — é exatamente o que o
    // front sofreria.

    public record TokenResponse(string Token);

    public record TokenPairDto(string Token, string RefreshToken, int ExpiresIn);

    public record CityDto(int Id, string CityName, string State, string Acronym);

    public record DistanceDto(int Id, int OriginCityId, int DestinationCityId, int Kilometers);

    public record PriceDto(int Id, int DistanceId, float PricePerKm);

    public record RouteDto(int Id, string RouteName, int Kilometers);

    public record BoardingCreatedDto(int Id, int RouteId, int Seat, DateOnly BoardingDate, TimeOnly BoardingTime);

    public record SearchBoardingDto(
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

    public record TicketDto(
        int Id,
        int BoardingId,
        int RouteId,
        string RouteName,
        DateOnly BoardingDate,
        TimeOnly BoardingTime,
        float FarePaid,
        DateTime PurchasedOn);

    public record UserProfileDto(int Id, string Name, string Email, int Age);

    public record ErrorDto(string Message);
}
