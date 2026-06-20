using MediatR;

namespace ProfinetApi.Application.Features.Config.Commands.SaveConfiguration;

public record SaveConfigurationCommand(string FilePath) : IRequest;