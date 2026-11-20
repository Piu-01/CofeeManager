using System.Data;
using System.Diagnostics;
using System.Text.Json;
using CafeManager.Business.Security;
using CafeManager.Business.Services;
using CafeManager.DataAccess;
using CafeManager.DataAccess.Repositories;
using CafeManager.Models.Dtos.Assets;
using CafeManager.Models.Requests.Assets;
using Microsoft.Data.SqlClient;

// Regression G07-B01: cùng connection sau lỗi để đối chiếu transaction; không retry tiền.
internal sealed class AssetLockRegression(ConnectionFactory factory,SqlConnection owner,PermissionService permissions,SessionContext admin,string output)
{
    private sealed record Case(string Id,string Expected,object? Actual,string Status);
    private sealed record Outcome(Guid RequestId,int Error,string? Code,string Before,string Transaction,double Milliseconds);
    private readonly List<Case> cases=[];
    private readonly AssetService assets=new(new AssetRepository(factory),permissions);
    private readonly FinanceService finance=new(new FinanceRepository(factory),permissions);
    private string category="";
    public async Task<object> Run()
    {
        try{
            category=(await finance.CategoriesAsync(admin)).First(c=>c.Type=="Chi").Code;
            foreach(var family in new[]{"delete-expense","update-delete","update-update"})for(int i=0;i<10;i++)await Check(family+"-"+(i+1),"2session qua barrier; kết quả nghiệp vụ đúng, không1205, transaction0:0, receipt/phiếu/row khớp",()=>Race(family));
            await Check("independent-asset","Giữ khóaA, ghi chi tài sảnB và khôngMaTS vẫn hoàn tất",Independent);
            await Check("lock-timeout","CùngMaTS kháccase bị52603 sau~10s; rollback/khôngreceipt, sau nhả khóa gửi lại request chưaghi được1lần",Timeout);
            await Check("replay-actor-payload","Replay đúng không ghi2lần; sai payload/actor51608; không giữ transaction",Replay);
            await Check("write-cost","30create/update/expense mỗi loại, rawsamples vàmedian/p95; tài sản cóchi vẫnxóa53402",WriteCost);
        }catch(Exception e){cases.Add(new("setup","Fixture đủ để chạy regression",e.ToString(),"BLOCKED"));}
        finally{Save();}
        return new{Executed=cases.Count,Passed=cases.Count(c=>c.Status=="PASS"),Failed=cases.Count(c=>c.Status=="FAIL"),Blocked=cases.Count(c=>c.Status=="BLOCKED"),File=Path.GetFileName(output)};
    }
    private void Save()=>File.WriteAllText(output,JsonSerializer.Serialize(cases,new JsonSerializerOptions{WriteIndented=true}));
    private async Task Check(string id,string expected,Func<Task<object>> test){try{cases.Add(new(id,expected,await test(),"PASS"));}catch(Exception e){cases.Add(new(id,expected,e.ToString(),"FAIL"));}Save();}
    private static void Assert(bool valid,string message){if(!valid)throw new InvalidOperationException(message);}
    private async Task<AssetRow> Create(){var r=await assets.WriteAsync(admin,new(Guid.NewGuid(),"Create",null,null,new("G07-B01 regression",DateOnly.FromDateTime(DateTime.Today),100,3)));return (await assets.GetAsync(admin,r.Code))!;}
    private async Task<object> Race(string family)
    {
        var row=await Create();var first=new AssetWriteRequest(Guid.NewGuid(),family=="delete-expense"?"Delete":"Update",row.Code,row.Revision,family=="delete-expense"?null:new("G07 raceA",row.PurchasedOn,201,3));
        var second=new AssetWriteRequest(Guid.NewGuid(),family=="update-delete"?"Delete":"Update",row.Code,row.Revision,family=="update-delete"?null:new("G07 raceB",row.PurchasedOn,202,3));var expenseId=Guid.NewGuid();
        using var barrier=owner.BeginTransaction();await using(var cmd=owner.CreateCommand()){cmd.Transaction=barrier;cmd.CommandText="SELECT GiaTri FROM dbo.TAISAN_CODINH WITH(XLOCK,HOLDLOCK) WHERE MaTS=@c";cmd.Parameters.AddWithValue("@c",row.Code);await cmd.ExecuteScalarAsync();}
        var one=Write(first);var two=family=="delete-expense"?Expense(expenseId,row.Code):Write(second);int waiters=0;
        try{for(int i=0;i<80;i++){await using var cmd=owner.CreateCommand();cmd.Transaction=barrier;cmd.CommandText="SELECT COUNT(*) FROM sys.dm_exec_requests WHERE database_id=DB_ID() AND session_id<>@@SPID AND wait_type LIKE 'LCK_M%'";waiters=Convert.ToInt32(await cmd.ExecuteScalarAsync());if(waiters>=2)break;await Task.Delay(100);}}finally{barrier.Rollback();}
        var result=await Task.WhenAll(one,two);var actual=await assets.GetAsync(admin,row.Code);var receipts=new[]{await Receipt(first.RequestId),await Receipt(family=="delete-expense"?expenseId:second.RequestId)};
        var linked=Convert.ToInt32(await Scalar("SELECT COUNT(*) FROM dbo.PHIEUTHUCHI WHERE MaTS=@c",row.Code));
        var evidence=new{family,row.Code,waiters,result,Actual=actual,Receipts=receipts,LinkedExpenses=linked};
        Assert(waiters>=2&&result.All(r=>r.Before=="0:0"&&r.Transaction=="0:0"),"Barrier/transaction: "+JsonSerializer.Serialize(evidence));
        if(family=="delete-expense")Assert(result[0].Error==0&&result[1].Error==52602&&actual is null&&linked==0||result[0].Error==53402&&result[1].Error==0&&actual is not null&&linked==1,"Delete/expense outcomes: "+JsonSerializer.Serialize(evidence));
        else{Assert(result.Count(r=>r.Error==0)==1&&result.Count(r=>r.Error==53403)==1,"Revision race: "+JsonSerializer.Serialize(evidence));Assert(family=="update-delete"?(result[0].Error==0?actual?.Value==201:actual is null):actual?.Value==(result[0].Error==0?201:202),"Row differs from winner");}
        for(int i=0;i<2;i++)Assert(result[i].Error==0?receipts[i] is not null:receipts[i] is null,"Receipt differs from committed outcome: "+JsonSerializer.Serialize(evidence));
        return evidence;
    }
    private async Task<object> Independent()
    {
        var a=await Create();var b=await Create();using var hold=owner.BeginTransaction();await Lock(hold,a.Code);
        try{var tasks=new[]{Expense(Guid.NewGuid(),b.Code),Expense(Guid.NewGuid(),null)};var all=Task.WhenAll(tasks);Assert(await Task.WhenAny(all,Task.Delay(5000))==all,"Other asset/noasset blocked by applockA");var results=await all;Assert(results.All(r=>r.Error==0&&r.Transaction=="0:0"),"Independent write failed");return new{Held=a.Code,Other=b.Code,results};}
        finally{hold.Rollback();}
    }
    private async Task<object> Timeout()
    {
        var row=await Create();var id=Guid.NewGuid();Outcome result;using(var hold=owner.BeginTransaction()){await Lock(hold,row.Code);try{result=await Expense(id,row.Code.ToLowerInvariant());}finally{hold.Rollback();}}
        Assert(result.Error==52603&&result.Transaction=="0:0"&&await Receipt(id) is null&&Convert.ToInt32(await Scalar("SELECT COUNT(*) FROM dbo.PHIEUTHUCHI WHERE MaTS=@c",row.Code))==0,"Timeout wrote partial result: "+JsonSerializer.Serialize(result));
        // Đã xác minh khôngreceipt và rollback, mới gửi lại đúng payload/request bằng thao tác riêng.
        var retry=await Expense(id,row.Code.ToLowerInvariant());Assert(retry.Error==0&&retry.Transaction=="0:0"&&await Receipt(id) is not null,"Reconciled request failed");return new{result,retry};
    }
    private async Task<object> Replay()
    {
        var row=await Create();var request=new AssetWriteRequest(Guid.NewGuid(),"Update",row.Code,row.Revision,new("G07 replay",row.PurchasedOn,201,3));var one=await Write(request);var two=await Write(request);var mismatch=await Write(request with{Value=request.Value! with{Value=202}});
        var employee=await new EmployeeService(new EmployeeRepository(factory),permissions).SaveAsync(admin,new("G07 admin2",null,"Part-time",0,30000,"CV002"),null,Guid.NewGuid());var password=Guid.NewGuid().ToString("N");await new EmployeeAccountService(new EmployeeAccountRepository(factory),permissions,new PasswordService()).WriteAsync(admin,"Create",new(employee,null,"g07.admin2","ADMIN",true),password,password,Guid.NewGuid());var staff=(await new EmployeeService(new EmployeeRepository(factory),permissions).GetAsync(admin,employee))!;using var other=new SessionContext((await new AuthRepository(factory).ReadIdentityAsync(staff.AccountId!.Value,default))!);var wrongActor=await Write(request,other);
        Assert(one.Error==0&&two.Error==0&&one.Code==two.Code&&mismatch.Error==51608&&wrongActor.Error==51608&&new[]{one,two,mismatch,wrongActor}.All(x=>x.Transaction=="0:0"),"Replay/actor behavior changed");var id=Guid.NewGuid();var income=await Expense(id,row.Code);var repeated=await Expense(id,row.Code);Assert(income.Error==0&&repeated.Error==0&&income.Code==repeated.Code&&Convert.ToInt32(await Scalar("SELECT COUNT(*) FROM dbo.PHIEUTHUCHI WHERE MaTS=@c",row.Code))==1,"Finance replay duplicated expense");return new{one,two,mismatch,wrongActor,income,repeated};
    }
    private async Task<object> WriteCost()
    {
        var create=new List<double>();var update=new List<double>();var expense=new List<double>();
        for(int i=0;i<33;i++){var clock=Stopwatch.StartNew();var row=await Create();if(i>=3)create.Add(clock.Elapsed.TotalMilliseconds);var changed=await Write(new(Guid.NewGuid(),"Update",row.Code,row.Revision,new(row.Name,row.PurchasedOn,200,3)));var paid=await Expense(Guid.NewGuid(),row.Code);Assert(changed.Error==0&&paid.Error==0,"Measured write failed");if(i>=3){update.Add(changed.Milliseconds);expense.Add(paid.Milliseconds);}row=(await assets.GetAsync(admin,row.Code))!;var deleted=await Write(new(Guid.NewGuid(),"Delete",row.Code,row.Revision,null));Assert(deleted.Error==53402&&deleted.Transaction=="0:0","Linked expense deleted");}
        object Stats(List<double> samples){var sorted=samples.Order().ToArray();return new{Samples=samples,Median=(sorted[14]+sorted[15])/2,P95=sorted[28]};}return new{Create=Stats(create),Update=Stats(update),Expense=Stats(expense),Note="3warm+30samples on demo fixture; not a same-dataset before/after benchmark"};
    }
    private async Task Lock(SqlTransaction transaction,string code){await using var cmd=owner.CreateCommand();cmd.Transaction=transaction;cmd.CommandText="EXEC dbo.sp_AssetLock @c";cmd.Parameters.AddWithValue("@c",code);await cmd.ExecuteNonQueryAsync();}
    private async Task<Outcome> Write(AssetWriteRequest request,SessionContext? actor=null){await using var c=await Runtime();await using var cmd=c.CreateCommand();cmd.CommandText="EXEC dbo.sp_AssetWrite @Actor=@a,@Stamp=@s,@Action=@action,@RequestId=@id,@Code=@c,@ExpectedRevision=@r,@Name=@n,@PurchasedOn=@d,@Value=@v,@Months=@m";Actor(cmd,actor??admin);cmd.Parameters.AddWithValue("@action",request.Action);cmd.Parameters.AddWithValue("@id",request.RequestId);cmd.Parameters.AddWithValue("@c",(object?)request.Code??DBNull.Value);cmd.Parameters.Add("@r",SqlDbType.Binary,8).Value=(object?)request.ExpectedRevision??DBNull.Value;cmd.Parameters.AddWithValue("@n",(object?)request.Value?.Name??DBNull.Value);cmd.Parameters.Add("@d",SqlDbType.Date).Value=(object?)request.Value?.PurchasedOn.ToDateTime(TimeOnly.MinValue)??DBNull.Value;cmd.Parameters.Add(new SqlParameter("@v",SqlDbType.Decimal){Precision=38,Scale=18,Value=(object?)request.Value?.Value??DBNull.Value});cmd.Parameters.AddWithValue("@m",(object?)request.Value?.Months??DBNull.Value);return await Call(c,cmd,request.RequestId);}
    private async Task<Outcome> Expense(Guid request,string? asset){await using var c=await Runtime();await using var cmd=c.CreateCommand();cmd.CommandText="EXEC dbo.sp_FinanceCreate @Actor=@a,@Stamp=@s,@RequestId=@id,@Date='2026-10-31T12:00:00',@Category=@cat,@Amount=7,@Note=N'G07-B01',@Asset=@asset";Actor(cmd,admin);cmd.Parameters.AddWithValue("@id",request);cmd.Parameters.AddWithValue("@cat",category);cmd.Parameters.AddWithValue("@asset",(object?)asset??DBNull.Value);return await Call(c,cmd,request);}
    private static async Task<Outcome> Call(SqlConnection c,SqlCommand cmd,Guid id){var before=await State(c);var clock=Stopwatch.StartNew();int error=0;string? code=null;try{await using var r=await cmd.ExecuteReaderAsync();if(await r.ReadAsync())code=r.GetString(0).TrimEnd();}catch(SqlException e){error=e.Number;}clock.Stop();return new(id,error,code,before,await State(c),clock.Elapsed.TotalMilliseconds);}
    private static async Task<string> State(SqlConnection c){await using var cmd=c.CreateCommand();cmd.CommandText="DECLARE @tc int=@@TRANCOUNT,@xs int=XACT_STATE();SELECT CONCAT(@tc,':',@xs)";return (string)(await cmd.ExecuteScalarAsync())!;}
    private async Task<SqlConnection> Runtime(){var c=new SqlConnection(new SqlConnectionStringBuilder(owner.ConnectionString){Pooling=false}.ConnectionString);await c.OpenAsync();await using var cmd=c.CreateCommand();cmd.CommandText="EXECUTE AS USER='G07Runtime'";await cmd.ExecuteNonQueryAsync();return c;}
    private static void Actor(SqlCommand cmd,SessionContext actor){cmd.Parameters.AddWithValue("@a",actor.Identity.AccountId);cmd.Parameters.Add("@s",SqlDbType.Binary,8).Value=actor.Identity.SecurityStamp;}
    private async Task<string?> Receipt(Guid id){await using var cmd=owner.CreateCommand();cmd.CommandText="SELECT EntityCode FROM dbo.G03OperationReceipt WHERE RequestId=@id";cmd.Parameters.AddWithValue("@id",id);return (await cmd.ExecuteScalarAsync()) as string;}
    private async Task<object?> Scalar(string sql,string code){await using var cmd=owner.CreateCommand();cmd.CommandText=sql;cmd.Parameters.AddWithValue("@c",code);return await cmd.ExecuteScalarAsync();}
}
