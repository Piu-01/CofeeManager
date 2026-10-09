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
    [Trait("Case", "T05-K10")]
    [InlineData("master")][InlineData("tempdb")][InlineData("QuanLyCafe_ERP_V2")]
    [InlineData("CoffeeManager_Test_T05_wrong")]
    public void TestGuardRejectsNonIsolatedDatabase(string database)
        => Assert.Throws<InvalidOperationException>(() => TestDatabaseFixture.ValidateTarget(
            $"Server=localhost;Initial Catalog={database};Integrated Security=True;TrustServerCertificate=True"));
}
