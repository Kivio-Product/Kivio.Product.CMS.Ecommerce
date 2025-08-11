using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Nop.Services.Catalog;

/// <summary>
/// Servicio centralizado para tokenización y normalización de texto de productos.
/// Proporciona funcionalidades para convertir texto de productos en tokens útiles para búsquedas,
/// manteniendo la semántica y relevancia de términos técnicos, medidas, códigos y modelos.
/// </summary>
public partial class ProductTokenizationService : IProductTokenizationService
{
    #region Configuración de Stop Words y Sinónimos

    /// <summary>
    /// Palabras muy comunes que no aportan valor semántico en búsquedas de productos.
    /// Estas palabras tendrán un IDF (Inverse Document Frequency) bajo y serán filtradas.
    /// </summary>
    private static readonly HashSet<string> _stopWords =
        new(StringComparer.OrdinalIgnoreCase)
        { 
            // Artículos, preposiciones y conectores comunes en español
            "de","la","el","y","con","para","en","un","una","los","las","del","al","por","sin",
            // Términos genéricos de productos que no aportan especificidad
            "producto", "articulo", "item", "pieza", "unidad", "pack", "set", "generico", "standard", "normal", "regular"
        };

    /// <summary>
    /// Unidades de medida genéricas que pueden aparecer en descripciones de productos.
    /// Estas unidades se tokenizan y normalizan para mejorar la búsqueda.
    /// </summary>
    private static readonly string[] _genericUnits =
        [
            "ml", "l", "g", "kg", "cm", "mm", "in", "oz", "fl oz", "lb", "ft", "yd"
        ];


    /// <summary>
    /// Descriptores de modelo y características de productos.
    /// Estos términos se utilizan para identificar atributos específicos de los productos.
    /// </summary>
    private static readonly string[] _modelDescriptors =
        [
            "tono", "tone", "color", "modelo", "model", "size", "talla", "numero", "no", "ref", "#"
        ];

    /// <summary>
    /// Diccionario de sinónimos y normalizaciones para unificar términos equivalentes.
    /// </summary>
    private static readonly Dictionary<string, string> _synonyms =
        new(StringComparer.OrdinalIgnoreCase)
        {
            // Unidades de medida normalizadas
            ["pulgadas"] = "pulgada",
            ["inches"] = "pulgada",
            ["inch"] = "pulgada",
            ["lts"] = "litros",
            ["l"] = "litros",
            ["kg"] = "kilogramo",
            ["gr"] = "gramo",
            ["g"] = "gramo",
            ["cm"] = "centimetro",
            ["mm"] = "milimetro",

            // Normalización de abreviaciones con puntos
            ["oz."] = "oz",
            ["ml."] = "ml"
        };

    #endregion

    #region Expresiones Regulares Compiladas

    /// <summary>
    /// Regex principal para tokenización completa. Captura palabras, códigos, medidas y patrones especiales.
    /// Se compila una sola vez para optimizar el rendimiento.
    /// </summary>
    private static readonly Regex _mainTokenRegex = MainTokenRegex();

    /// <summary>
    /// Regex específica para capturar medidas físicas (número + unidad) y descriptores de modelo/tono.
    /// Ejemplos: "500ml", "tono 175", "modelo ABC123"
    /// </summary>
    private static readonly Regex _measureRegex = MeasureRegex();

    /// <summary>
    /// Regex para capturar patrones de productos multifuncionales como "5en1", "3in1".
    /// Útil para productos como "champú 5en1" o "herramienta 3in1".
    /// </summary>
    private static readonly Regex _multiInOneRegex = MultiInOneRegex();

    /// <summary>
    /// Regex para normalizar múltiples espacios consecutivos en uno solo.
    /// Parte del proceso de limpieza y normalización de texto.
    /// </summary>
    private static readonly Regex _normalizerRegex = NormalizerRegex();

    #endregion

    #region Métodos Públicos de Normalización

    /// <summary>
    /// Normaliza un texto eliminando acentos, convirtiendo a minúsculas y limpiando espacios.
    /// Es el primer paso en el proceso de tokenización.
    /// </summary>
    /// <param name="text">Texto a normalizar</param>
    /// <returns>Texto normalizado listo para tokenización</returns>
    /// <example>
    /// Input: "ACEITE DE COCO   Orgánico"
    /// Output: "aceite de coco organico"
    /// </example>
    public string Normalize(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        var normalized = text.ToLowerInvariant().Trim();
        normalized = RemoveDiacritics(normalized); // Elimina acentos y caracteres especiales
        normalized = _normalizerRegex.Replace(normalized, " "); // Normaliza espacios múltiples
        return normalized;
    }

    /// <summary>
    /// Convierte un texto en un conjunto de tokens únicos y relevantes para búsquedas.
    /// Utiliza una estrategia de múltiples pasadas con diferentes prioridades:
    /// 1. Medidas y especificaciones técnicas (alta prioridad)
    /// 2. Patrones multifuncionales (ej: "5en1")
    /// 3. Códigos alfanuméricos únicos  
    /// 4. Palabras generales (baja prioridad)
    /// </summary>
    /// <param name="text">Texto del producto a tokenizar</param>
    /// <returns>HashSet de tokens únicos, sin duplicados y filtrados</returns>
    /// <example>
    /// Input: "Champú Pantene 500ml Tono 175 5en1"
    /// Output: {"champu", "pantene", "500ml", "500", "ml", "tono175", "175", "5en1", "5"}
    /// </example>
    public HashSet<string> Tokenize(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return new HashSet<string>();

        var normalized = Normalize(text);
        var tokens = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // ESTRATEGIA DE MÚLTIPLES PASADAS CON PRIORIDADES:

        // 1. PRIORIDAD ALTA: Extraer medidas completas primero (número + unidad)
        //    Ejemplo: "500ml" -> ["500ml", "500", "ml"]
        ExtractMeasurementTokens(normalized, tokens);

        // 2. PRIORIDAD ALTA: Extraer patrones "XenY" (ej: "5en1")
        //    Ejemplo: "5en1" -> ["5en1", "5"]
        ExtractMultiInOneTokens(normalized, tokens);

        // 3. PRIORIDAD MEDIA: Extraer códigos alfanuméricos únicos
        //    Ejemplo: "ABC123XYZ" -> ["abc123xyz"]
        ExtractUniqueCodeTokens(normalized, tokens);

        // 4. PRIORIDAD BAJA: Extraer palabras generales (sin overlap con medidas)
        //    Ejemplo: "champú" -> ["champu"]
        ExtractGeneralTokens(normalized, tokens);

        return tokens;
    }

    /// <summary>
    /// Determina si un token debe ser omitido del índice de búsqueda.
    /// Filtra stop words, tokens muy cortos y números puros poco útiles.
    /// </summary>
    /// <param name="token">Token a evaluar</param>
    /// <returns>true si el token debe omitirse, false si es útil</returns>
    public bool ShouldSkipToken(string token)
    {
        if (string.IsNullOrWhiteSpace(token) || token.Length < 2)
            return true;

        // Filtrar stop words definidas
        if (_stopWords.Contains(token))
            return true;

        // Solo números puros (ej: "123") - usualmente no son útiles solos
        // EXCEPCIÓN: Permitir números que podrían ser tonos, modelos, tallas, etc.
        if (Regex.IsMatch(token, @"^\d+$"))
        {
            // Permitir números cortos que podrían ser tonos, modelos, tallas (ej: 7, 175, 300)
            if (token.Length <= 4)
                return false;
            return true; // Números largos solos no suelen ser útiles
        }

        return false;
    }

    /// <summary>
    /// Procesa un token individual aplicando normalizaciones y sinónimos.
    /// Convierte términos equivalentes a una forma canónica.
    /// </summary>
    /// <param name="token">Token a procesar</param>
    /// <returns>Token procesado y normalizado</returns>
    /// <example>
    /// Input: "coconut" -> Output: "coco"
    /// Input: "ml." -> Output: "ml"
    /// </example>
    public string ProcessToken(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return string.Empty;

        token = token.Trim().ToLowerInvariant();

        // Aplicar sinónimos/normalizaciones definidas
        if (_synonyms.TryGetValue(token, out var synonym))
            return synonym;

        return token;
    }

    #endregion

    #region Métodos Privados de Extracción de Tokens

    /// <summary>
    /// Extrae patrones multifuncionales como "5en1", "3in1" que indican productos con múltiples funciones.
    /// Estos patrones son importantes para la búsqueda ya que usuarios buscan específicamente productos multifuncionales.
    /// </summary>
    /// <param name="text">Texto normalizado donde buscar patrones</param>
    /// <param name="tokens">HashSet donde agregar los tokens encontrados</param>
    /// <param name="debugInfo">Información de debug opcional para tracking</param>
    /// <example>
    /// Input: "champu 5en1 anticaspa"
    /// Tokens añadidos: "5en1", "5"
    /// </example>
    private void ExtractMultiInOneTokens(string text, HashSet<string> tokens, Dictionary<string, string> debugInfo = null)
    {
        foreach (Match match in _multiInOneRegex.Matches(text))
        {
            var fullMatch = match.Value.ToLowerInvariant();

            // Agregar el patrón completo (ej: "5en1")
            if (tokens.Add(fullMatch) && debugInfo != null)
            {
                debugInfo[fullMatch] = "PATRON_XenY";
            }

            // También extraer el número principal si es útil para búsquedas
            // Usuarios pueden buscar solo "5" esperando encontrar productos "5en1"
            var numberMatch = Regex.Match(fullMatch, @"(\d+)");
            if (numberMatch.Success)
            {
                var number = numberMatch.Groups[1].Value;
                if (!ShouldSkipToken(number))
                {
                    if (tokens.Add(number) && debugInfo != null)
                        debugInfo[number] = $"NUMERO (de {fullMatch})";
                }
            }
        }
    }

    /// <summary>
    /// Extrae medidas físicas (volumen, peso, dimensiones) y descriptores de modelo/tono.
    /// Diferencia entre medidas técnicas ("500ml") y identificadores de modelo ("tono 175").
    /// Las medidas son tokens de alta prioridad porque son términos de búsqueda muy específicos.
    /// </summary>
    /// <param name="text">Texto normalizado donde buscar medidas</param>
    /// <param name="tokens">HashSet donde agregar los tokens encontrados</param>
    /// <param name="debugInfo">Información de debug opcional para tracking</param>
    /// <example>
    /// Input: "crema 500ml tono 175"
    /// Tokens de medida: "500ml", "500", "ml"
    /// Tokens de modelo: "tono175", "175"
    /// </example>
    private void ExtractMeasurementTokens(string text, HashSet<string> tokens, Dictionary<string, string> debugInfo = null)
    {
        foreach (Match match in _measureRegex.Matches(text))
        {
            var fullMatch = match.Value.ToLowerInvariant().Trim();

            // DIFERENCIACIÓN CRÍTICA: ¿Es un modelo/tono o una medida física?
            if (Regex.IsMatch(fullMatch, @"\b(tono|tone|color|modelo|model|size|talla|numero|no\.?|ref\.?|#)\s+\d", RegexOptions.IgnoreCase))
            {
                // CASO 1: Es un identificador de modelo/tono (ej: "tono 175")
                // Estos se tratan como códigos únicos, no como medidas separables
                tokens.Add(fullMatch);

                if (debugInfo != null)
                    debugInfo[fullMatch] = $"MODELO_TONO_COMPLETO (original: {match.Value})";

                // También extraer solo el número para búsquedas directas por código
                var numberMatch = Regex.Match(fullMatch, @"\d+(?:[.,]\d+)?");
                if (numberMatch.Success && !ShouldSkipToken(numberMatch.Value))
                {
                    if (tokens.Add(numberMatch.Value) && debugInfo != null)
                        debugInfo[numberMatch.Value] = $"NUMERO (de {fullMatch})";
                }
            }
            else
            {
                // CASO 2: Es una medida física (ej: "500ml", "2kg")
                // ESTRATEGIA: Crear tokens que permitan búsquedas flexibles
                // - Token completo: "500ml" (búsqueda exacta)
                // - Token numérico: "500" (búsqueda por cantidad)
                // - Token de unidad: "ml" (búsqueda por tipo de medida)

                // Extraer número y unidad del match completo
                var numberMatch = Regex.Match(fullMatch, @"(\d+(?:[.,]\d+)?)");
                var unitMatch = Regex.Match(fullMatch, @"(ml|mL|l|L|lts|g|gr|kg|mm|cm|in|pulgadas?|litros?|gramos?|kilogramos?|centimetros?|milimetros?|oz|fl\s*oz|lb|lbs|ft|feet|gal|qt|pt|yd|cal|kcal|mg|mcg|µg|units?|unidades?|pcs|piezas?)", RegexOptions.IgnoreCase);

                if (numberMatch.Success && unitMatch.Success)
                {
                    var number = numberMatch.Groups[1].Value;
                    var unit = ProcessToken(unitMatch.Groups[1].Value); // Aplicar sinónimos a la unidad

                    // Token de medida completa normalizada (sin espacios, sin 'x')
                    var measureToken = $"{number}{unit}";
                    tokens.Add(measureToken);

                    if (debugInfo != null)
                        debugInfo[measureToken] = $"MEDIDA_COMPLETA (original: '{match.Value}', normalizada: '{measureToken}')";

                    // Agregar componentes separados si son útiles para búsquedas independientes
                    if (!ShouldSkipToken(number) && number.Length >= 2)
                    {
                        if (tokens.Add(number) && debugInfo != null)
                            debugInfo[number] = $"VALOR_MEDIDA (de {measureToken})";
                    }

                    // Solo agregar unidad si no es demasiado genérica
                    if (!IsGenericUnit(unit))
                    {
                        if (tokens.Add(unit) && debugInfo != null)
                            debugInfo[unit] = $"UNIDAD (de {measureToken})";
                    }
                }
                else
                {
                    // Fallback: si no pudimos parsear la medida, agregar como está
                    tokens.Add(fullMatch);
                    if (debugInfo != null)
                        debugInfo[fullMatch] = $"MEDIDA_COMPLETA_FALLBACK (original: '{match.Value}')";
                }
            }
        }
    }

    /// <summary>
    /// Extrae códigos alfanuméricos únicos como SKUs, códigos de barras, referencias de modelo.
    /// Estos códigos son tokens de alta especificidad y relevancia para búsquedas exactas.
    /// </summary>
    /// <param name="text">Texto normalizado donde buscar códigos</param>
    /// <param name="tokens">HashSet donde agregar los tokens encontrados</param>
    /// <param name="debugInfo">Información de debug opcional para tracking</param>
    /// <example>
    /// Input: "producto ABC123XYZ codigo SKU-456"
    /// Tokens añadidos: "abc123xyz", "sku-456"
    /// </example>
    private void ExtractUniqueCodeTokens(string text, HashSet<string> tokens, Dictionary<string, string> debugInfo = null)
    {
        // Regex que captura varios patrones de códigos:
        // - 2+ letras seguidas de 2+ números + opcional alfanumérico (ej: AB123, XYZ456C)
        // - Alfanumérico con 2+ números seguido de 2+ letras (ej: 123ABC, 45XY)
        // - 3+ letras seguidas de números (ej: SKU123, REF456)
        // - Códigos con separadores (ej: ABC-123, SKU_456, REF.789)
        var codeRegex = new Regex(@"\b(?:[A-Z]{2,}[0-9]{2,}[A-Z0-9]*|[A-Z0-9]*[0-9]{2,}[A-Z]{2,}|[A-Z]{3,}[0-9]+[A-Z]*|[A-Z0-9]+[-._][A-Z0-9]+(?:[-._][A-Z0-9]+)*)\b",
            RegexOptions.IgnoreCase);

        foreach (Match match in codeRegex.Matches(text))
        {
            var code = ProcessToken(match.Value);
            if (!string.IsNullOrEmpty(code) && !ShouldSkipToken(code))
            {
                if (tokens.Add(code) && debugInfo != null)
                {
                    debugInfo[code] = "CODIGO_UNICO";
                }
            }
        }
    }

    /// <summary>
    /// Extrae palabras generales después de que se han procesado tokens más específicos.
    /// Implementa lógica anti-overlap para evitar duplicar tokens ya capturados por medidas o códigos.
    /// Esta es la pasada de menor prioridad pero necesaria para capturar términos descriptivos.
    /// </summary>
    /// <param name="text">Texto normalizado donde buscar palabras</param>
    /// <param name="tokens">HashSet donde agregar los tokens encontrados</param>
    /// <param name="debugInfo">Información de debug opcional para tracking</param>
    private void ExtractGeneralTokens(string text, HashSet<string> tokens, Dictionary<string, string> debugInfo = null)
    {
        // ESTRATEGIA ANTI-OVERLAP: Crear un set de tokens y componentes ya procesados
        // para evitar extraer duplicados que ya fueron capturados en pasadas anteriores
        var alreadyProcessed = new HashSet<string>();

        foreach (var existing in tokens)
        {
            alreadyProcessed.Add(existing.ToLowerInvariant());

            // Marcar componentes de medidas como procesados (ej: de "500ml" marcar "ml")
            if (Regex.IsMatch(existing, @"^\d+[a-z]*$"))
            {
                var unitPart = Regex.Replace(existing, @"^\d+", "");
                if (!string.IsNullOrEmpty(unitPart))
                    alreadyProcessed.Add(unitPart);
            }

            // Marcar números de descriptores de modelo como procesados (ej: de "tono175" marcar "175")
            if (Regex.IsMatch(existing, @"^(tono|tone|color|modelo|model)\d+$"))
            {
                var numberPart = Regex.Replace(existing, @"^[a-z]+", "");
                if (!string.IsNullOrEmpty(numberPart))
                    alreadyProcessed.Add(numberPart);
            }

            // Marcar números de patrones "XenY" como procesados (ej: de "5en1" marcar "5" y "1")
            if (Regex.IsMatch(existing, @"^\d+en\d+$"))
            {
                var numbers = Regex.Matches(existing, @"\d+");
                foreach (Match numMatch in numbers)
                {
                    alreadyProcessed.Add(numMatch.Value);
                }
            }
        }

        // Extraer palabras que no hayan sido ya capturadas por pasadas anteriores
        foreach (Match match in _mainTokenRegex.Matches(text))
        {
            var rawToken = match.Value.ToLowerInvariant();

            // Saltar si ya se procesó como medida, código, tono o patrón XenY
            if (alreadyProcessed.Contains(rawToken))
                continue;

            // Saltar si es parte de una medida/tono que ya capturamos
            if (_measureRegex.IsMatch(match.Value))
                continue;

            // Saltar si es parte de un patrón XenY que ya capturamos
            if (_multiInOneRegex.IsMatch(match.Value))
                continue;

            // Aplicar filtros de calidad del token
            if (ShouldSkipToken(rawToken))
                continue;

            var processedToken = ProcessToken(rawToken);
            if (!string.IsNullOrEmpty(processedToken))
            {
                if (tokens.Add(processedToken) && debugInfo != null)
                {
                    debugInfo[processedToken] = "PALABRA_GENERAL";
                }
            }
        }
    }

    /// <summary>
    /// Determina si un descriptor indica un modelo o referencia de producto.
    /// Usado para diferenciar entre medidas físicas y códigos de identificación.
    /// </summary>
    /// <param name="descriptor">Palabra a evaluar (ej: "tono", "modelo", "ref")</param>
    /// <returns>true si es un descriptor de modelo, false si es otra cosa</returns>
    private bool IsModelDescriptor(string descriptor)
    {
        return _modelDescriptors.Contains(descriptor.ToLowerInvariant().Replace(".", ""));
    }

    /// <summary>
    /// Determina si una unidad es demasiado genérica para ser útil como token independiente.
    /// Las unidades genéricas (ml, g, kg) solo se incluyen como parte de medidas completas,
    /// mientras que unidades más específicas pueden ser tokens independientes útiles.
    /// </summary>
    /// <param name="unit">Unidad a evaluar</param>
    /// <returns>true si la unidad es muy genérica, false si puede ser útil sola</returns>
    private bool IsGenericUnit(string unit)
    {
        return _genericUnits.Contains(unit.ToLowerInvariant());
    }

    #endregion

    #region Sobrecargas de Compatibilidad (sin debug info)

    /// <summary>Sobrecarga sin debug info para ExtractMultiInOneTokens</summary>
    private void ExtractMultiInOneTokens(string text, HashSet<string> tokens)
    {
        ExtractMultiInOneTokens(text, tokens, null);
    }

    /// <summary>Sobrecarga sin debug info para ExtractUniqueCodeTokens</summary>
    private void ExtractUniqueCodeTokens(string text, HashSet<string> tokens)
    {
        ExtractUniqueCodeTokens(text, tokens, null);
    }

    /// <summary>Sobrecarga sin debug info para ExtractMeasurementTokens</summary>
    private void ExtractMeasurementTokens(string text, HashSet<string> tokens)
    {
        ExtractMeasurementTokens(text, tokens, null);
    }

    /// <summary>Sobrecarga sin debug info para ExtractGeneralTokens</summary>
    private void ExtractGeneralTokens(string text, HashSet<string> tokens)
    {
        ExtractGeneralTokens(text, tokens, null);
    }

    #endregion

    #region Métodos de Utilidad

    /// <summary>
    /// Elimina diacríticos (acentos, tildes, diéresis) de un texto para normalización.
    /// Convierte caracteres como á, é, í, ó, ú, ñ a sus equivalentes sin acentos.
    /// Es fundamental para que búsquedas funcionen independientemente de acentuación.
    /// </summary>
    /// <param name="text">Texto con posibles diacríticos</param>
    /// <returns>Texto sin diacríticos</returns>
    /// <example>
    /// Input: "Crema antiedad 50ml"
    /// Output: "crema antiedad 50ml"
    /// </example>
    private static string RemoveDiacritics(string text)
    {
        var normalized = text.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder();

        foreach (var c in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                sb.Append(c);
        }

        return sb.ToString().Normalize(NormalizationForm.FormC);
    }

    #endregion

    #region Definiciones de Expresiones Regulares Compiladas

    /// <summary>
    /// REGEX PRINCIPAL: Captura todos los tipos de tokens importantes en una sola pasada.
    /// Esta expresión regular está optimizada y compilada para máximo rendimiento.
    /// 
    /// PATRONES CAPTURADOS:
    /// 1. Números con unidades físicas: \d+(?:[.,]\d+)?\s*(?:ml|mL|l|L|lts|oz|...)
    ///    Ejemplos: "500ml", "2.5kg", "15 cm"
    ///    
    /// 2. Códigos alfanuméricos únicos: [A-Z]{2,}[0-9]{2,}[A-Z0-9]*
    ///    Ejemplos: "ABC123", "SKU456XYZ"
    ///    
    /// 3. Códigos con separadores: [A-Z0-9]+[-._][A-Z0-9]+
    ///    Ejemplos: "REF-123", "SKU_456", "CODE.789"
    ///    
    /// 4. Patrones multifuncionales: \d+\s*(?:en|in)\s*\d+
    ///    Ejemplos: "5en1", "3 in 1"
    ///    
    /// 5. Palabras normales: [a-zA-Z]{2,}
    ///    Ejemplos: "champú", "crema", "aceite"
    /// </summary>
    [GeneratedRegex(@"\b(?:" +
        // 1. Números con unidades (capturado por _measureRegex pero también aquí para completitud)
        @"\d+(?:[.,]\d+)?\s*(?:ml|mL|l|L|lts|oz|fl\s*oz|g|gr|kg|lb|lbs|in|ft|cm|mm|gal|qt|pt|yd|mi|cal|kcal|mg|mcg|µg|units?|pcs|pack|box|can|tube|bag|roll|bottle|lata|botella|caja|bolsa|rollo|paquete|unidades?|piezas?)\b|" +

        // 2. Códigos alfanuméricos únicos (ej: MUVWW00900133C, SKU123, ABC-456)
        @"[A-Z]{2,}[0-9]{2,}[A-Z0-9]*|[A-Z0-9]*[0-9]{2,}[A-Z]{2,}|[A-Z]{3,}[0-9]+[A-Z]*|" +

        // 3. Códigos con separadores (ej: ABC-123, REF.456, SKU_789)
        @"[A-Z0-9]+[-._][A-Z0-9]+(?:[-._][A-Z0-9]+)*|" +

        // 4. Patrones XenY (ej: 5en1, 3in1)
        @"\d+\s*(?:en|in)\s*\d+|" +

        // 5. Palabras normales (2+ letras)
        @"[a-zA-Z]{2,}" +

        @")\b", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex MainTokenRegex();

    /// <summary>
    /// REGEX ESPECÍFICA PARA MEDIDAS Y TONOS: Captura patrones completos de medidas físicas y descriptores de modelo.
    /// 
    /// PATRONES CAPTURADOS:
    /// 1. Descriptores de modelo/tono: (?:tono|tone|color|modelo|model|size|talla|numero|no\.?|ref\.?|#)\s+\d+(?:[.,]\d+)?
    ///    Ejemplos: "tono 175", "modelo ABC", "ref. 123", "size 42"
    ///    
    /// 2. Medidas físicas: x?\d+(?:[.,]\d+)?\s*(?:ml|mL|l|L|lts|g|gr|kg|mm|cm|in|pulgadas?|...)
    ///    Ejemplos: "500ml", "x25cm", "2.5kg", "15 pulgadas"
    ///    
    /// NOTA: La 'x' opcional al inicio captura dimensiones como "x25cm" en descripciones de tamaño.
    /// </summary>
    [GeneratedRegex(@"\b(?:(?:tono|tone|color|modelo|model|size|talla|numero|no\.?|ref\.?|#)\s+\d+(?:[.,]\d+)?|x?\d+(?:[.,]\d+)?\s*(?:ml|mL|l|L|lts|g|gr|kg|mm|cm|in|pulgadas?|litros?|gramos?|kilogramos?|centimetros?|milimetros?|oz|fl\s*oz|lb|lbs|ft|feet|gal|qt|pt|yd|cal|kcal|mg|mcg|µg|units?|unidades?|pcs|piezas?))\b",
        RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex MeasureRegex();

    /// <summary>
    /// REGEX PARA PATRONES MULTIFUNCIONALES: Captura específicamente patrones "XenY" y "XinY".
    /// 
    /// PATRÓN: (\d+)\s*(?:en|in)\s*(\d+)
    /// - Grupo 1: Número principal (ej: "5" en "5en1")
    /// - Grupo 2: Número secundario (ej: "1" en "5en1")
    /// - Espacios opcionales alrededor de "en" o "in"
    /// 
    /// EJEMPLOS CAPTURADOS:
    /// - "5en1" -> grupos: ("5", "1")
    /// - "3 in 1" -> grupos: ("3", "1") 
    /// - "10en1" -> grupos: ("10", "1")
    /// 
    /// PROPÓSITO: Productos multifuncionales son una categoría importante en ecommerce,
    /// especialmente en cosméticos, herramientas y productos de limpieza.
    /// </summary>
    [GeneratedRegex(@"\b(\d+)\s*(?:en|in)\s*(\d+)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex MultiInOneRegex();

    /// <summary>
    /// REGEX PARA NORMALIZACIÓN DE ESPACIOS: Convierte múltiples espacios consecutivos en uno solo.
    /// 
    /// PATRÓN: \s+
    /// - Captura uno o más caracteres de espacio en blanco consecutivos
    /// - Se reemplaza por un solo espacio " "
    /// 
    /// PROPÓSITO: Limpiar texto de entrada que puede tener espaciado inconsistente,
    /// tabs, múltiples espacios, etc. Parte del proceso de normalización previo a tokenización.
    /// </summary>
    [GeneratedRegex(@"\s+", RegexOptions.Compiled)]
    private static partial Regex NormalizerRegex();

    #endregion
}