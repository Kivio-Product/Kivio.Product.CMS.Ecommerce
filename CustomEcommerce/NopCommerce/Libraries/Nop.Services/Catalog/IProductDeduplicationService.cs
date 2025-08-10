using Nop.Core.Domain.Catalog;

namespace Nop.Services.Catalog;

public interface IProductDeduplicationService
{
    public Task<DeduplicationResult> DeduplicateCategoryAsync(int categoryId, DeduplicationOptions options = null);
}