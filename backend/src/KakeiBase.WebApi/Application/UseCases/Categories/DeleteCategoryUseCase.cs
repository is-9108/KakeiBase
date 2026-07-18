using KakeiBase.WebApi.Application.Interfaces;

namespace KakeiBase.WebApi.Application.UseCases.Categories;

/// <summary>カテゴリを削除するユースケース</summary>
public class DeleteCategoryUseCase(ICategoryRepository categoryRepository)
{
    /// <param name="userId">リクエストユーザーのID</param>
    /// <param name="categoryId">削除するカテゴリのID</param>
    /// <param name="ct">キャンセルトークン</param>
    /// <returns>削除に成功した場合は true。未存在または別ユーザーのリソースの場合は false</returns>
    public async Task<bool> ExecuteAsync(Guid userId, Guid categoryId, CancellationToken ct = default)
    {
        var category = await categoryRepository.FindByIdAsync(categoryId, ct);
        if (category is null || category.UserId != userId)
            return false;

        await categoryRepository.DeleteAsync(category, ct);
        await categoryRepository.SaveChangesAsync(ct);

        return true;
    }
}
