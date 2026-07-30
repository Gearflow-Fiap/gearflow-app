using Shared.Domain.Primitives;
using Workshop.Application.Abstractions;
using Workshop.Application.DTOs;

namespace Workshop.Application.UseCases.GetServiceOrders;

internal sealed class GetServiceOrdersHandler : IQueryHandler<GetServiceOrdersQuery, IReadOnlyList<ServiceOrderDto>>
{
    private readonly IServiceOrderRepository _repository;

    public GetServiceOrdersHandler(IServiceOrderRepository repository) => _repository = repository;

    public async Task<Result<IReadOnlyList<ServiceOrderDto>>> Handle(GetServiceOrdersQuery query, CancellationToken ct)
    {
        var orders = await _repository.ListAsync(ct);
        IReadOnlyList<ServiceOrderDto> dtos = orders.Select(ServiceOrderDto.FromAggregate).ToList();
        return Result.Success(dtos);
    }
}
