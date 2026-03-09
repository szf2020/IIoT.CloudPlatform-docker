using IIoT.Services.Common.Contracts;
using IIoT.SharedKernel.Messaging;
using IIoT.SharedKernel.Result;

namespace IIoT.IdentityService.Commands;

public record LoginUserCommand(string EmployeeNo, string Password) : ICommand<Result<string>>;

public class LoginUserHandler(
    IIdentityService identityService,
    IJwtTokenGenerator jwtTokenGenerator) // 你的 JWT 生成器也应该定义在 Common 接口里
    : ICommandHandler<LoginUserCommand, Result<string>>
{
    public async Task<Result<string>> Handle(LoginUserCommand request, CancellationToken cancellationToken)
    {
        // 1. 校验密码
        var checkResult = await identityService.CheckPasswordAsync(request.EmployeeNo, request.Password);
        if (!checkResult.IsSuccess || !checkResult.Value)
        {
            return Result.Failure("工号不存在或密码错误");
        }

        // 2. 拿到底层数据
        var userId = await identityService.GetUserIdByEmployeeNoAsync(request.EmployeeNo);
        var roles = await identityService.GetRolesAsync(request.EmployeeNo);

        // 3. 颁发令牌 (注意：你的 JwtTokenGenerator 接口参数需要改一下，接收 Guid 和工号，而不是 ApplicationUser)
        var token = jwtTokenGenerator.GenerateToken(userId!.Value, request.EmployeeNo, roles);

        return Result.Success(token);
    }
}