CREATE OR ALTER PROCEDURE [dbo].[GetProductSuggestionsCount]
@ContainsQuery NVARCHAR(4000)
AS
BEGIN
    SET NOCOUNT ON;

    IF LTRIM(RTRIM(ISNULL(@ContainsQuery, ''))) = ''
        BEGIN
            SELECT 0;
            RETURN;
        END

    DECLARE @LikePattern NVARCHAR(100) = '%' + REPLACE(REPLACE(@ContainsQuery, '"', ''), '*', '') + '%';

    SELECT COUNT(*)
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
        );
END
GO

CREATE OR ALTER PROCEDURE [dbo].[GetProductSuggestionsPaged]
@ContainsQuery NVARCHAR(4000),
@PageIndex INT = 0,
@PageSize INT = 10
AS
BEGIN
    SET NOCOUNT ON;

    IF LTRIM(RTRIM(ISNULL(@ContainsQuery, ''))) = ''
        RETURN;

    IF @PageIndex IS NULL OR @PageIndex < 0 SET @PageIndex = 0;
    IF @PageSize IS NULL OR @PageSize < 1 SET @PageSize = 10;
    IF @PageSize > 100 SET @PageSize = 100;

    DECLARE @LikePattern NVARCHAR(100) = '%' + REPLACE(REPLACE(@ContainsQuery, '"', ''), '*', '') + '%';
    DECLARE @Offset INT = @PageIndex * @PageSize;

    SELECT 
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
        p.[Id] ASC
    OFFSET @Offset ROWS
    FETCH NEXT @PageSize ROWS ONLY;
END
GO

DROP INDEX IF EXISTS [IX_Product_Search_Optimized] ON [dbo].[Product];
GO

CREATE NONCLUSTERED INDEX [IX_Product_Search_Optimized]
ON [dbo].[Product] ([Published], [Deleted], [VisibleIndividually])
INCLUDE ([Id], [Name], [ShortDescription], [FullDescription], [StockQuantity], [AvailableEndDateTimeUtc])
WHERE ([Published] = 1 AND [Deleted] = 0 AND [VisibleIndividually] = 1);
GO