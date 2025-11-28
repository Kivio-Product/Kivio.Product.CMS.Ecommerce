using Nop.Services.ScheduleTasks;
using System.Data;
using LinqToDB.Data;
using Nop.Data;
using Nop.Services.Logging;
using LinqToDB;

namespace Nop.Services.Catalog;

/// <summary>
/// Tarea programada para limpiar imágenes de productos inactivos o sin stock
/// </summary>
public class CleanupInactiveProductImagesTask(
    ILogger logger,
    INopDataProvider dataProvider) : IScheduleTask
{
    private readonly ILogger _logger = logger;
    private readonly INopDataProvider _data = dataProvider;

    private sealed record CleanupResult(
        int PicturesRemoved,
        long BytesFreed,
        decimal MBFreed,
        DateTime? StartedAt,
        DateTime? FinishedAt,
        string Message,
        string ErrorMessage);

    public async Task ExecuteAsync()
    {
        var start = DateTime.UtcNow;
        _logger.Information($"[CleanupInactiveProductImagesTask] Inicio {start}");

        try
        {
            await CleanupAsync();
            var end = DateTime.UtcNow;
            _logger.Information($"[CleanupInactiveProductImagesTask] Completado en {(end - start).TotalSeconds:F2}s");
        }
        catch (Exception ex)
        {
            _logger.Error("[CleanupInactiveProductImagesTask] Error ejecutando limpieza de imágenes", ex);
            throw;
        }
    }

    public async Task CleanupAsync(string excludedProductIds = null)
    {
        _logger.Information($"[CleanupInactiveProductImagesTask] Ejecutando stored procedure de limpieza");

        var parameters = new[]
        {
            new DataParameter("@ExcludedProductIds", excludedProductIds ?? (object)DBNull.Value)
        };

        var results = await _data.QueryProcAsync<CleanupResult>("dbo.CleanupInactiveProductImages", parameters);
        var result = results.FirstOrDefault();

        if (result == null)
        {
            _logger.Warning("[CleanupInactiveProductImagesTask] No se recibió resultado del stored procedure");
            return;
        }

        if (!string.IsNullOrEmpty(result.ErrorMessage))
        {
            _logger.Error($"[CleanupInactiveProductImagesTask] Error en SP: {result.ErrorMessage}");
            return;
        }

        _logger.Information(
            $"[CleanupInactiveProductImagesTask] Resultado: " +
            $"{result.PicturesRemoved} imágenes eliminadas, " +
            $"{result.MBFreed:F2} MB liberados. " +
            $"Mensaje: {result.Message}");
    }
}
