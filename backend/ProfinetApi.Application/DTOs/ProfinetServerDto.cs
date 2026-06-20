public record ProfinetServerDto(
        Guid Id,
        string Name,
        bool Active,
        string Description,
        List<NetworkInterfaceDto> Interfaces
    ) : ServerDtoBase(Id, Name, Active, Description);