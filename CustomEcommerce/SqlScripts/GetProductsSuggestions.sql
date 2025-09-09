CREATE OR ALTER PROCEDURE [dbo].[GetProductSuggestions]
    @FtsQuery   NVARCHAR(1000),
    @MaxResults INT = 10
AS
BEGIN
    SET NOCOUNT ON;

    IF LTRIM(RTRIM(ISNULL(@FtsQuery, ''))) = ''
        RETURN;

    IF @MaxResults IS NULL OR @MaxResults < 1 SET @MaxResults = 10;
    IF @MaxResults > 100 SET @MaxResults = 100;

    DECLARE @TopN INT = @MaxResults * 2;  
    IF @TopN > 1000 SET @TopN = 1000;  

    SELECT TOP (@MaxResults)
        p.Id,
        p.Name,
        ft.[RANK] AS FtRank
    FROM [dbo].[Product] AS p
    INNER JOIN FREETEXTTABLE(
        [dbo].[Product],
        (Name, ShortDescription, FullDescription),
        @FtsQuery,
        @TopN
    ) AS ft
        ON p.[Id] = ft.[KEY]
    WHERE p.[Published] = 1
      AND p.[Deleted] = 0
      AND p.[VisibleIndividually] = 1
      AND ft.[RANK] > 20
      AND p.[StockQuantity] > 0
    ORDER BY ft.[RANK] DESC, p.[Id] ASC;
END
GO


-- Índices recomendados para optimizar la consulta
CREATE NONCLUSTERED INDEX [IX_Product_Search_Optimized] 
ON [dbo].[Product] ([Published], [Deleted], [VisibleIndividually], [StockQuantity])
INCLUDE ([Id], [Name], [ShortDescription], [FullDescription])
WHERE ([Published] = 1 AND [Deleted] = 0);
GO