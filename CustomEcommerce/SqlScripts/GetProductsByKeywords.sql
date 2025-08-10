
---- Activar full text search en la base de datos
IF NOT EXISTS (SELECT 1 FROM sys.fulltext_catalogs WHERE name = N'FTCatalog')
BEGIN
  CREATE FULLTEXT CATALOG FTCatalog
  WITH ACCENT_SENSITIVITY = OFF
  AS DEFAULT;
END
GO

-- Crear índice full-text sobre Product.Name
IF NOT EXISTS (
  SELECT 1
  FROM sys.fulltext_indexes fi
  JOIN sys.objects o ON fi.object_id = o.object_id
  WHERE o.name = N'Product'
)
BEGIN
  CREATE FULLTEXT INDEX ON  [market-kivio-ecommerce-db-andres-dev].[dbo].[Product]
    (
        Name LANGUAGE 3082,
        Sku  LANGUAGE 3082
    )
    KEY INDEX PK_Product
    WITH CHANGE_TRACKING AUTO, STOPLIST = SYSTEM;
END
GO


-- Crear procedimiento almacenado para buscar productos por consulta FTS

USE [market-kivio-ecommerce-db-andres-dev];
GO

CREATE OR ALTER PROCEDURE dbo.GetProductsByFtsQuery
    @FtsQuery      NVARCHAR(400),
    @MaxCandidates INT = 200
AS
BEGIN
    SET NOCOUNT ON;
    
    IF @FtsQuery IS NULL OR LTRIM(RTRIM(@FtsQuery)) = N''
    BEGIN
        SELECT TOP (0)
            CAST(NULL AS INT) AS Id, 
            CAST(NULL AS NVARCHAR(MAX)) AS Name, 
            CAST(NULL AS NVARCHAR(MAX)) AS Sku, 
            CAST(NULL AS DECIMAL(18,2)) AS Price, 
            CAST(NULL AS NVARCHAR(MAX)) AS ShortDescription,
            CAST(0 AS INT) AS FtRank;
        RETURN;
    END

    ;WITH FT AS
    (
        SELECT TOP (@MaxCandidates)
            [KEY]  AS ProductId,
            [RANK] AS FtRank
        FROM CONTAINSTABLE([dbo].[Product], (Name, Sku), @FtsQuery, LANGUAGE 3082)
        ORDER BY [RANK] DESC
    )
    SELECT
        p.Id,
        p.Name,
        p.Sku,
        p.Price,
        p.ShortDescription,
        FT.FtRank
    FROM FT
    JOIN [dbo].[Product] AS p ON p.Id = FT.ProductId
    WHERE p.Published = 1
      AND p.Deleted   = 0
    ORDER BY FT.FtRank DESC, p.Id DESC;
END
GO
