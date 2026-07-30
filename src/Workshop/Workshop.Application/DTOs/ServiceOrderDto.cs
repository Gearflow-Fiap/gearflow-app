using Workshop.Domain.Aggregates.ServiceOrderModel;

namespace Workshop.Application.DTOs;

public sealed record ServiceOrderHistoryDto(string Status, string Message, DateTime CreatedOn, string ChangedByType)
{
    public static ServiceOrderHistoryDto FromEntity(ServiceOrderHistory h) =>
        new(h.Status.ToString(), h.Message, h.CreatedOn, h.ChangedBy.Type.ToString());
}

public sealed record ServiceOrderDto(
    Guid Id,
    int OSCode,
    Guid VehicleId,
    string Status,
    bool IsActive,
    DateTime CreatedOn,
    IReadOnlyList<Guid> RequestedJobIds,
    IReadOnlyList<ServiceOrderHistoryDto> History)
{
    public static ServiceOrderDto FromAggregate(ServiceOrder o) =>
        new(
            o.Id.Value,
            o.OSCode,
            o.VehicleId,
            o.Status.ToString(),
            o.IsActive,
            o.CreatedOn,
            o.RequestedJobs.Select(j => j.JobId).ToList(),
            o.Histories.OrderBy(h => h.CreatedOn).Select(ServiceOrderHistoryDto.FromEntity).ToList());
}
