using System.Text.Json;
using System.Text;
using Microsoft.Extensions.Configuration;
using Nop.Core.Caching;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Orders;
using Nop.Services.Logging;

namespace Nop.Services.Catalog;

public class ProductValidationService : IProductValidationService
{
    private readonly HttpClient _httpClient;
    private readonly IProductService _productService;
    private readonly ILogger _logger;
    private readonly IStaticCacheManager _cacheManager;
    private readonly string _validationApiUrl;

    public ProductValidationService(
        HttpClient httpClient,
        IProductService productService,
        ILogger logger,
        IStaticCacheManager cacheManager,
        IConfiguration configuration)
    {
        _httpClient = httpClient;
        _productService = productService;
        _logger = logger;
        _cacheManager = cacheManager;
        _validationApiUrl = configuration["ProductValidation:ApiUrl"] ?? throw new ArgumentNullException("ApiUrl not configured");
    }

    public async Task<ValidationJobResult> StartCartValidationAsync(IList<ShoppingCartItem> cartItems, int customerId)
    {
        try
        {
            // Limpiar caché anterior antes de crear nuevo snapshot
            await ClearProductSnapshotAsync(customerId);

            // Obtener productos únicos del carrito
            var productIds = cartItems.Select(x => x.ProductId).Distinct().ToList();
            var products = await _productService.GetProductsByIdsAsync(productIds.ToArray());
            var scrapedProducts = products.Where(p => p.Sku != null && p.Sku.ToUpper().Contains("SCRAPED")).ToList();

            // Crear snapshot y guardarlo en caché
            var snapshot = scrapedProducts.Select(p => new ProductSnapshot
            {
                ProductId = p.Id,
                ProductName = p.Name,
                Price = p.Price,
                Published = p.Published,
                SnapshotDate = DateTime.UtcNow,
                StockQuantity = p.StockQuantity
            }).ToList();

            // Guardar snapshot en caché
            var snapshotCacheKey = _cacheManager.PrepareKey(SnapshotProductsCacheDefaults.SnapshotProductsModelKey, customerId);
            await _cacheManager.SetAsync(snapshotCacheKey, snapshot);

            // Preparar request para API externa
            var validationRequest = new
            {
                products = scrapedProducts.Select(p => new
                {
                    name = p.Name,
                    externalId = p.Id.ToString()
                }).ToList(),
                triggerSource = "ECOMMERCE"
            };

            var json = JsonSerializer.Serialize(validationRequest);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync($"{_validationApiUrl}/api/product-validation/validate", content);

            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<ValidationJobResult>(responseContent, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                // Guardar información del job en caché para seguimiento
                if (result.Success && result.Data != null)
                {
                    var jobCacheKey = _cacheManager.PrepareKey(JobCacheDefaults.JobModelKey, customerId);
                    await _cacheManager.SetAsync(jobCacheKey, result.Data);
                }

                _logger.Information($"Validación iniciada para cliente {customerId}. JobId: {result.Data?.JobId}");
                return result;
            }
            else
            {
                _logger.Error($"Error al iniciar validación para cliente {customerId}: {response.StatusCode}");
                return new ValidationJobResult { Success = false, Message = "Error al comunicarse con el servicio de validación" };
            }
        }
        catch (Exception ex)
        {
            _logger.Error($"Error al iniciar validación para cliente {customerId}", ex);
            return new ValidationJobResult { Success = false, Message = "Error interno del servidor" };
        }
    }

    public async Task<ProductChangeResult> CheckProductChangesAsync(int customerId)
    {
        try
        {
            // Obtener snapshot del caché
            var snapshotCacheKey = _cacheManager.PrepareKey(SnapshotProductsCacheDefaults.SnapshotProductsModelKey, customerId);
            var snapshots = await _cacheManager.GetAsync<List<ProductSnapshot>>(snapshotCacheKey);

            if (snapshots == null || !snapshots.Any())
            {
                _logger.Warning($"No se encontró snapshot en caché para cliente {customerId}");
                return new ProductChangeResult { HasChanges = false, Changes = new List<ProductChange>(), CheckedAt = DateTime.UtcNow, IsJobCompleted = true };
            }

            // Verificar si el snapshot es muy antiguo (más de 30 minutos)
            var oldestSnapshot = snapshots.Min(s => s.SnapshotDate);
            if (DateTime.UtcNow.Subtract(oldestSnapshot).TotalMinutes > 30)
            {
                _logger.Information($"Snapshot muy antiguo para cliente {customerId}, limpiando caché");
                await ClearProductSnapshotAsync(customerId);
                return new ProductChangeResult { HasChanges = false, Changes = new List<ProductChange>(), CheckedAt = DateTime.UtcNow, IsJobCompleted = true };
            }

            // Primero verificar estado del job
            var jobCacheKey = _cacheManager.PrepareKey(JobCacheDefaults.JobModelKey, customerId);
            var jobData = await _cacheManager.GetAsync<ValidationJobData>(jobCacheKey);
            ValidationJobStatusResult? jobStatus = null;
            bool isJobCompleted = true; // Por defecto true si no hay job

            if (jobData?.JobId > 0)
            {
                jobStatus = await GetJobStatusAsync(jobData.JobId);
                isJobCompleted = jobStatus?.Data?.IsCompleted ?? false;
            }

            // Solo calcular cambios si el job está completo
            var changes = new List<ProductChange>();
            bool hasChanges = false;

            if (isJobCompleted)
            {
                // Obtener productos actuales de la base de datos
                var productIds = snapshots.Select(s => s.ProductId).ToArray();
                var currentProducts = await _productService.GetProductsByIdsAsync(productIds);

                foreach (var snapshot in snapshots)
                {
                    var currentProduct = currentProducts.FirstOrDefault(p => p.Id == snapshot.ProductId);

                    // Producto eliminado o despublicado
                    if (currentProduct == null || !currentProduct.Published || currentProduct.StockQuantity == 0)
                    {
                        changes.Add(new ProductChange
                        {
                            ProductId = snapshot.ProductId,
                            ProductName = snapshot.ProductName,
                            ChangeType = currentProduct == null ? "deleted" : "unpublished",
                            OldPrice = snapshot.Price,
                            NewPrice = currentProduct?.Price ?? 0,
                            Message = currentProduct == null ? "Producto eliminado del catálogo" : "Producto ya no está disponible"
                        });
                        continue;
                    }

                    // Verificar cambio de precio (tolerancia de 1 centavo)
                    if (Math.Abs(snapshot.Price - currentProduct.Price) > 0.01m)
                    {
                        changes.Add(new ProductChange
                        {
                            ProductId = snapshot.ProductId,
                            ProductName = currentProduct.Name,
                            ChangeType = "price_changed",
                            OldPrice = snapshot.Price,
                            NewPrice = currentProduct.Price,
                            Message = $"El precio cambió de ${snapshot.Price:F2} a ${currentProduct.Price:F2}"
                        });
                    }
                }

                hasChanges = changes.Any();
                _logger.Information($"Job completado - Verificación final para cliente {customerId}. Cambios encontrados: {changes.Count}");
            }
            else
            {
                _logger.Information($"Job aún en progreso para cliente {customerId}. No calculando cambios aún.");
            }

            return new ProductChangeResult
            {
                HasChanges = hasChanges,
                Changes = changes,
                CheckedAt = DateTime.UtcNow,
                IsJobCompleted = isJobCompleted
            };
        }
        catch (Exception ex)
        {
            _logger.Error($"Error verificando cambios de productos para cliente {customerId}", ex);
            return new ProductChangeResult { HasChanges = false, Changes = new List<ProductChange>(), IsJobCompleted = true };
        }
    }

    public async Task ClearProductSnapshotAsync(int customerId)
    {
        try
        {
            var snapshotCacheKey = _cacheManager.PrepareKey(SnapshotProductsCacheDefaults.SnapshotProductsModelKey, customerId);
            var jobCacheKey = _cacheManager.PrepareKey(JobCacheDefaults.JobModelKey, customerId);

            await _cacheManager.RemoveAsync(snapshotCacheKey);
            await _cacheManager.RemoveAsync(jobCacheKey);

            _logger.Information($"Cache limpiado para cliente {customerId}");
        }
        catch (Exception ex)
        {
            _logger.Error($"Error limpiando cache para cliente {customerId}", ex);
        }
    }

    public async Task<ValidationJobStatusResult> GetJobStatusAsync(int jobId)
    {
        try
        {
            if (jobId <= 0)
            {
                return new ValidationJobStatusResult { Success = false, Message = "JobId requerido" };
            }

            var response = await _httpClient.GetAsync($"{_validationApiUrl}/api/product-validation/job-status/{jobId}");

            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<ValidationJobStatusResult>(responseContent, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                return result;
            }
            else
            {
                _logger.Error($"Error obteniendo estado del job {jobId}: {response.StatusCode}");
                return new ValidationJobStatusResult { Success = false, Message = "Error consultando estado del trabajo" };
            }
        }
        catch (Exception ex)
        {
            _logger.Error($"Error obteniendo estado del job {jobId}", ex);
            return new ValidationJobStatusResult { Success = false, Message = "Error interno del servidor" };
        }
    }

    public async Task<CheckoutValidation> ValidateForCheckoutAsync(int customerId)
    {
        try
        {
            var jobCacheKey = _cacheManager.PrepareKey(JobCacheDefaults.JobModelKey, customerId);
            var jobData = await _cacheManager.GetAsync<ValidationJobData>(jobCacheKey);

            if (jobData?.JobId > 0)
            {
                var jobStatus = await GetJobStatusAsync(jobData.JobId);

                // Si el job no está completo, no calcular cambios aún
                if (!jobStatus.Data?.IsCompleted == true)
                {
                    return new CheckoutValidation
                    {
                        CanProceed = false,
                        Message = "Validación en progreso...",
                        ShouldRedirectHome = false,
                        HasProductChanges = false,
                        ProductChanges = new List<ProductChange>(),
                        IsJobCompleted = false
                    };
                }

                // Job completo: obtener cambios finales
                var currentProductsStatus = await CheckProductChangesAsync(customerId);

                return new CheckoutValidation
                {
                    CanProceed = jobStatus.Success && jobStatus.Data.IsCompleted,
                    Message = jobStatus.Message,
                    ShouldRedirectHome = !jobStatus.Success || !jobStatus.Data.IsCompleted,
                    HasProductChanges = currentProductsStatus.HasChanges,
                    ProductChanges = currentProductsStatus.Changes,
                    IsJobCompleted = true
                };
            }
            else
            {
                // Si no hay job, verificar cambios directamente (job ya completado previamente o no iniciado)
                var currentProductsStatus = await CheckProductChangesAsync(customerId);
                
                return new CheckoutValidation
                {
                    CanProceed = true,
                    Message = "Validación completada",
                    ShouldRedirectHome = false,
                    HasProductChanges = currentProductsStatus.HasChanges,
                    ProductChanges = currentProductsStatus.Changes,
                    IsJobCompleted = true
                };
            }
        }
        catch (Exception ex)
        {
            _logger.Error($"Error en la validación de carrito para checkout para cliente {customerId}", ex);
            return new CheckoutValidation
            {
                CanProceed = false,
                Message = "Error interno del servidor",
                ShouldRedirectHome = true,
                HasProductChanges = false,
                ProductChanges = new List<ProductChange>(),
                IsJobCompleted = true
            };
        }
    }

    private static class SnapshotProductsCacheDefaults
    {
        public static CacheKey SnapshotProductsModelKey => new("nop.productvalidation.snapshot.{0}", SnapshotProductsPrefix);

        public static string SnapshotProductsPrefix = "nop.productvalidation.snapshot";

        public static int CacheTime { get; set; } = 30; // Aumentado a 30 minutos
    }

    private static class JobCacheDefaults
    {
        public static CacheKey JobModelKey => new("nop.productvalidation.job.{0}", JobPrefix);

        public static string JobPrefix = "nop.productvalidation.job";

        public static int CacheTime { get; set; } = 30; // Aumentado a 30 minutos
    }
}