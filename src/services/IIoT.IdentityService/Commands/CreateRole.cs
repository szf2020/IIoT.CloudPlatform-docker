using IIoT.Services.Common.Contracts;
using IIoT.SharedKernel.Messaging;
using IIoT.SharedKernel.Result;

namespace IIoT.IdentityService.Commands;

public record CreateRoleCommand(string RoleName) : ICommand<Result<string>>;

// 🌟 注入的是纯纯的接口！彻底告别 RoleManager
public class CreateRoleHandler(IIdentityService identityService)
    : ICommandHandler<CreateRoleCommand, Result<string>>
{
    public async Task<Result<string>> Handle(CreateRoleCommand request, CancellationToken cancellationToken)
    {
        var result = await identityService.CreateRoleAsync(request.RoleName);
        if (!result.IsSuccess)
            return Result.Failure(result.Errors?.ToArray() ?? ["创建失败"]);

        return Result.Success("角色创建成功");
    }
}