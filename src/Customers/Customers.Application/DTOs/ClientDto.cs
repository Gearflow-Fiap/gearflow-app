using Customers.Domain.Aggregates;

namespace Customers.Application.DTOs;

public sealed record AddressDto(string Street, string City, string State, string Country, string ZipCode);

public sealed record VehicleDto(
    Guid Id, string LicensePlate, string Mark, string Model, string Color, int YearFabrication, int YearModel)
{
    public static VehicleDto FromAggregate(Vehicle v) =>
        new(v.Id.Value, v.LicensePlate, v.Mark, v.Model, v.Color, v.YearFabrication, v.YearModel);
}

public sealed record ClientDto(
    Guid Id,
    string? Cpf,
    string? Cnpj,
    string Name,
    string Email,
    string Phone,
    AddressDto Address,
    IReadOnlyList<VehicleDto> Vehicles)
{
    public static ClientDto FromAggregate(Client c) =>
        new(
            c.Id.Value,
            c.Cpf?.Value,
            c.Cnpj?.Value,
            c.Name,
            c.Email.Value,
            c.Phone,
            new AddressDto(c.Address.Street, c.Address.City, c.Address.State, c.Address.Country, c.Address.ZipCode),
            c.Vehicles.Select(VehicleDto.FromAggregate).ToList());
}
