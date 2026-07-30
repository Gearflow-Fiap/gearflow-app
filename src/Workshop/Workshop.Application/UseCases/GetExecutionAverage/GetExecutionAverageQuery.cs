using Workshop.Application.Abstractions;
using Workshop.Application.DTOs;

namespace Workshop.Application.UseCases.GetExecutionAverage;

public sealed record GetExecutionAverageQuery : IQuery<MonitoringAverageDto>;
