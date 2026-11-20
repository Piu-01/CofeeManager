using System.Security.Cryptography;
using CafeManager.Business.Security;
using CafeManager.Business.Services;
using CafeManager.DataAccess;
using CafeManager.DataAccess.Repositories;
using CafeManager.Models.Requests;
using Microsoft.Data.SqlClient;
using Xunit;

namespace CafeManager.Tests.G02;

[Collection("G02Sql"), Trait("Group", "G02"), Trait("Task", "T06")]
public sealed class AuthIntegrationTests(G02DatabaseFixture db)
{
    [Fact]
    public async Task T06_K01_ConcurrentBootstrapAllowsExactlyOneCommit()
    {
        var connection = G02DatabaseFixture.FixtureConnection("RACE");
        var password = Convert.ToBase64String(RandomNumberGenerator.GetBytes(24));
        async Task<bool> Attempt(string username)
        {
            var factory = G02DatabaseFixture.Factory(connection, true);
            try { await new BootstrapService(new SetupRepository(factory), new AuthRepository(factory), new PasswordService()).CompleteAsync(
                new BootstrapRequest { Mode = "NewEmployee", Username = username, Password = password, Confirmation = password, EmployeeName = "Race" }); return true; }
            catch (DataAccessException ex) when (ex.Error.Number == 51401) { return false; }
        }
        var results = await Task.WhenAll(Attempt("race.a"), Attempt("race.b"));
        Assert.Single(results, x => x);
        await using var owner = await G02DatabaseFixture.OpenOwner(connection);
        Assert.Equal(1, Convert.ToInt32(await TestDatabaseFixture.ScalarAsync(owner, "SELECT COUNT(*) FROM dbo.TAIKHOAN WHERE MaVT='ADMIN'")));
        Assert.Equal("Completed", await TestDatabaseFixture.ScalarAsync(owner, "SELECT State FROM dbo.AppSetupState WHERE Id=1"));
        Assert.Equal(0, Convert.ToInt32(await TestDatabaseFixture.ScalarAsync(owner, "SELECT @@TRANCOUNT")));
    }
    [Fact]
    public async Task T06_K01_RollbackKeepsStateAndEmployeeAccountRowsRetryable()
    {
        var connection = G02DatabaseFixture.FixtureConnection("ROLLBACK");
        var factory = G02DatabaseFixture.Factory(connection, true);
        var service = new BootstrapService(new SetupRepository(factory), new AuthRepository(factory), new PasswordService());
        var password = Convert.ToBase64String(RandomNumberGenerator.GetBytes(24));
        var request = new BootstrapRequest { Mode = "NewEmployee", Username = "rollback.admin", Password = password, Confirmation = password, EmployeeName = "Rollback" };
        await using var owner = await G02DatabaseFixture.OpenOwner(connection);
        var before = await TestDatabaseFixture.SnapshotAsync(owner);
        await Assert.ThrowsAsync<DataAccessException>(() => service.CompleteAsync(request));
        Assert.Equal(before, await TestDatabaseFixture.SnapshotAsync(owner));
        await G02DatabaseFixture.Execute(owner, "DROP TRIGGER dbo.G02_FailSetupCompletion;");
        await service.CompleteAsync(request);
        Assert.Equal("Completed", (await service.ReadStateAsync()).State);
    }
    [Fact]
    public async Task T06_K02_RestartAndAdminLockDoNotReopenBootstrap()
    {
        using var session = await db.Login();
        await using var owner = await G02DatabaseFixture.OpenOwner(db.Connection);
        try
        {
            await G02DatabaseFixture.Execute(owner, "UPDATE dbo.TAIKHOAN SET HoatDong=0 WHERE MaTK=@id", new SqlParameter("@id", session.Identity.AccountId));
            var factory = G02DatabaseFixture.Factory(db.Connection, true);
            var service = new BootstrapService(new SetupRepository(factory), new AuthRepository(factory), new PasswordService());
            var error = await Assert.ThrowsAsync<DataAccessException>(() => service.CompleteAsync(new BootstrapRequest
            { Mode = "NewEmployee", Username = "second.admin", Password = db.Password, Confirmation = db.Password, EmployeeName = "Blocked" }));
            Assert.Equal(51401, error.Error.Number); Assert.Equal("Completed", (await service.ReadStateAsync()).State);
        }
        finally { await G02DatabaseFixture.Execute(owner, "UPDATE dbo.TAIKHOAN SET HoatDong=1 WHERE MaTK=@id", new SqlParameter("@id", session.Identity.AccountId)); }
    }
    [Fact]
    public async Task T06_K05_CasCannotOverwritePasswordChangedAfterRead()
    {
        var old = (await db.Auth.ReadCredentialAsync("g02.admin"))!;
        var changed = Convert.ToBase64String(RandomNumberGenerator.GetBytes(24));
        await using var owner = await G02DatabaseFixture.OpenOwner(db.Connection);
        try
        {
            await G02DatabaseFixture.Execute(owner, "UPDATE dbo.TAIKHOAN SET MatKhauHash=@hash WHERE MaTK=@id",
                new SqlParameter("@hash", new PasswordService().Hash(changed)), new SqlParameter("@id", old.Identity.AccountId));
            Assert.Null(await db.Auth.RehashAsync(old, new PasswordService().Hash(db.Password)));
            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => db.Login());
            using var current = await new AuthService(db.Auth, new PasswordService()).LoginAsync(new LoginRequest("g02.admin", changed));
            Assert.Equal(old.Identity.AccountId, current.Identity.AccountId);
        }
        finally { await G02DatabaseFixture.Execute(owner, "UPDATE dbo.TAIKHOAN SET MatKhauHash=@hash WHERE MaTK=@id", new SqlParameter("@hash", new PasswordService().Hash(db.Password)), new SqlParameter("@id", old.Identity.AccountId)); }
    }
    [Fact]
    public async Task T06_K08_OldSessionCannotOperateAfterAccountRevocation()
    {
        using var session = await db.Login();
        await using var owner = await G02DatabaseFixture.OpenOwner(db.Connection);
        try
        {
            await G02DatabaseFixture.Execute(owner, "UPDATE dbo.TAIKHOAN SET HoatDong=0 WHERE MaTK=@id", new SqlParameter("@id", session.Identity.AccountId));
            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => new PermissionService(db.Auth).DemandAsync(session, AppPermission.CatalogAdmin));
            Assert.True(session.IsRevoked);
        }
        finally { await G02DatabaseFixture.Execute(owner, "UPDATE dbo.TAIKHOAN SET HoatDong=1 WHERE MaTK=@id", new SqlParameter("@id", session.Identity.AccountId)); }
    }
}
