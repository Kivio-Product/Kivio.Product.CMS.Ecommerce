/* ============================================================
   SISTEMA DE BÚSQUEDA AVANZADA DE PRODUCTOS
   ============================================================
   
   PROPÓSITO:
   Este script configura un sistema completo de búsqueda de productos
   que combina Full-Text Search (FTS) de SQL Server con métricas IDF
   (Inverse Document Frequency) para mejorar la relevancia de resultados.
   
   COMPONENTES PRINCIPALES:
   1. Catálogo e Índice Full-Text Search
   2. Stored Procedure para búsquedas FTS con filtros de precio
   3. Infraestructura de estadísticas IDF para ranking avanzado
   4. Vistas para monitoreo y análisis de tokens
   
   ARQUITECTURA:
   - FTS para búsquedas rápidas y flexibles en texto natural
   - IDF scoring para mejorar relevancia basada en frecuencia de términos
   - Filtros opcionales por precio para refinamiento de resultados
   - Soporte para español (LANGUAGE 3082)
   
   CASOS DE USO:
   - Búsqueda por nombre de producto: "champú pantene"
   - Búsqueda por SKU: "ABC123"
   - Búsqueda por descripción: "cabello graso"
   - Búsqueda con filtro de precio: precio similar a $500
   - Análisis de relevancia de términos de búsqueda
   
   ============================================================ */

/* ============================================================
   CONFIGURACIÓN DE BASE DE DATOS
   ============================================================ */
USE [market-kivio-ecommerce-db-andres-dev];
GO

/* ============================================================
   INFRAESTRUCTURA FULL-TEXT SEARCH
   ============================================================ */

-- ============================================================
-- CATÁLOGO FULL-TEXT
-- ============================================================
-- Crea el catálogo FTS si no existe. El catálogo es el contenedor
-- principal para todos los índices full-text de la base de datos.
-- ACCENT_SENSITIVITY = OFF permite búsquedas sin considerar acentos
IF NOT EXISTS (SELECT 1 FROM sys.fulltext_catalogs WHERE name = N'FTCatalog')
BEGIN
  CREATE FULLTEXT CATALOG FTCatalog
  WITH ACCENT_SENSITIVITY = OFF  -- Búsquedas insensibles a acentos
  AS DEFAULT;                    -- Catálogo por defecto para la DB
  
  PRINT 'Catálogo Full-Text creado: FTCatalog';
END
ELSE
BEGIN
  PRINT 'Catálogo Full-Text ya existe: FTCatalog';
END
GO

-- ============================================================
-- ÍNDICE FULL-TEXT EN TABLA PRODUCT
-- ============================================================
-- Crea índice FTS en campos clave de Product para búsquedas rápidas.
-- Indexa Name, Sku y ShortDescription con idioma español (3082).
IF NOT EXISTS (
  SELECT 1
  FROM sys.fulltext_indexes fi
  WHERE fi.object_id = OBJECT_ID(N'dbo.Product')
)
BEGIN
  CREATE FULLTEXT INDEX ON dbo.Product
    (
        Name LANGUAGE 3082,             -- Español
        Sku  LANGUAGE 3082,             -- SKUs también en español
        ShortDescription LANGUAGE 3082,  -- Descripciones cortas
        FullDescription LANGUAGE 3082    -- Descripciones largas
    )
    KEY INDEX PK_Product                -- Usar clave primaria como KEY INDEX
    WITH CHANGE_TRACKING AUTO,          -- Actualización automática del índice
         STOPLIST = SYSTEM;              -- Usar stoplist del sistema (filtrar palabras comunes)
         
  PRINT 'Índice Full-Text creado en tabla Product';
END
ELSE
BEGIN
  PRINT 'Índice Full-Text ya existe en tabla Product';
END
GO

/* ============================================================
   STORED PROCEDURE PARA BÚSQUEDA FULL-TEXT
   ============================================================ */

-- ============================================================
-- PROCEDIMIENTO: GetProductsByFtsQuery
-- ============================================================
-- Realiza búsquedas avanzadas de productos usando Full-Text Search
-- con capacidades de filtrado por precio y ranking por relevancia.
--
-- PARÁMETROS:
-- @FtsQuery: Consulta en lenguaje natural (ej: "champú cabello graso")
-- @MaxCandidates: Límite máximo de resultados (optimización de performance)
-- @Price: Precio de referencia para filtrado opcional
-- @MaxPriceDiffPercent: Porcentaje de variación permitido del precio
CREATE OR ALTER PROCEDURE [dbo].[GetProductsByFtsQuery]
    @FtsQuery NVARCHAR(4000),              
    @MaxCandidates INT = 100,         
    @Price DECIMAL(18, 4) = NULL,          
    @MaxPriceDiffPercent DECIMAL(5, 2) = NULL  
AS
BEGIN
    SET NOCOUNT ON;
    SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;

    IF LTRIM(RTRIM(ISNULL(@FtsQuery, ''))) = '' OR @MaxCandidates <= 0
    BEGIN
        RETURN;
    END
    
    SET @MaxCandidates = CASE 
        WHEN @MaxCandidates > 200 THEN 200 
        ELSE @MaxCandidates 
    END;
    
    DECLARE @PriceMin DECIMAL(18, 4) = NULL;
    DECLARE @PriceMax DECIMAL(18, 4) = NULL;
    
    IF @Price IS NOT NULL AND @MaxPriceDiffPercent IS NOT NULL AND @MaxPriceDiffPercent > 0
    BEGIN
        DECLARE @PriceFactor DECIMAL(10, 6) = @MaxPriceDiffPercent / 100.0;
        SET @PriceMin = @Price * (1.0 - @PriceFactor);
        SET @PriceMax = @Price * (1.0 + @PriceFactor);
    END
    
    SELECT TOP (@MaxCandidates)
        p.Id,
        p.Name,
        ISNULL(p.Sku, '') as Sku,
        p.Price,
        ISNULL(p.ShortDescription, '') as ShortDescription,
        ft.RANK as FtRank
    FROM Product p WITH (NOLOCK, INDEX(PK_Product))
    INNER JOIN FREETEXTTABLE(Product, (Name, Sku, FullDescription, ShortDescription), @FtsQuery, @MaxCandidates) ft
        ON p.Id = ft.[KEY]
    WHERE p.Published = 1              
        AND p.Deleted = 0
        AND (@PriceMin IS NULL OR p.Price >= @PriceMin)
        AND (@PriceMax IS NULL OR p.Price <= @PriceMax)
    ORDER BY ft.RANK DESC
    OPTION (
        MAXDOP 1,           
        RECOMPILE,          
        FAST 50             
    );
END
GO


-- ============================================================
-- ÍNDICES PARA BATCHING
-- ============================================================

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Product_Deduplication_Optimized' AND object_id = OBJECT_ID('Product'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_Product_Deduplication_Optimized 
    ON [Product] (Published, Deleted) 
    INCLUDE (Id, Name, Sku, Price, ShortDescription)
    WITH (
        ONLINE = ON, 
        FILLFACTOR = 85,       
        PAD_INDEX = ON,
        SORT_IN_TEMPDB = ON,    
        MAXDOP = 1              
    );
    
    PRINT 'Índice IX_Product_Deduplication_Optimized creado exitosamente';
END

/* ============================================================
   INFRAESTRUCTURA DE SCORING IDF (INVERSE DOCUMENT FREQUENCY)
   ============================================================ */

-- ============================================================
-- TABLA: TokenStats
-- ============================================================
-- Almacena estadísticas de frecuencia de tokens para cálculo de IDF.
-- El IDF mejora la relevancia penalizando términos muy comunes y
-- premiando términos específicos/raros que aportan más información.
--
-- CAMPOS:
-- Token: Término extraído de productos (normalizado)
-- DocFreq: Frecuencia de documentos (en cuántos productos aparece)
-- Idf: Inverse Document Frequency calculado = log((N+1)/(DocFreq+1)) + 1
-- LastComputed: Timestamp de última actualización (para invalidación de cache)
--
-- FÓRMULA IDF:
-- IDF = log((N + 1) / (DocFreq + 1)) + 1
-- Donde N = total de productos publicados
IF OBJECT_ID(N'dbo.TokenStats', N'U') IS NULL
BEGIN
  CREATE TABLE dbo.TokenStats (
    Token        nvarchar(100) NOT NULL PRIMARY KEY,  -- Token normalizado
    DocFreq      int           NOT NULL,              -- Frecuencia en documentos
    Idf          float         NOT NULL,              -- Valor IDF calculado
    LastComputed datetime2     NOT NULL DEFAULT sysutcdatetime()  -- Timestamp de cálculo
  );
  
  PRINT 'Tabla TokenStats creada para estadísticas IDF';
END
ELSE
BEGIN
  PRINT 'Tabla TokenStats ya existe';
END
GO

-- ============================================================
-- TIPO DE TABLA: TokenStatUpsertType
-- ============================================================
-- Tipo de tabla para operaciones batch en UpsertTokenStats.
-- Permite actualizar estadísticas de múltiples tokens eficientemente
-- en una sola operación MERGE.
IF TYPE_ID(N'dbo.TokenStatUpsertType') IS NULL
BEGIN
  CREATE TYPE dbo.TokenStatUpsertType AS TABLE
  (
    Token   nvarchar(100),  -- Token a actualizar
    DocFreq int             -- Nueva frecuencia de documentos
  );
  
  PRINT 'Tipo de tabla TokenStatUpsertType creado';
END
ELSE
BEGIN
  PRINT 'Tipo de tabla TokenStatUpsertType ya existe';
END
GO

-- ============================================================
-- PROCEDIMIENTO: UpsertTokenStats
-- ============================================================
-- Actualiza estadísticas IDF para un conjunto de tokens de manera eficiente.
-- Usa operación MERGE para insertar nuevos tokens o actualizar existentes.
--
-- PARÁMETROS:
-- @Stats: Tabla con tokens y sus nuevas frecuencias de documento
-- @N: Total de productos publicados (necesario para cálculo IDF)
--
-- OPERACIÓN:
-- - Si token existe: actualiza DocFreq, recalcula IDF, actualiza timestamp
-- - Si token no existe: inserta nuevo registro con IDF calculado
--
-- FÓRMULA IDF APLICADA:
-- IDF = LOG((N + 1) / (DocFreq + 1)) + 1
-- - +1 en numerador y denominador: suavizado de Laplace (evita log(0))
-- - +1 al final: asegura IDF positivo para todos los tokens
CREATE OR ALTER PROCEDURE dbo.UpsertTokenStats
  @Stats dbo.TokenStatUpsertType READONLY,  -- Tokens con sus frecuencias
  @N     int                                -- Total de productos publicados
AS
BEGIN
  SET NOCOUNT ON;

  -- ========================================
  -- OPERACIÓN MERGE PARA UPSERT
  -- ========================================
  MERGE dbo.TokenStats AS tgt
  USING (SELECT Token, DocFreq FROM @Stats) AS s
    ON tgt.Token = s.Token
  WHEN MATCHED THEN
    -- Actualizar token existente con nueva frecuencia y IDF recalculado
    UPDATE SET
      DocFreq      = s.DocFreq,
      Idf          = LOG( (CAST(@N AS float) + 1.0) / (CAST(s.DocFreq AS float) + 1.0) ) + 1.0,
      LastComputed = SYSUTCDATETIME()
  WHEN NOT MATCHED THEN
    -- Insertar nuevo token con IDF calculado
    INSERT (Token, DocFreq, Idf, LastComputed)
    VALUES (s.Token, s.DocFreq,
            LOG( (CAST(@N AS float) + 1.0) / (CAST(s.DocFreq AS float) + 1.0) ) + 1.0,
            SYSUTCDATETIME());
            
  -- Información de diagnóstico
  DECLARE @UpdatedCount int = @@ROWCOUNT;
  PRINT 'TokenStats: ' + CAST(@UpdatedCount AS nvarchar(10)) + ' registros procesados';
END
GO

-- ============================================================
-- VISTA: vw_TokenStats_TopIdf
-- ============================================================
-- Vista de análisis que muestra los tokens con mayor valor IDF.
-- Útil para monitoreo, debugging y análisis del comportamiento del sistema.
--
-- PROPÓSITO:
-- - Identificar términos más discriminativos (alta relevancia)
-- - Detectar tokens potencialmente problemáticos
-- - Análisis de calidad del tokenizador
-- - Monitoring de performance del sistema de búsqueda
--
-- INTERPRETACIÓN DE RESULTADOS:
-- - IDF Alto (>3.0): Términos muy específicos, alta relevancia
-- - IDF Medio (1.5-3.0): Términos moderadamente específicos
-- - IDF Bajo (<1.5): Términos comunes, baja relevancia
IF OBJECT_ID(N'dbo.vw_TokenStats_TopIdf', N'V') IS NOT NULL
  DROP VIEW dbo.vw_TokenStats_TopIdf;
GO

CREATE VIEW dbo.vw_TokenStats_TopIdf
AS
SELECT TOP 200 
    Token,           -- Token normalizado
    DocFreq,         -- Frecuencia en documentos (cuántos productos lo contienen)
    Idf,            -- Valor IDF calculado (relevancia inversa)
    LastComputed    -- Timestamp de última actualización
FROM dbo.TokenStats
ORDER BY Idf DESC;  -- Ordenar por relevancia descendente (más específicos primero)
GO