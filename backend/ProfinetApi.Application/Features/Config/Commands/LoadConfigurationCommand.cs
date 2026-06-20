using MediatR;
using ProfinetApi.Domain.Entities;

namespace ProfinetApi.Application.Features.Config.Commands.LoadConfiguration;

public record LoadConfigurationCommand(string FilePath) : IRequest<List<Project>>;