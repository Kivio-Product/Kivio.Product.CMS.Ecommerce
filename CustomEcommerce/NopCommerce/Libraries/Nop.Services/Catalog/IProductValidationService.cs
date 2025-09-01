using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Orders;

namespace Nop.Services.Catalog;

public interface IProductValidationService
{
    /// <summary>
    /// Inicia la validación en segundo plano y guarda snapshot en caché
    /// </summary>
    public Task<ValidationJobResult> StartCartValidationAsync(IList<ShoppingCartItem> cartItems, int customerId);

    /// <summary>
    /// Verifica si hay cambios comparando caché vs base de datos
    /// </summary>
    public Task<ProductChangeResult> CheckProductChangesAsync(int customerId);

    /// <summary>
    /// Limpia el snapshot del caché
    /// </summary>
    public Task ClearProductSnapshotAsync(int customerId);

    /// <summary>
    /// Obtiene el estado del job de validación
    /// </summary>
    public Task<ValidationJobStatusResult> GetJobStatusAsync(int jobId);

    public Task<CheckoutValidation> ValidateForCheckoutAsync(int customerId);
}
