-- Test-only rollback injection; install only in marker-protected G02 fixture DB.
IF OBJECT_ID(N'dbo.G02TestMarker',N'U') IS NULL THROW 52400,'G02 test marker required.',1;
GO
CREATE OR ALTER TRIGGER dbo.G02_FailSetupCompletion ON dbo.AppSetupState AFTER UPDATE AS
BEGIN
 IF EXISTS(SELECT 1 FROM inserted WHERE State='Completed') THROW 52401,'Controlled setup rollback fixture.',1;
END;
GO
-- Remove this test-only trigger after the subcase; do not change expected to tolerate it.
