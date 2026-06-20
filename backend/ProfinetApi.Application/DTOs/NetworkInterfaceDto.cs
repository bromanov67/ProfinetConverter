public record NetworkInterfaceDto(
        Guid Id,
        string Name,
        string Type,
        bool Active,
        string Description,
        List<StationDto> Stations
    );