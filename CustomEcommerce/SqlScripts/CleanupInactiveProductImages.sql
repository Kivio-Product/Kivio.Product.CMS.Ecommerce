CREATE OR ALTER PROCEDURE [dbo].[CleanupInactiveProductImages]
    @ExcludedProductIds VARCHAR(MAX) = NULL -- csv: '12,34,56' (productos que NO deben considerarse para eliminar)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @StartTime DATETIME = GETDATE();

    BEGIN TRANSACTION;

    BEGIN TRY
        DECLARE @Excluded TABLE (ProductId INT PRIMARY KEY);
        IF @ExcludedProductIds IS NOT NULL AND LTRIM(RTRIM(@ExcludedProductIds)) <> ''
        BEGIN
            INSERT INTO @Excluded (ProductId)
            SELECT DISTINCT TRY_CAST(value AS INT)
            FROM STRING_SPLIT(@ExcludedProductIds, ',')
            WHERE TRY_CAST(value AS INT) IS NOT NULL;
        END

        DECLARE @PicturesToDelete TABLE (
            PictureId INT PRIMARY KEY,
            Bytes BIGINT
        );

        INSERT INTO @PicturesToDelete (PictureId, Bytes)
        SELECT DISTINCT ppm.PictureId,
               ISNULL(DATALENGTH(pb.BinaryData), 0) AS Bytes
        FROM dbo.Product p
        JOIN dbo.Product_Picture_Mapping ppm ON ppm.ProductId = p.Id
        LEFT JOIN dbo.PictureBinary pb ON pb.PictureId = ppm.PictureId
        WHERE (p.Published = 0 OR ISNULL(p.StockQuantity,0) = 0)
          AND (NOT EXISTS (SELECT 1 FROM @Excluded e WHERE e.ProductId = p.Id)); 

        IF NOT EXISTS (SELECT 1 FROM @PicturesToDelete)
        BEGIN
            ROLLBACK TRANSACTION;
            SELECT 
                0 AS PicturesRemoved,
                0 AS BytesFreed,
                CAST(0.0 AS DECIMAL(18,2)) AS MBFreed,
                'No images matched criteria.' AS Message;
            RETURN;
        END

        DELETE ptd
        FROM @PicturesToDelete ptd
        WHERE EXISTS (
            SELECT 1
            FROM dbo.Product_Picture_Mapping ppm2
            JOIN dbo.Product p2 ON ppm2.ProductId = p2.Id
            WHERE ppm2.PictureId = ptd.PictureId
              AND p2.Published = 1
              AND ISNULL(p2.StockQuantity,0) > 0
        )
        OR EXISTS (
            SELECT 1
            FROM dbo.Product_Picture_Mapping ppm3
            WHERE ppm3.PictureId = ptd.PictureId
              AND ppm3.ProductId IN (SELECT ProductId FROM @Excluded)
        );

        DECLARE @PicturesCount INT = (SELECT COUNT(*) FROM @PicturesToDelete);
        IF @PicturesCount = 0
        BEGIN
            ROLLBACK TRANSACTION;
            SELECT 
                0 AS PicturesRemoved,
                0 AS BytesFreed,
                CAST(0.0 AS DECIMAL(18,2)) AS MBFreed,
                'After filtering for active/excluded products, no images to delete.' AS Message;
            RETURN;
        END

        DECLARE @TotalBytes BIGINT = (SELECT SUM(ISNULL(Bytes,0)) FROM @PicturesToDelete);

        DELETE ppm
        FROM dbo.Product_Picture_Mapping ppm
        WHERE ppm.PictureId IN (SELECT PictureId FROM @PicturesToDelete);

        DELETE pb
        FROM dbo.PictureBinary pb
        WHERE pb.PictureId IN (SELECT PictureId FROM @PicturesToDelete);

        DELETE pic
        FROM dbo.Picture pic
        WHERE pic.Id IN (SELECT PictureId FROM @PicturesToDelete);

        COMMIT TRANSACTION;

        SELECT 
            (SELECT COUNT(*) FROM @PicturesToDelete) AS PicturesRemoved,
            @TotalBytes AS BytesFreed,
            CAST(@TotalBytes / 1024.0 / 1024.0 AS DECIMAL(18,2)) AS MBFreed,
            @StartTime AS StartedAt,
            GETDATE() AS FinishedAt,
            'Deletion completed successfully.' AS Message;

    END TRY
    BEGIN CATCH
        DECLARE @ErrMsg NVARCHAR(4000) = ERROR_MESSAGE();
        DECLARE @ErrSeverity INT = ERROR_SEVERITY();
        DECLARE @ErrState INT = ERROR_STATE();

        IF XACT_STATE() <> 0
            ROLLBACK TRANSACTION;

        SELECT 
            0 AS PicturesRemoved,
            0 AS BytesFreed,
            CAST(0.0 AS DECIMAL(18,2)) AS MBFreed,
            @StartTime AS StartedAt,
            GETDATE() AS FinishedAt,
            'Error occurred. Transaction rolled back.' AS Message,
            @ErrMsg AS ErrorMessage;
    END CATCH
END
GO


INSERT INTO [dbo].[ScheduleTask] (
    [Name],
    [Seconds],
    [Type],
    [Enabled],
    [StopOnError],
    [LastStartUtc],
    [LastEndUtc],
    [LastSuccessUtc]
)
VALUES (
    'Cleanup inactive product images',
    86400, -- 24 horas = 86400 segundos
    'Nop.Services.Catalog.CleanupInactiveProductImagesTask, Nop.Services',
    1, -- Habilitado
    0, -- No detener en error
    NULL,
    NULL,
    NULL
);
GO