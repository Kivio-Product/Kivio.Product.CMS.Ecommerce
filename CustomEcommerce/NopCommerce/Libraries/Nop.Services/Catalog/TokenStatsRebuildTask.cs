using Nop.Services.ScheduleTasks;
using System.Data;
using LinqToDB.Data;
using Nop.Data;
using Nop.Services.Logging;
using LinqToDB;

namespace Nop.Services.Catalog;

/// <summary>
/// Reconstruye estadísticas de tokens (DF/IDF) para búsquedas y deduplicación
/// </summary>
public class TokenStatsRebuildTask(
    ILogger<TokenStatsRebuildTask> logger,
    INopDataProvider dataProvider,
    IProductTokenizationService tokenizationService) : IScheduleTask
{
    private readonly ILogger<TokenStatsRebuildTask> _logger = logger;
    private readonly INopDataProvider _data = dataProvider;
    private readonly IProductTokenizationService _tokenizer = tokenizationService;

    private sealed record ProductRow(int Id, string Name);

    public async Task ExecuteAsync()
    {
        var start = DateTime.UtcNow;
        _logger.Information($"[TokenStatsRebuildTask] Inicio {start}");

        try
        {
            await RefreshAsync();
            var end = DateTime.UtcNow;
            _logger.Information($"[TokenStatsRebuildTask] OK en {(end - start).TotalSeconds}s");
        }
        catch (Exception ex)
        {
            _logger.Error("[TokenStatsRebuildTask] Error reconstruyendo TokenStats", ex);
            throw;
        }
    }

    public async Task RefreshAsync()
    {
        var products = await _data.QueryAsync<ProductRow>(
            @"SELECT Id, Name
              FROM dbo.Product WITH (NOLOCK)
              WHERE Published = 1 AND Deleted = 0");

        _logger.Information($"[TokenStatsRebuildTask] Procesando {products.Count()} productos");

        var df = new Dictionary<string, HashSet<int>>(StringComparer.OrdinalIgnoreCase);

        foreach (var (id, rawName) in products)
        {
            var tokens = _tokenizer.Tokenize(rawName ?? string.Empty);

            foreach (var token in tokens)
            {
                if (!df.TryGetValue(token, out var set))
                    df[token] = set = new HashSet<int>();
                set.Add(id);
            }
        }

        var tvp = new DataTable();
        tvp.Columns.Add("Token", typeof(string));
        tvp.Columns.Add("DocFreq", typeof(int));

        foreach (var (token, productIds) in df)
        {
            tvp.Rows.Add(token, productIds.Count);
        }

        var total = products.Count();
        _logger.Information($"[TokenStatsRebuildTask] Enviando {df.Count} tokens al SP para cálculo de IDF");

        var parameters = new[]
        {
            new DataParameter("@Stats", tvp) { DataType = DataType.Structured, DbType = "dbo.TokenStatUpsertType" },
            new DataParameter("@N", total)
        };

        await _data.QueryProcAsync<int>("dbo.UpsertTokenStats", parameters);

        _logger.Information($"[TokenStatsRebuildTask] Estadísticas de tokens actualizadas correctamente");
    }
}