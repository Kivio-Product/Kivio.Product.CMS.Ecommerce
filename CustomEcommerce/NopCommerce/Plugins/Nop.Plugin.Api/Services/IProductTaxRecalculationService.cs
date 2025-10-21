using Nop.Core.Domain.Catalog;

namespace Nop.Plugin.Api.Services;

/// <summary>
/// Service for recalculating product prices with tax
/// </summary>
public interface IProductTaxRecalculationService
{
    /// <summary>
    /// Processes tax recalculation for product creation or update
    /// </summary>
    /// <param name="product">Product entity from request</param>
    /// <param name="isInsert">True if this is an insert operation, false for update</param>
    /// <returns>Task</returns>
    Task ProcessProductTaxRecalculationAsync(Product product, bool isInsert);
}
