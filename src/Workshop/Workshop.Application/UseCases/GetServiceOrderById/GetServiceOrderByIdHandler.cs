using Shared.Domain.Primitives;
using Workshop.Application.Abstractions;
using Workshop.Application.DTOs;
using Workshop.Domain.ValueObjects;

namespace Workshop.Application.UseCases.GetServiceOrderById;

internal sealed class GetServiceOrderByIdHandler : IQueryHandler<GetServiceOrderByIdQuery, ServiceOrderDto>
{
    private readonly IServiceOrderRepository _repository;

    public GetServiceOrderByIdHandler(IServiceOrderRepository repository) => _repository = repository;

    public async Task<Result<ServiceOrderDto>> Handle(GetServiceOrderByIdQuery query, CancellationToken ct)
    {
        var order = await _repository.GetByIdAsync(ServiceOrderId.From(query.ServiceOrderId), ct);
        if (order is null)
            return Result.Failure<ServiceOrderDto>(Error.NotFound("ServiceOrder.NotFound", "Ordem de serviço não encontrada."));

        return Result.Success(ServiceOrderDto.FromAggregate(order));
    }
}
