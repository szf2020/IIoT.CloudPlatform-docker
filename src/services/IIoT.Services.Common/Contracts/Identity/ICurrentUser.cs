namespace IIoT.Services.Common.Contracts.Identity;

public interface ICurrentUser
{
    string? Id { get; }

    string? UserName { get; }

    string? Role { get; }

    bool IsAuthenticated { get; }
}
