using CafeManager.Business.Security;
using Microsoft.AspNetCore.Identity;
using Xunit;

namespace CafeManager.Tests.Auth;

[Trait("Group", "G02")]
[Trait("Task", "T06")]
public sealed class AuthRulesTests
{
    [Theory]
    [InlineData(" a.B_1- ", "a.B_1-")]
    [InlineData("x", "x")]
    public void UsernameNormalizationPreservesDisplayCase(string input, string expected) => Assert.Equal(expected, PasswordService.Username(input));
    [Theory]
    [InlineData("")][InlineData(" ")][InlineData("a b")][InlineData("a\tb")][InlineData("nguyễn")]
    public void InvalidUsernamesAreRejected(string input) => Assert.Throws<ArgumentException>(() => PasswordService.Username(input));
    [Fact]
    public void HashVerificationPreservesPasswordSpacesAndRejectsPlaceholder()
    {
        var service = new PasswordService(); var hash = service.Hash(" password with spaces ");
        Assert.Equal(PasswordVerificationResult.Success, service.Verify(hash, " password with spaces "));
        Assert.Equal(PasswordVerificationResult.Failed, service.Verify(hash, "password with spaces"));
        Assert.Equal(PasswordVerificationResult.Failed, service.Verify("!DISABLED_REPLACE_WITH_APP_PASSWORD_HASH!", "password"));
    }
    [Theory]
    [InlineData("ADMIN", AppPermission.CatalogAdmin, true)]
    [InlineData("CASH", AppPermission.Finance, false)]
    [InlineData("STORE", AppPermission.Sales, false)]
    [InlineData("STORE", AppPermission.DebtRead, true)]
    [InlineData("CASH", AppPermission.Customers, true)]
    [InlineData("unknown", AppPermission.SystemInfo, false)]
    public void PermissionMatrixIsIndependentOfMenu(string role, AppPermission permission, bool expected) => Assert.Equal(expected, PermissionService.Allows(role, permission));
}
