using CafeManager.Business.Security;
using CafeManager.Business.Services;
using CafeManager.Models.Requests.HumanResources;
using Xunit;
namespace CafeManager.Tests.HumanResources;

public sealed class HumanResourcesRulesTests
{
    [Theory]
    [InlineData("ADMIN",true)]
    [InlineData("CASH",false)]
    [InlineData("STORE",false)]
    public void HumanResourcesPermissionsAreIndependentOfJobPosition(string role,bool expected)
    {
        foreach(var permission in new[]{AppPermission.EmployeeManage,AppPermission.AttendanceManage,AppPermission.PayrollManage}) Assert.Equal(expected,PermissionService.Allows(role,permission));
    }
    [Fact]
    public void OptionalEmployeePhoneIsNullWithoutChangingSalary()
    {
        var r=EmployeeService.Normalize(new("  Nguyễn Minh  ","  ","Part-time",1000,35000,"CV002"));
        Assert.Equal("Nguyễn Minh",r.Name);Assert.Null(r.Phone);Assert.Equal(1000,r.MonthlyRate);Assert.Equal(35000,r.HourlyRate);
    }
    [Theory]
    [InlineData("-1")]
    [InlineData("0.01")]
    [InlineData("1000000000000000000")]
    public void EmployeeSalaryCannotSilentlyRoundOrOverflow(string amount)
    {
        var value=decimal.Parse(amount,global::System.Globalization.CultureInfo.InvariantCulture);
        Assert.Throws<ArgumentException>(()=>EmployeeService.Normalize(new("Tên",null,"Full-time",value,0,"CV001")));
    }
    [Fact]
    public void OvernightAttendanceUsesWorkDateAndAllowsIncompleteRow()
    {
        var day=new DateTime(2026,10,31);
        AttendanceService.Validate([new(1,"Insert","NV002",day,"CA001",day.AddHours(22),day.AddDays(1).AddHours(6),"Di lam",null)]);
        AttendanceService.Validate([new(1,"Insert","NV002",day,"CA001",day.AddHours(22),null,"Di lam",null)]);
    }
    [Fact]
    public void InvalidAttendanceBatchIsRejectedBeforeSending()
    {
        var day=new DateTime(2026,10,31);var line=new AttendanceLine(1,"Insert","NV002",day,"CA001",day.AddHours(8),day.AddHours(16),"Di lam",null);
        Assert.Throws<ArgumentException>(()=>AttendanceService.Validate([line,line with{RowNo=2}]));
        Assert.Throws<ArgumentException>(()=>AttendanceService.Validate([line with{Status="Vang"}]));
        Assert.Throws<ArgumentException>(()=>AttendanceService.Validate([line with{ClockIn=null}]));
        Assert.Throws<ArgumentException>(()=>AttendanceService.Validate([line with{ClockIn=day.AddHours(8).AddTicks(1)}]));
        Assert.Throws<ArgumentException>(()=>AttendanceService.Validate([line with{Action="Update"}]));
    }
    [Theory]
    [InlineData(0,2026)]
    [InlineData(13,2026)]
    [InlineData(1,1999)]
    [InlineData(1,10000)]
    public void PayrollPeriodMustBeRepresentable(int month,int year) => Assert.Throws<ArgumentException>(()=>PayrollService.Validate(new("NV002",month,year)));
    [Fact]
    public void PayrollRejectsFractionalMoneyAndExcessHourPrecision()
    {
        Assert.Throws<ArgumentException>(()=>PayrollService.Validate(new("NV002",10,2026,OverTimePay:0.1m)));
        Assert.Throws<ArgumentException>(()=>PayrollService.Validate(new("NV002",10,2026,Regular:0.001m)));
    }
}
