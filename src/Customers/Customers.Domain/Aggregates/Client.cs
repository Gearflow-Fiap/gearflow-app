using Customers.Domain.ValueObjects;
using Shared.Domain.Primitives;

namespace Customers.Domain.Aggregates;

/// <summary>
/// Cliente da oficina (PF por CPF ou PJ por CNPJ). Invariante preservada do GearFlow: ao menos um
/// entre CPF e CNPJ é obrigatório. Agora expressa via <see cref="Result{T}"/> em vez de exceção.
/// </summary>
public sealed class Client : AggregateRoot<ClientId>
{
    private readonly List<Vehicle> _vehicles = new();

    public Cpf? Cpf { get; private set; }
    public Cnpj? Cnpj { get; private set; }
    public string Name { get; private set; }
    public Email Email { get; private set; }
    public string Phone { get; private set; }
    public Address Address { get; private set; }
    public IReadOnlyCollection<Vehicle> Vehicles => _vehicles.AsReadOnly();

    private Client(ClientId id, Cpf? cpf, Cnpj? cnpj, string name, Email email, string phone, Address address)
        : base(id)
    {
        Cpf = cpf;
        Cnpj = cnpj;
        Name = name;
        Email = email;
        Phone = phone;
        Address = address;
    }

    private Client() : base(ClientId.New())
    {
        Name = null!;
        Email = null!;
        Phone = null!;
        Address = null!;
    }

    public static Result<Client> Create(
        string? cpf, string? cnpj, string name, string email, string phone, Address address)
    {
        if (string.IsNullOrWhiteSpace(cpf) && string.IsNullOrWhiteSpace(cnpj))
            return Result.Failure<Client>(
                Error.Validation("Client.DocumentRequired", "Informe CPF ou CNPJ."));

        if (string.IsNullOrWhiteSpace(name))
            return Result.Failure<Client>(Error.Validation("Client.NameRequired", "Nome é obrigatório."));

        Cpf? cpfVo = null;
        if (!string.IsNullOrWhiteSpace(cpf))
        {
            var cpfResult = ValueObjects.Cpf.Create(cpf);
            if (cpfResult.IsFailure) return Result.Failure<Client>(cpfResult.Error);
            cpfVo = cpfResult.Value;
        }

        Cnpj? cnpjVo = null;
        if (!string.IsNullOrWhiteSpace(cnpj))
        {
            var cnpjResult = ValueObjects.Cnpj.Create(cnpj);
            if (cnpjResult.IsFailure) return Result.Failure<Client>(cnpjResult.Error);
            cnpjVo = cnpjResult.Value;
        }

        var emailResult = Email.Create(email);
        if (emailResult.IsFailure) return Result.Failure<Client>(emailResult.Error);

        return Result.Success(new Client(ClientId.New(), cpfVo, cnpjVo, name.Trim(), emailResult.Value, phone, address));
    }

    public Result UpdateInformations(string name, string email, string phone, Address address)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Result.Failure(Error.Validation("Client.NameRequired", "Nome é obrigatório."));

        var emailResult = Email.Create(email);
        if (emailResult.IsFailure) return Result.Failure(emailResult.Error);

        Name = name.Trim();
        Email = emailResult.Value;
        Phone = phone;
        Address = address;
        return Result.Success();
    }

    public Vehicle AddVehicle(
        string licensePlate, string mark, string model, string color,
        int yearFabrication, int yearModel, Guid createdById, DateTime nowUtc)
    {
        var vehicle = new Vehicle(Id, licensePlate, mark, model, color, yearFabrication, yearModel, createdById, nowUtc);
        _vehicles.Add(vehicle);
        return vehicle;
    }

    public void RemoveVehicle(VehicleId vehicleId)
    {
        var vehicle = _vehicles.FirstOrDefault(v => v.Id == vehicleId);
        if (vehicle is not null) _vehicles.Remove(vehicle);
    }
}
