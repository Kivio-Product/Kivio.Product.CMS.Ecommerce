using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Nop.Services.Catalog;

/// <summary>
/// Servicio centralizado para tokenización y normalización de texto de productos
/// </summary>
public interface IProductTokenizationService
{
    /// <summary>
    /// Tokeniza un texto de producto en tokens únicos normalizados
    /// </summary>
    public HashSet<string> Tokenize(string text);

    /// <summary>
    /// Normaliza un texto (lowercase, sin diacríticos, espacios normalizados)
    /// </summary>
    public string Normalize(string text);

    /// <summary>
    /// Verifica si un token debe ser excluido (stop words, etc.)
    /// </summary>
    public bool ShouldSkipToken(string token);

    /// <summary>
    /// Procesa y normaliza un token individual
    /// </summary>
    public string ProcessToken(string token);
}