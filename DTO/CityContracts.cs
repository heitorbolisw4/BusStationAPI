namespace BusStation_API.DTO
{
    public record CreateCityRequest(string CityName, string State, string Acronym);
    public record CityResponse(int Id, string CityName, string State, string Acronym);
    public record UpdateCityRequest(string CityName, string State, string Acronym);

}