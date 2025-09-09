CREATE OR ALTER PROCEDURE [dbo].[GetProductSuggestions]
@ContainsQuery NVARCHAR(4000),
@MaxResults INT = 10
AS
BEGIN
SET NOCOUNT ON;

IF LTRIM(RTRIM(ISNULL(@ContainsQuery, ''))) = ''
    RETURN;

IF @MaxResults IS NULL OR @MaxResults < 1 SET @MaxResults = 10;
IF @MaxResults > 100 SET @MaxResults = 100;

DECLARE @LikePattern NVARCHAR(100) = '%' + REPLACE(REPLACE(@ContainsQuery, '"', ''), '*', '') + '%';

SELECT TOP (@MaxResults)
    p.Id,
    p.Name,
    ISNULL(ct.[RANK], 50) AS FtRank
FROM [dbo].[Product] AS p
LEFT JOIN CONTAINSTABLE(
    [dbo].[Product],
    (Name, ShortDescription, FullDescription),
    @ContainsQuery
) AS ct ON p.[Id] = ct.[KEY]
WHERE p.[Published] = 1
    AND p.[Deleted] = 0
    AND p.[VisibleIndividually] = 1
    AND p.[StockQuantity] > 0
    AND (p.[AvailableEndDateTimeUtc] IS NULL OR p.[AvailableEndDateTimeUtc] > SYSUTCDATETIME())
    AND (
        ct.[KEY] IS NOT NULL 
        OR (
            ct.[KEY] IS NULL AND (
                p.[Name] LIKE @LikePattern 
                OR p.[ShortDescription] LIKE @LikePattern 
                OR p.[FullDescription] LIKE @LikePattern
            )
        )
    )
ORDER BY 
    CASE WHEN ct.[KEY] IS NOT NULL THEN ct.[RANK] ELSE 50 END DESC,
    CASE 
        WHEN p.[Name] LIKE @LikePattern THEN 1 
        WHEN p.[ShortDescription] LIKE @LikePattern THEN 2 
        ELSE 3 
    END,
    LEN(p.[Name]),
    p.[Id] ASC;

END
GO

DROP INDEX IF EXISTS [IX_Product_Search_Optimized] ON [dbo].[Product];
GO

CREATE NONCLUSTERED INDEX [IX_Product_Search_Optimized]
ON [dbo].[Product] ([Published], [Deleted], [VisibleIndividually])
INCLUDE ([Id], [Name], [ShortDescription], [FullDescription], [StockQuantity], [AvailableEndDateTimeUtc])
WHERE ([Published] = 1 AND [Deleted] = 0 AND [VisibleIndividually] = 1);
GO