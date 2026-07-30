using Shared.Domain.Primitives;

namespace Customers.Domain.ValueObjects;

public sealed class Address : ValueObject
{
    public string Street { get; }
    public string City { get; }
    public string State { get; }
    public string Country { get; }
    public string ZipCode { get; }

    public Address(string street, string city, string state, string country, string zipCode)
    {
        Street = street ?? string.Empty;
        City = city ?? string.Empty;
        State = state ?? string.Empty;
        Country = country ?? string.Empty;
        ZipCode = zipCode ?? string.Empty;
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Street;
        yield return City;
        yield return State;
        yield return Country;
        yield return ZipCode;
    }
}
