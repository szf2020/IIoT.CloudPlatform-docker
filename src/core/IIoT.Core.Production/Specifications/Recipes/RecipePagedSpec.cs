using IIoT.Core.Production.Aggregates.Recipes;
using IIoT.SharedKernel.Specification;

namespace IIoT.Core.Production.Specifications.Recipes;

public class RecipePagedSpec : Specification<Recipe>
{
    public RecipePagedSpec(
        int skip,
        int take,
        List<Guid>? allowedDeviceIds = null,
        string? keyword = null,
        bool isPaging = true)
    {
        FilterCondition = r =>
            (allowedDeviceIds == null || allowedDeviceIds.Contains(r.DeviceId))
            && (string.IsNullOrEmpty(keyword) || r.RecipeName.Contains(keyword));

        SetOrderBy(r => r.RecipeName);

        if (isPaging)
        {
            SetPaging(skip, take);
        }
    }
}