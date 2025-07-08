using Nop.Services.ScheduleTasks;
using Nop.Core;
using Nop.Core.Domain.Catalog;

namespace Nop.Services.Catalog;

/// <summary>
/// Unpublish end date product scheduled task implementation
/// </summary>
public partial class UnpublishEndDateProductTask : IScheduleTask
{
    #region Fields
    protected readonly IProductService _productService;
    #endregion

    #region Ctor

    public UnpublishEndDateProductTask(IProductService productService)
    {
        _productService = productService;
    }

    #endregion

    #region Methods

    /// <summary>
    /// Executes a task
    /// </summary>
    public async System.Threading.Tasks.Task ExecuteAsync()
    {
        int pageIndex = 0;
        int pageSize = 500;
        IPagedList<Product> products;

        do
        {
            products = await _productService.SearchProductsAsync(
                pageIndex: pageIndex,
                pageSize: pageSize,
                overridePublished: true,
                showHidden: true
            );

            foreach (var product in products)
            {
                if (product.Published && product.AvailableEndDateTimeUtc.HasValue && product.AvailableEndDateTimeUtc.Value <= DateTime.UtcNow)
                {
                    product.Published = false;
                    await _productService.UpdateProductAsync(product);
                }
            }

            pageIndex++;
        }
        while (products.Count == pageSize);
    }

    #endregion
}