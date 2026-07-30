using Shared.Domain.Primitives;
using Workshop.Application.Abstractions;
using Workshop.Application.DTOs;

namespace Workshop.Application.UseCases.GetServiceOrders;

internal sealed class GetServiceOrdersHandler : IQueryHandler<GetServiceOrdersQuery, PagedResult<ServiceOrderDto>>
{
    private readonly IServiceOrderRepository _repository;

    public GetServiceOrdersHandler(IServiceOrderRepository repository) => _repository = repository;

    public async Task<Result<PagedResult<ServiceOrderDto>>> Handle(GetServiceOrdersQuery query, CancellationToken ct)
    {
        var page = query.Page < 1 ? 1 : query.Page;
        var pageSize = Math.Clamp(query.PageSize, 1, 100);

        var (items, total) = await _repository.GetPagedByPriorityAsync(page, pageSize, ct);
        IReadOnlyList<ServiceOrderDto> dtos = items.Select(ServiceOrderDto.FromAggregate).ToList();

        return Result.Success(new PagedResult<ServiceOrderDto>(dtos, page, pageSize, total));
    }
}
