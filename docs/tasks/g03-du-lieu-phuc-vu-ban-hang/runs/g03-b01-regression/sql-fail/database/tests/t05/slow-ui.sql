-- Run on the T05 isolated database in a separate SSMS session after test approval.
-- While this transaction holds BAN locks, click Load in the real WinForms app.
-- App connection must use a dedicated principal granted the T03 role.
IF DB_NAME() NOT LIKE 'CoffeeManager_Test_T05[_]%' THROW 52910,'T05 test DB required.',1;
SET XACT_ABORT ON;
BEGIN TRY
    BEGIN TRAN;
    SELECT * FROM dbo.BAN WITH(TABLOCKX,HOLDLOCK);
    WAITFOR DELAY '00:00:20';
    ROLLBACK;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT>0 ROLLBACK;
    THROW;
END CATCH;
