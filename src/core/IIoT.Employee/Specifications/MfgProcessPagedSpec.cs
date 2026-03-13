using IIoT.Core.Employee.Aggregates.MfgProcesses;
using IIoT.SharedKernel.Specification;

namespace IIoT.Core.Employee.Specifications;

/// <summary>
/// 专用查询规约：查询工序分页列表 (支持按编码或名称模糊搜索)
/// </summary>
public class MfgProcessPagedSpec : Specification<MfgProcess>
{
    /// <param name="skip">跳过条数</param>
    /// <param name="take">获取条数</param>
    /// <param name="keyword">关键字 (匹配工序编码或名称)</param>
    /// <param name="isPaging">是否启用分页 (查总数时传 false)</param>
    public MfgProcessPagedSpec(int skip, int take, string? keyword = null, bool isPaging = true)
    {
        // 如果传了关键字，按编码或名称模糊匹配
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            FilterCondition = p => p.ProcessCode.Contains(keyword) || p.ProcessName.Contains(keyword);
        }

        // 必须有排序才能做 EF Core 分页
        SetOrderBy(p => p.ProcessCode);

        // 只有明确要分页时才装载分页参数
        if (isPaging)
        {
            SetPaging(skip, take);
        }
    }
}
