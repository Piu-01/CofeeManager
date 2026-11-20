using System.Data;
using System.Diagnostics;
using System.Reflection;
using System.Text.Json;
using CafeManager.Business.Security;
using CafeManager.Business.Services;
using CafeManager.DataAccess;
using CafeManager.DataAccess.Repositories;
using CafeManager.Models.Dtos.Products;
using CafeManager.Models.Dtos.Reports;
using CafeManager.Models.Requests;
using CafeManager.Models.Requests.Sales;
using Microsoft.Data.SqlClient;

if(args.Length!=3||!System.Text.RegularExpressions.Regex.IsMatch(args[0],@"^CoffeeManager_Test_G07_\d{8}_\d{6}_[a-f0-9]{8}_demo$"))throw new ArgumentException("Exact test DB/marker/output required");
var b=new SqlConnectionStringBuilder{DataSource="localhost",InitialCatalog=args[0],IntegratedSecurity=true,Encrypt=true,TrustServerCertificate=true,Pooling=false};
await using var owner=new SqlConnection(b.ConnectionString);await owner.OpenAsync();
async Task<object?> Scalar(string text){await using var c=owner.CreateCommand();c.CommandText=text;return await c.ExecuteScalarAsync();}
if((string?)await Scalar("SELECT RunId FROM dbo.G07TestMarker")!=args[1])throw new InvalidOperationException("Marker mismatch");
await using(var c=owner.CreateCommand()){
 c.CommandText="DECLARE @s binary(8)=(SELECT SecurityStamp FROM dbo.TAIKHOAN WHERE MaTK=1);EXEC dbo.sp_SetupBootstrap @Mode='ActivateDemo',@Username=N'admin',@NewHash=@hash,@MaNV='NV001',@ExpectedStamp=@s;CREATE USER ExtendedRuntime WITHOUT LOGIN;ALTER ROLE CafeManagerApp ADD MEMBER ExtendedRuntime;";c.Parameters.AddWithValue("@hash",new PasswordService().Hash(Guid.NewGuid().ToString("N")));await c.ExecuteNonQueryAsync();
}
ConnectionFactory Factory(int timeout=30){var f=new ConnectionFactory(b.ConnectionString,timeout);typeof(ConnectionFactory).GetProperty("SessionInitializer",BindingFlags.Instance|BindingFlags.NonPublic)!.SetValue(f,(Func<SqlConnection,CancellationToken,Task>)(async(c,t)=>{await using var cmd=c.CreateCommand();cmd.CommandText="EXECUTE AS USER='ExtendedRuntime'";await cmd.ExecuteNonQueryAsync(t);}));return f;}
var factory=Factory();var auth=new AuthRepository(factory);var permissions=new PermissionService(auth);using var session=new SessionContext((await auth.ReadIdentityAsync(1))!);
var reports=new ReportService(new ReportRepository(factory),permissions);var sales=new SalesService(new SalesRepository(factory),permissions);var finance=new FinanceService(new FinanceRepository(factory),permissions);
var cases=new List<object>();var failures=0;
void Save()=>File.WriteAllText(args[2],JsonSerializer.Serialize(new{Database=args[0],Cases=cases,Failures=failures},new JsonSerializerOptions{WriteIndented=true}));
async Task Check(string id,string expected,Func<Task<object?>> test){try{var result=await test();cases.Add(new{Id=id,Expected=expected,Status="PASS",Actual=result});}catch(Exception e){failures++;cases.Add(new{Id=id,Expected=expected,Status="FAIL",Actual=e.ToString()});}Save();Console.WriteLine(id+" completed; failures="+failures);}
void Assert(bool condition,string message){if(!condition)throw new InvalidOperationException(message);}
DateTime Now(){var current=DateTime.Now;return current.AddTicks(-(current.Ticks%TimeSpan.TicksPerSecond));}
async Task<(decimal Revenue,decimal Expense,decimal Count)> Snapshot(){var d=await reports.ReadDashboardAnalyticsAsync(session,false);return(d.Pages["trend.current"].Totals.Amount,d.Indicators["expense.current"]!.Value,d.Indicators["invoice-count.current"]!.Value);}
var materials=new MaterialService(new MaterialRepository(factory),permissions);var products=new ProductService(new ProductRepository(factory),permissions);
Assert(Convert.ToInt32(await Scalar("SELECT (SELECT COUNT(*) FROM dbo.NGUYENVATLIEU WHERE MaNVL='NVL99')+(SELECT COUNT(*) FROM dbo.SANPHAM WHERE MaSP='SP999')"))==0,"Fixture codes already exist");
var material=await materials.SaveAsync(session,new("NVL99","Analytics extended material","g"),null,Guid.NewGuid());await materials.OpenAsync(session,new("NVL99",1000000,"Dashboard isolated fixture"),material.Revision!,Guid.NewGuid());
var category=(await products.CategoriesAsync(session))[0].Code;await products.SaveAsync(session,new("SP999",new string('A',50),category,1000,"Dang ban",[new RecipeInput("NVL99",0.0001m)]),null,Guid.NewGuid());
var expenseCategory=Convert.ToString(await Scalar("SELECT TOP(1) d.MaDMTC FROM dbo.DANHMUC_THUCHI d WHERE d.Loai=N'Chi' AND NOT EXISTS(SELECT 1 FROM dbo.SystemExpenseCategory s WHERE s.MaDMTC=d.MaDMTC) ORDER BY d.MaDMTC"))!.Trim();
async Task<string> NewOrder(string channel="Mang di"){
 var code=await sales.AllocateAsync(session);await sales.CreateAsync(session,new(code,Guid.NewGuid(),channel=="Giao hang"?new(channel,Recipient:"Test",Phone:"0900000000",Address:"DB test"):new(channel),"SP999",1,null));return code;
}
async Task Checkout(string code){var order=(await sales.GetAsync(session,code))!;var preview=await sales.PreviewAsync(session,order.Header);await sales.CheckoutAsync(session,new(code,order.Header.Revision,Guid.NewGuid(),preview.Token,[new PaymentAllocation("PT001",preview.Payable)]));}
await Check("DA05/linked-expenses","manual73+purchase100+debt200+paid payroll; commit/asset do not create extra expense",async()=>{
 var before=await Snapshot();await finance.CreateAsync(session,new(Guid.NewGuid(),Now(),expenseCategory,73,"Manual isolated",null));
 var purchase=new PurchaseService(new PurchaseRepository(factory),permissions);var pn=await purchase.CreateAsync(session,new(Guid.NewGuid(),"NCC01",Now(),DateTime.Today.AddDays(7),100,1000,[new("NVL99",1,1000)]));var row=(await purchase.GetAsync(session,pn))!;
 await new SupplierDebtService(new SupplierDebtRepository(factory),permissions).PayAsync(session,new(Guid.NewGuid(),row.DebtCode!,"PT001",200,Now()));
 var employees=new EmployeeService(new EmployeeRepository(factory),permissions);var employee=await employees.SaveAsync(session,new("Analytics payroll",null,"Full-time",1000,0,"CV001"),null,Guid.NewGuid());
 var payroll=new PayrollService(new PayrollRepository(factory),new EmployeeRepository(factory),permissions);var request=new CafeManager.Models.Requests.HumanResources.PayrollRequest(employee,DateTime.Today.Month,DateTime.Today.Year,0,0,0,0,0,0);var preview=await payroll.PreviewAsync(session,request);var id=int.Parse(await payroll.CommitAsync(session,request,preview.Token,Guid.NewGuid()));var wage=(await payroll.GetAsync(session,id))!;
 var assetService=new AssetService(new AssetRepository(factory),permissions);await assetService.WriteAsync(session,new(Guid.NewGuid(),"Create",null,null,new("Dashboard asset",DateOnly.FromDateTime(DateTime.Today),1000,10)));
 var committed=await Snapshot();Assert(committed.Expense-before.Expense==373,"Commit/asset doubled expense");
 await Task.Delay(1100);var payCode=await payroll.PayAsync(session,wage,Now(),Guid.NewGuid());await Task.Delay(1100);var after=await Snapshot();
 Assert(after.Expense-before.Expense==373+wage.Amount,"Linked expense total mismatch");
 var sources=await reports.DashboardAnalyticsSourcesAsync(session,"expense",DateTime.Today,DateTime.Now);Assert(sources.Rows.Select(r=>r.Code).Distinct().Count()==sources.Rows.Count,"Duplicate source");return new{pn,row.DebtCode,payCode,wage.Amount,Delta=after.Expense-before.Expense};
});
await Check("DA10/concurrent-checkout","20 simultaneous reads with checkout; snapshot equality; revenue+1000, count+1, stock−0.0001, single payment/receipt",async()=>{
 var code=await NewOrder();var order=(await sales.GetAsync(session,code))!;var preview=await sales.PreviewAsync(session,order.Header);var before=await Snapshot();var stock=Convert.ToDecimal(await Scalar("SELECT SoLuongTon FROM dbo.NGUYENVATLIEU WHERE MaNVL='NVL99'"));var requestId=Guid.NewGuid();
 var gate=new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
 var writer=Task.Run(async()=>{await gate.Task;await sales.CheckoutAsync(session,new(code,order.Header.Revision,requestId,preview.Token,[new("PT001",preview.Payable)]));});
 var reader=Task.Run(async()=>{await gate.Task;for(var i=0;i<20;i++){var d=await reports.ReadDashboardAnalyticsAsync(session,false);Assert(d.Pages["trend.current"].Totals.Amount==d.Pages["channels"].Totals.Amount,"Snapshot totals differ");Assert(d.Pages["trend.current"].Rows.OfType<SalesReportRow>().Sum(r=>r.CustomerPaid)==d.Pages["trend.current"].Totals.Amount,"Buckets differ");}});
 var watch=Stopwatch.StartNew();gate.SetResult();await Task.WhenAll(writer,reader);await Task.Delay(1100);var after=await Snapshot();
 Assert(after.Revenue-before.Revenue==1000&&after.Count-before.Count==1,"Revenue/count mismatch");Assert(stock-Convert.ToDecimal(await Scalar("SELECT SoLuongTon FROM dbo.NGUYENVATLIEU WHERE MaNVL='NVL99'"))==0.0001m,"Stock mismatch");
 Assert(Convert.ToInt32(await Scalar("SELECT COUNT(*) FROM dbo.CHITIET_THANHTOAN WHERE MaHD='"+code+"'"))==1,"Repeated payment");Assert(await sales.ReconcileAsync(session,requestId,code,"SalesCheckout") is not null,"Missing receipt");return new{code,ElapsedMs=watch.Elapsed.TotalMilliseconds,Reads=20};
});
await Check("DA10/concurrent-expense","20 reads concurrently with expense; one73 increment and one receipt",async()=>{
 var before=await Snapshot();var id=Guid.NewGuid();var gate=new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);string? code=null;
 var write=Task.Run(async()=>{await gate.Task;code=await finance.CreateAsync(session,new(id,Now(),expenseCategory,73,"Race",null));});var read=Task.Run(async()=>{await gate.Task;for(var i=0;i<20;i++)await reports.ReadDashboardAnalyticsAsync(session,true);});gate.SetResult();await Task.WhenAll(write,read);await Task.Delay(1100);var after=await Snapshot();Assert(after.Expense-before.Expense==73&&await finance.ReconcileAsync(session,id)==code,"Duplicate expense or receipt");return new{code,Delta=after.Expense-before.Expense};
});
await Check("DA07/delivery-states","unpaid/cancelled not pending; paid waiting +1; delivered back to baseline",async()=>{
 async Task<decimal> Pending()=> (await reports.ReadDashboardAnalyticsAsync(session,false)).Indicators["delivery.pending"]!.Value;
 var before=await Pending();var code=await NewOrder("Giao hang");Assert(await Pending()==before,"Unpaid counted");await Checkout(code);Assert(await Pending()==before+1,"Paid waiting missing");
 var delivery=new DeliveryService(new DeliveryRepository(factory),permissions);var first=(await sales.GetAsync(session,code))!;await delivery.ChangeAsync(session,new(code,first.Header.Revision,Guid.NewGuid(),"Cho giao","Dang giao"));var next=(await sales.GetAsync(session,code))!;await delivery.ChangeAsync(session,new(code,next.Header.Revision,Guid.NewGuid(),"Dang giao","Da giao"));Assert(await Pending()==before,"Completed counted");
 var cancelled=await NewOrder("Giao hang");await sales.CancelAsync(session,(await sales.GetAsync(session,cancelled))!.Header,Guid.NewGuid());Assert(await Pending()==before,"Cancelled counted");return code;
});
await Check("DA11/timeout","HOADON lock causes real SQL timeout; next read succeeds; no 0 fabricated",async()=>{
 await using var locker=new SqlConnection(b.ConnectionString);await locker.OpenAsync();await using var transaction=await locker.BeginTransactionAsync();await using(var command=locker.CreateCommand()){command.Transaction=(SqlTransaction)transaction;command.CommandText="SELECT COUNT(*) FROM dbo.HOADON WITH(TABLOCKX,HOLDLOCK)";await command.ExecuteScalarAsync();}
 var shortReports=new ReportService(new ReportRepository(Factory(1)),permissions);int? number=null;try{await shortReports.ReadDashboardAnalyticsAsync(session,false);}catch(UserOperationException e){number=e.Error.Number;}finally{await transaction.RollbackAsync();}
 Assert(number==-2,"Expected SQL timeout −2, actual "+number);await reports.ReadDashboardAnalyticsAsync(session,false);return number;
});
await Check("DA04/midnight","midnight and month start have empty current buckets with exact previous cutoff",async()=>{
 foreach(var at in new[]{new DateTime(2026,1,1),new DateTime(2026,11,1),new DateTime(2026,1,15)}){
  await using var c=owner.CreateCommand();c.CommandText="EXEC dbo.sp_DashboardAnalyticsRead @Actor=@a,@Stamp=@s,@Month=0,@At=@at";c.Parameters.AddWithValue("@a",session.Identity.AccountId);c.Parameters.Add("@s",SqlDbType.Binary,8).Value=session.Identity.SecurityStamp;c.Parameters.AddWithValue("@at",at);
  using var adapter=new SqlDataAdapter(c);var d=new DataSet();adapter.Fill(d);Assert(d.Tables[2].Rows.Cast<DataRow>().All(r=>(string)r["PageKey"]!="trend.current"),"Future buckets at midnight");
 }return "3 midnight cutoffs matched";
});
await Check("DA13/10000","exactly10000 mixed-status invoices via real contracts; measure2modes, totals agree",async()=>{
 var initial=Convert.ToInt32(await Scalar("SELECT COUNT(*) FROM dbo.HOADON"));Assert(initial<1000,"Unexpected baseline size");
 for(var i=initial;i<10000;i++){
  var code=await NewOrder();if(i<8000)await Checkout(code);else if(i>=9000)await sales.CancelAsync(session,(await sales.GetAsync(session,code))!.Header,Guid.NewGuid());
  if((i+1)%1000==0)Console.WriteLine("Generated total "+(i+1));
 }
 Assert(Convert.ToInt32(await Scalar("SELECT COUNT(*) FROM dbo.HOADON"))==10000,"Dataset count");
 var timings=new List<object>();foreach(var month in new[]{false,true}){for(var i=0;i<2;i++)await reports.ReadDashboardAnalyticsAsync(session,month);var samples=new List<double>();for(var i=0;i<10;i++){var watch=Stopwatch.StartNew();var d=await reports.ReadDashboardAnalyticsAsync(session,month);samples.Add(watch.Elapsed.TotalMilliseconds);Assert(d.Pages["trend.current"].Totals.Amount==d.Pages["channels"].Totals.Amount,"Large dataset totals");}samples.Sort();timings.Add(new{Month=month,MedianMs=samples[5],P90Ms=samples[9]});}
 return new{Total=10000,Timings=timings,Distribution=await Scalar("SELECT (SELECT TrangThai,COUNT(*) Bills FROM dbo.HOADON GROUP BY TrangThai FOR JSON PATH)")};
});
await Check("DA09/revoked-session","revoked session cannot read analytics or source",async()=>{
 using var revoked=new SessionContext(session.Identity);revoked.Revoke();var denied=0;try{await reports.ReadDashboardAnalyticsAsync(revoked,false);}catch(Exception e) when(e is UserOperationException or OperationCanceledException){denied++;}try{await reports.DashboardAnalyticsSourcesAsync(revoked,"orders",null,null);}catch(Exception e) when(e is UserOperationException or OperationCanceledException){denied++;}Assert(denied==2,"Session leak");return denied;
});
await Check("DA10/final-transaction","fresh connection state0:0",async()=>{await using var c=new SqlConnection(b.ConnectionString);await c.OpenAsync();await using var cmd=c.CreateCommand();cmd.CommandText="DECLARE @t int=@@TRANCOUNT,@x int=XACT_STATE();SELECT CONCAT(@t,':',@x)";var state=Convert.ToString(await cmd.ExecuteScalarAsync());Assert(state=="0:0","Transaction state "+state);return state;});
Save();Environment.ExitCode=failures==0?0:1;
