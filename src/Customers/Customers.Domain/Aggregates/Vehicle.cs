using Customers.Domain.ValueObjects;
using Shared.Domain.Primitives;

namespace Customers.Domain.Aggregates;

/// <summary>Veículo de um <see cref="Client"/>. Preserva os campos e o UpdateDetails do GearFlow.</summary>
public sealed class Vehicle : Entity<VehicleId>
{
    public ClientId ClientId { get; private set; }
    public string LicensePlate { get; private set; }
    public string Mark { get; private set; }
    public string Model { get; private set; }
    public string Color { get; private set; }
    public int YearFabrication { get; private set; }
    public int YearModel { get; private set; }
    public DateTime CreatedOn { get; private set; }
    public Guid CreatedById { get; private set; }

    internal Vehicle(
        ClientId clientId, string licensePlate, string mark, string model, string color,
        int yearFabrication, int yearModel, Guid createdById, DateTime createdOn)
        : base(VehicleId.New())
    {
        ClientId = clientId;
        LicensePlate = licensePlate;
        Mark = mark;
        Model = model;
        Color = color;
        YearFabrication = yearFabrication;
        YearModel = yearModel;
        CreatedById = createdById;
        CreatedOn = createdOn;
    }

    private Vehicle() : base(VehicleId.New())
    {
        ClientId = null!;
        LicensePlate = null!;
        Mark = null!;
        Model = null!;
        Color = null!;
    }

    public void UpdateDetails(
        string licensePlate, string mark, string model, string color, int yearFabrication, int yearModel)
    {
        LicensePlate = licensePlate;
        Mark = mark;
        Model = model;
        Color = color;
        YearFabrication = yearFabrication;
        YearModel = yearModel;
    }
}
