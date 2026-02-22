using MediatR;
using ProfinetApi.Application.Services;

namespace ProfinetApi.Application.Features.Stations.Commands.ImportGsdml;

public record ImportGsdmlCommand(Guid StationId, Stream FileStream, string FileName) : IRequest;

public class ImportGsdmlCommandHandler : IRequestHandler<ImportGsdmlCommand>
{
    private readonly IConfigurationService _configService;

    public ImportGsdmlCommandHandler(IConfigurationService configService)
    {
        _configService = configService;
    }

    public async Task Handle(ImportGsdmlCommand request, CancellationToken ct)
    {
        await _configService.ImportGsdmlAsync(request.StationId, request.FileStream, request.FileName, ct);
    }
}
