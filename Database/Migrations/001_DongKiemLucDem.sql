-- SQL Server 2017+ | Apply to the existing POS database before deploying the services.
SET NOCOUNT ON;
SET XACT_ABORT ON;

IF OBJECT_ID(N'dbo.DongKiem', N'U') IS NULL
    THROW 50020, N'DongKiem is missing from the selected database.', 1;

BEGIN TRY
    BEGIN TRANSACTION;

    DECLARE @LockResult int;
    EXEC @LockResult = sys.sp_getapplock
        @Resource = N'RetailCorePos.Migration.001_DongKiemLucDem',
        @LockMode = 'Exclusive',
        @LockOwner = 'Transaction',
        @LockTimeout = 5000;
    IF @LockResult < 0
        THROW 50021, N'Unable to acquire the migration lock.', 1;

    IF COL_LENGTH(N'dbo.DongKiem', N'lucDem') IS NULL
        ALTER TABLE dbo.DongKiem ADD lucDem datetime2(3) NULL;

    IF OBJECT_ID(N'dbo.CK_DongKiem_LucDem', N'C') IS NULL
        EXEC sys.sp_executesql N'ALTER TABLE dbo.DongKiem
            ADD CONSTRAINT CK_DongKiem_LucDem CHECK (lucDem IS NULL OR soLuongDem IS NOT NULL);';

    IF NOT EXISTS (SELECT 1 FROM sys.indexes
        WHERE object_id = OBJECT_ID(N'dbo.DongKiem') AND name = N'IX_DongKiem_Hang')
        EXEC sys.sp_executesql N'CREATE INDEX IX_DongKiem_Hang ON dbo.DongKiem (maHang, maPhieuKiem) INCLUDE (lucDem);';

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
