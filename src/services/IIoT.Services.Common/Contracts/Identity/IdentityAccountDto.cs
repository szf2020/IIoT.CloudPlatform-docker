namespace IIoT.Services.Common.Contracts.Identity;

public record IdentityAccountDto(
    Guid Id,
    string UserName,
    bool IsEnabled,
    IList<string> Roles);
