using System.Data;
using CafeManager.DataAccess;
using CafeManager.Models.Errors;
using CafeManager.Models.Requests;
using Xunit;

namespace CafeManager.Tests;

[Trait("Category", "Unit")]
public sealed class ParameterAndErrorTests
{
    [Fact]
    [Trait("Case", "T05-K02")]
    public void PrecisionAndLengthAreRejectedInsteadOfTruncated()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => SqlParameters.Decimal("@money", 1.5m));
        Assert.Throws<ArgumentOutOfRangeException>(() => SqlParameters.Decimal("@money", 1000000000000000000m));
        Assert.Throws<ArgumentOutOfRangeException>(() => SqlParameters.Decimal("@qty", 1.00001m, 4));
        Assert.Throws<ArgumentException>(() => SqlParameters.Text("@code", SqlDbType.Char, 5, "ABCDEF"));
        Assert.Throws<ArgumentException>(() => SqlParameters.Text("@code", SqlDbType.Char, 5, " ", true));
        Assert.Throws<ArgumentException>(() => SqlParameters.DateTime("@date", new DateTime(2025, 4, 1).AddTicks(1)));
    }

    [Fact]
    [Trait("Case", "T05-K04")]
    public void PaymentRowsRejectInvalidAmountsAndReferences()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => PaymentTableBuilder.Build([new PaymentAllocation("PT001", 0)]));
        Assert.Throws<ArgumentOutOfRangeException>(() => PaymentTableBuilder.Build([new PaymentAllocation("PT001", -1)]));
        Assert.Throws<ArgumentOutOfRangeException>(() => PaymentTableBuilder.Build([new PaymentAllocation("PT001", 1.5m)]));
        Assert.Throws<ArgumentException>(() => PaymentTableBuilder.Build([new PaymentAllocation("PT001", 1m, new string('a', 101))]));
    }

    [Fact]
    [Trait("Case", "T05-K09")]
    public void UnknownWriteAndReadTimeoutHaveDifferentReconciliationSemantics()
    {
        Assert.Equal(DataErrorKind.Unknown, SqlErrorMapper.MapNumber(52905, true).Kind);
        Assert.True(SqlErrorMapper.MapNumber(52905, true).RequiresReconciliation);
        Assert.False(SqlErrorMapper.MapNumber(-2, false).RequiresReconciliation);
        Assert.True(SqlErrorMapper.MapNumber(-2, true).RequiresReconciliation);
        Assert.Equal(DataErrorKind.Permission, SqlErrorMapper.MapNumber(229, false).Kind);
    }

    [Theory]
    [InlineData(258, false, DataErrorKind.Connection, false, "kết nối")]
    [InlineData(258, true, DataErrorKind.Connection, true, "kết nối")]
    [InlineData(-2146893019, false, DataErrorKind.Connection, false, "chứng chỉ")]
    [InlineData(-2146893019, true, DataErrorKind.Connection, true, "chứng chỉ")]
    [InlineData(53, false, DataErrorKind.Connection, false, "kết nối")]
    [InlineData(53, true, DataErrorKind.Connection, true, "kết nối")]
    [InlineData(18456, false, DataErrorKind.Connection, false, "kết nối")]
    [InlineData(18456, true, DataErrorKind.Connection, true, "kết nối")]
    [InlineData(-2, false, DataErrorKind.Timeout, false, "quá thời gian")]
    [InlineData(-2, true, DataErrorKind.Timeout, true, "đối chiếu")]
    [InlineData(52905, false, DataErrorKind.Unknown, false, "mã hỗ trợ")]
    [InlineData(52905, true, DataErrorKind.Unknown, true, "mã hỗ trợ")]
    [InlineData(229, true, DataErrorKind.Permission, false, "quyền")]
    [InlineData(1205, true, DataErrorKind.BusinessRule, false, "rollback")]
    public void ConnectionFailuresKeepActionableMessagesAndWriteReconciliation(
        int number, bool writing, DataErrorKind kind, bool reconcile, string action)
    {
        var error = SqlErrorMapper.MapNumber(number, writing);

        Assert.Equal(number, error.Number);
        Assert.Equal(kind, error.Kind);
        Assert.Equal(reconcile, error.RequiresReconciliation);
        Assert.Contains(action, error.Message);
        Assert.False(error.Message.Contains("TrustServerCertificate", StringComparison.OrdinalIgnoreCase));
    }

    [Theory]
    [Trait("Case", "G04-POS-04")]
    [InlineData(2812, false, false)]
    [InlineData(2812, true, false)]
    [InlineData(2715, true, false)]
    [InlineData(208, false, false)]
    [InlineData(208, true, true)]
    public void MissingSchemaObjectsPointToMigrationInsteadOfUnknownResult(int number, bool writing, bool reconcile)
    {
        var error = SqlErrorMapper.MapNumber(number, writing);

        Assert.Equal(DataErrorKind.Schema, error.Kind);
        Assert.Equal(reconcile, error.RequiresReconciliation);
        Assert.Contains("migration", error.Message);
        Assert.Contains("triển khai", error.Message);
    }

    [Theory]
    [Trait("Case", "T05-K10")]
    [InlineData("master")][InlineData("tempdb")][InlineData("QuanLyCafe_ERP_V2")]
    [InlineData("CoffeeManager_Test_T05_wrong")]
    public void TestGuardRejectsNonIsolatedDatabase(string database)
        => Assert.Throws<InvalidOperationException>(() => TestDatabaseFixture.ValidateTarget(
            $"Server=localhost;Initial Catalog={database};Integrated Security=True;TrustServerCertificate=True"));
}
