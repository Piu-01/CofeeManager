using CafeManager.Business.Services;
using CafeManager.Models.Requests.Inventory;
using Xunit;
namespace CafeManager.Tests.Inventory;
public sealed class StockIssueRulesTests
{
    [Theory][InlineData("Xuat ra quay")][InlineData("Khac")]
    public void BlocksKindsOutsideSingleWarehouseContract(string kind)=>Assert.Throws<ArgumentException>(()=>StockIssueService.Normalize(new(Guid.NewGuid(),DateTime.Today,kind,"Hỏng",[new("NVL01",1)])));
    [Fact] public void RequiresReason()=>Assert.Throws<ArgumentException>(()=>StockIssueService.Normalize(new(Guid.NewGuid(),DateTime.Today,"Huy do hong"," ",[new("NVL01",1)])));
}
