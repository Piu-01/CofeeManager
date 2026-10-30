using CafeManager.Business.Services;
using CafeManager.Models.Requests.Inventory;
using Xunit;
namespace CafeManager.Tests.Inventory;
public sealed class StocktakeRulesTests
{
    [Fact] public void PreservesUncountedNullAndCountedZero()
    {
        var request=new StocktakeRequest(Guid.NewGuid(),"StocktakeSave","KK00000001",new byte[8],[],[new("NVL01",null,null),new("NVL02",0,"Đếm thực")]);
        StocktakeService.Validate(request);Assert.Null(request.Counts[0].Quantity);Assert.Equal(0m,request.Counts[1].Quantity);
    }
    [Fact] public void RejectsMissingRevision()=>Assert.Throws<ArgumentException>(()=>StocktakeService.Validate(new(Guid.NewGuid(),"StocktakeCommit","KK00000001",null,[],[])));
}
