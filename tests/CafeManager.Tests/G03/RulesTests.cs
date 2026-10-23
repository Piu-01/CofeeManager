using CafeManager.Business.Security;
using CafeManager.Business.Services;
using CafeManager.Models.Dtos.Loyalty;
using CafeManager.Models.Requests.Customers;
using CafeManager.Models.Requests.Loyalty;
using Xunit;
namespace CafeManager.Tests.G03;
public sealed class RulesTests
{
    [Theory][InlineData("CASH",true,false,false)][InlineData("STORE",true,true,false)][InlineData("ADMIN",true,true,true)]
    public void MaterialPermissionsAreSeparated(string role,bool read,bool manage,bool open)
    {Assert.Equal(read,PermissionService.Allows(role,AppPermission.MaterialRead));Assert.Equal(manage,PermissionService.Allows(role,AppPermission.MaterialManage));Assert.Equal(open,PermissionService.Allows(role,AppPermission.MaterialOpen));}
    [Theory][InlineData("")][InlineData("   ")]
    public void BlankPhoneIsNull(string phone)=>Assert.Null(CustomerService.Normalize(new CustomerRequest("Khách",phone)).Phone);
    [Fact]public void PhoneIsNotNumber()=>Assert.Equal("090001",CustomerService.Normalize(new CustomerRequest("Khách"," 090001 ")).Phone);
    [Theory][InlineData("090a")][InlineData("1234567890123456")]
    public void RejectsInvalidPhone(string phone)=>Assert.Throws<ArgumentException>(()=>CustomerService.Normalize(new CustomerRequest("Khách",phone)));
    [Fact]public void RejectsTierPolicyWithoutZero()=>Assert.Throws<ArgumentException>(()=>LoyaltyService.NormalizeTiers([new TierInput("BAC",1,3)]));
    [Fact]public void RejectsDuplicateThreshold()=>Assert.Throws<ArgumentException>(()=>LoyaltyService.NormalizeTiers([new TierInput("DONG",0,0),new TierInput("BAC",0,3)]));
    [Fact]public void SameDayPromotionIsValid(){var date=new DateOnly(2026,10,18);var r=LoyaltyService.Normalize(new PromotionRequest("km12345678"," Ưu đãi ",date,date,100));Assert.Equal("KM12345678",r.Code);}
    [Fact]public void LegacyPromotionCodeCanBeEditedButNotCreated()
    {
        var date=new DateOnly(2026,10,18);var request=new PromotionRequest("KM24BF","Legacy",date,date,30);
        Assert.Equal("KM24BF",LoyaltyService.Normalize(request,false).Code);
        Assert.Throws<ArgumentException>(()=>LoyaltyService.Normalize(request,true));
    }
    [Theory][InlineData("0.5")][InlineData("-1")][InlineData("1000000000000000000")]
    public void MoneyIsWholeVnd(string text)=>Assert.Throws<ArgumentException>(()=>LoyaltyService.Money(decimal.Parse(text,global::System.Globalization.CultureInfo.InvariantCulture)));
    [Fact]public void ReasonIsRequired()=>Assert.Throws<ArgumentException>(()=>LoyaltyService.Normalize(new PointAdjustmentRequest("KH001","Cong",1," ")));
}

