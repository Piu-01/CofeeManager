using System.Data;
using CafeManager.Business.Security;
using CafeManager.Business.Services;
using CafeManager.DataAccess;
using CafeManager.DataAccess.Repositories;
using CafeManager.Models.Dtos.Sales;
using CafeManager.Models.Requests.Inventory;
using CafeManager.Models.Requests.Sales;
using Microsoft.Data.SqlClient;

internal static class RaceProbe
{
    internal sealed record Result(string Id,string Expected,string Actual,string Status);
    private sealed record Outcome(bool Success,int Error);
    public static async Task Run(ConnectionFactory factory,SqlConnection owner,PermissionService permissions,SessionContext session,Action<Result> record)
    {
        var sales=new SalesService(new SalesRepository(factory),permissions);var materials=new MaterialService(new MaterialRepository(factory),permissions);var purchases=new PurchaseService(new PurchaseRepository(factory),permissions);var issues=new StockIssueService(new StockIssueRepository(factory),permissions);
        for(int family=0;family<3;family++)for(int iteration=0;iteration<10;iteration++){
            var name=new[]{"checkout-checkout","checkout-purchase","checkout-issue"}[family];var id=$"T29-K09.{name}-{iteration+1}";string actual="";
            try{
                decimal initial=family==1?0:1;var current=(await materials.GetAsync(session,"NVL98"))!.Quantity;
                if(current<initial)await Buy(initial-current);else if(current>initial)await Issue(current-initial);
                var first=await Prepare();var second=family==0?await Prepare():null;
                using var hold=owner.BeginTransaction();await using(var lockRow=owner.CreateCommand()){lockRow.Transaction=hold;lockRow.CommandText="SELECT SoLuongTon FROM dbo.NGUYENVATLIEU WITH(UPDLOCK,HOLDLOCK) WHERE MaNVL='NVL98'";await lockRow.ExecuteScalarAsync();}
                var one=Capture(()=>Checkout(first));var two=Capture(family==0?()=>Checkout(second!):family==1?()=>Buy(1):()=>Issue(1));int waiters=0;Exception? observationError=null;
                try{for(int attempt=0;attempt<80;attempt++){await using var observe=owner.CreateCommand();observe.Transaction=hold;observe.CommandText="SELECT COUNT(*) FROM sys.dm_exec_requests WHERE database_id=DB_ID() AND session_id<>@@SPID AND wait_type LIKE 'LCK_M%';";waiters=Convert.ToInt32(await observe.ExecuteScalarAsync());if(waiters>=2)break;await Task.Delay(100);}}
                catch(Exception e){observationError=e;}
                finally{hold.Rollback();}
                var outcomes=await Task.WhenAll(one,two);var after=(await materials.GetAsync(session,"NVL98"))!.Quantity;
                var expected=family==1?initial+1-(outcomes[0].Success?1:0):initial-outcomes.Count(o=>o.Success);
                actual=System.Text.Json.JsonSerializer.Serialize(new{waiters,Before=initial,After=after,Expected=expected,Outcomes=outcomes});
                if(observationError is not null)throw new InvalidOperationException("Không quan sát được barrier",observationError);
                if(waiters<2||after!=expected||after<0||family!=1&&outcomes.Count(o=>o.Success)!=1||family==1&&!outcomes[1].Success)throw new InvalidOperationException("Barrier hoặc tồn/atomicity không khớp: "+actual);
                foreach(var outcome in outcomes.Where(o=>!o.Success))if(outcome.Error is not(52203 or 52803))throw new InvalidOperationException("Lỗi cạnh tranh ngoài expected: "+actual);
                record(new(id,"Hai session chờ barrier; không âm tồn, không hai lần trừ/paid; lỗi thiếu tồn rollback",actual,"PASS"));
            }catch(Exception e){record(new(id,"Hai session chờ barrier; tồn/transaction đúng",actual+"\n"+e,"FAIL"));}
        }
        async Task<CheckoutRequest> Prepare(){var code=await sales.AllocateAsync(session);await sales.CreateAsync(session,new(code,Guid.NewGuid(),new("Mang di"),"SP901",1,"RaceG07"));var order=(await sales.GetAsync(session,code))!;var preview=await sales.PreviewAsync(session,order.Header);return new(code,order.Header.Revision,Guid.NewGuid(),preview.Token,[new("PT001",preview.Payable,null)]);}
        async Task Checkout(CheckoutRequest r){await sales.CheckoutAsync(session,r);}
        async Task Buy(decimal quantity){var now=DateTime.Now;now=now.AddTicks(-(now.Ticks%TimeSpan.TicksPerSecond));await purchases.CreateAsync(session,new(Guid.NewGuid(),"NCC01",now,null,0,quantity*10,[new PurchaseLine("NVL98",quantity,10)]));}
        async Task Issue(decimal quantity){var now=DateTime.Now;now=now.AddTicks(-(now.Ticks%TimeSpan.TicksPerSecond));await issues.CreateAsync(session,new(Guid.NewGuid(),now,"Huy do hong","RaceG07",[new("NVL98",quantity)]));}
        static async Task<Outcome> Capture(Func<Task> action){try{await action();return new(true,0);}catch(UserOperationException e){return new(false,e.Error.Number);}catch(Exception){return new(false,-1);}}
    }
}
