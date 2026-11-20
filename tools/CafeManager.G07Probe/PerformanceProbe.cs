using System.Diagnostics;
using System.Reflection;
using System.Text.Json;
using CafeManager.Business.Security;
using CafeManager.Business.Services;
using CafeManager.DataAccess;
using CafeManager.DataAccess.Repositories;
using CafeManager.Models.Dtos.Catalogs;
using CafeManager.Models.Dtos.Products;
using CafeManager.Models.Requests.Catalogs;
using CafeManager.Models.Requests.Inventory;
using CafeManager.Models.Requests.Reports;
using CafeManager.Models.Requests.Sales;
using Microsoft.Data.SqlClient;

// Đo triển khai T29 trên DB riêng; không tick ca nghiệm thu hoặc đo qua seed nhỏ.
internal static class PerformanceProbe
{
    private static readonly JsonSerializerOptions Json=new(){WriteIndented=true};
    public static async Task Run(string database,string marker,string output)
    {
        if(!System.Text.RegularExpressions.Regex.IsMatch(database,@"^CoffeeManager_Test_G07Bench_\d{8}_\d{6}_[a-f0-9]{8}$"))throw new ArgumentException("Chỉ DB benchmark G07.");
        Directory.CreateDirectory(output);var manifest=new Dictionary<string,object?>{["Kind"]="Implementation benchmark, not acceptance",["Database"]=database,["Seed"]=7,["HistoricalDateLimitation"]="Checkout owns server date; dataset generated on actual current day, not backdated90days"};
        var b=new SqlConnectionStringBuilder{DataSource="localhost",InitialCatalog=database,IntegratedSecurity=true,Encrypt=true,TrustServerCertificate=true,Pooling=false};
        try{
            await using var owner=new SqlConnection(b.ConnectionString);await owner.OpenAsync();
            if((string?)await Scalar(owner,"SELECT RunId FROM dbo.G07TestMarker")!=marker)throw new InvalidOperationException("Marker mismatch");
            await using(var setup=owner.CreateCommand()){
                setup.CommandText="EXEC dbo.sp_SetupBootstrap @Mode='NewEmployee',@Username=N'bench.admin',@NewHash=@Hash,@TenNV=N'Quản lý benchmark';CREATE USER G07Runtime WITHOUT LOGIN;ALTER ROLE CafeManagerApp ADD MEMBER G07Runtime;GRANT SHOWPLAN TO G07Runtime;IF NOT EXISTS(SELECT 1 FROM dbo.HANGTHANHVIEN WHERE DiemToiThieu=0) INSERT dbo.HANGTHANHVIEN(MaHang,DiemToiThieu,PhanTramGiam) VALUES('HT001',0,0);";
                setup.Parameters.AddWithValue("@Hash",new PasswordService().Hash(Guid.NewGuid().ToString("N")));await setup.ExecuteNonQueryAsync();
            }
            var factory=new ConnectionFactory(b.ConnectionString,120);
            typeof(ConnectionFactory).GetProperty("SessionInitializer",BindingFlags.NonPublic|BindingFlags.Instance)!.SetValue(factory,(Func<SqlConnection,CancellationToken,Task>)(async(c,t)=>{await using var command=c.CreateCommand();command.CommandText="EXECUTE AS USER='G07Runtime'";await command.ExecuteNonQueryAsync(t);}));
            var auth=new AuthRepository(factory);var permissions=new PermissionService(auth);using var session=new SessionContext(await auth.ReadIdentityAsync(1,default)??throw new InvalidOperationException("Actor missing"));
            var catalogs=new CatalogService(new CatalogRepository(factory),permissions);
            var category=await catalogs.SaveAsync(session,new ProductCategoryRequest("","Benchmark",Guid.NewGuid()),null);
            var materials=new MaterialService(new MaterialRepository(factory),permissions);var products=new ProductService(new ProductRepository(factory),permissions);var tables=new TableService(new TableRepository(factory),permissions);
            for(int i=1;i<=50;i++){var code=$"NVL{i:00}";var row=await materials.SaveAsync(session,new(code,$"Nguyên liệu benchmark {i}","g"),null,Guid.NewGuid());await materials.OpenAsync(session,new(code,1000000,"Benchmark opening"),row.Revision!,Guid.NewGuid());}
            for(int i=1;i<=200;i++)await products.SaveAsync(session,new($"SP{i:000}",$"Món benchmark {i}",category.Code,25000+i*100,"Dang ban",[new RecipeInput($"NVL{1+i%50:00}",1.0001m)]),null,Guid.NewGuid());
            await tables.SaveAsync(session,true,new("KV001","Khu benchmark",null),null,Guid.NewGuid());for(int i=1;i<=50;i++)await tables.SaveAsync(session,false,new($"B{i:0000}",$"Bàn {i}","KV001"),null,Guid.NewGuid());
            for(int i=1;i<=10;i++)await catalogs.SaveAsync(session,new SupplierRequest($"NCC{i:00}",$"NCC benchmark {i}",$"090000{i:0000}"),null);
            var customers=new CustomerService(new CustomerRepository(factory),permissions);for(int i=1;i<=100;i++)await customers.SaveAsync(session,new($"Khách benchmark {i}",null),null,Guid.NewGuid());
            var assets=new AssetService(new AssetRepository(factory),permissions);for(int i=1;i<=10;i++)await assets.WriteAsync(session,new(Guid.NewGuid(),"Create",null,null,new($"Máy benchmark {i}",DateOnly.FromDateTime(DateTime.Today).AddMonths(-12),10000001+i,36)));
            var purchase=new PurchaseService(new PurchaseRepository(factory),permissions);for(int i=1;i<=30;i++)await purchase.CreateAsync(session,new(Guid.NewGuid(),$"NCC{1+i%10:00}",Now(),DateTime.Today.AddDays(30),200,1000,[new PurchaseLine("NVL01",1,1000)]));
            var debts=new SupplierDebtService(new SupplierDebtRepository(factory),permissions);var debt=(await debts.ListAsync(session,"",null,0))[0];await debts.PayAsync(session,new(Guid.NewGuid(),debt.Code,"PT001",300,Now().AddSeconds(2)));await debts.PayAsync(session,new(Guid.NewGuid(),debt.Code,"PT002",200,Now().AddSeconds(2)));
            await new StockIssueService(new StockIssueRepository(factory),permissions).CreateAsync(session,new(Guid.NewGuid(),Now(),"Huy do hong","Benchmark",[new("NVL01",0.1m)]));
            await catalogs.SaveAsync(session,new PositionRequest("CV001","Nhân viên benchmark",0,1.5m),null);await catalogs.SaveAsync(session,new ShiftRequest("CA001","Ca benchmark",TimeSpan.FromHours(8),TimeSpan.FromHours(16)),null);
            var employees=new EmployeeService(new EmployeeRepository(factory),permissions);var accounts=new EmployeeAccountService(new EmployeeAccountRepository(factory),permissions,new PasswordService());
            var worker=await employees.SaveAsync(session,new("Nhân viên benchmark",null,"Part-time",0,35000,"CV001"),null,Guid.NewGuid());var storeWorker=await employees.SaveAsync(session,new("Kho benchmark",null,"Full-time",3000000,0,"CV001"),null,Guid.NewGuid());
            foreach(var (code,role) in new[]{(worker,"CASH"),(storeWorker,"STORE")}){var password=Guid.NewGuid().ToString("N");await accounts.WriteAsync(session,"Create",new(code,null,"bench."+role.ToLowerInvariant(),role,true),password,password,Guid.NewGuid());}
            var attendance=new AttendanceService(new AttendanceRepository(factory),permissions);var workday=DateTime.Today.AddDays(-1);await attendance.SaveAsync(session,[new(1,"Insert",worker,workday,"CA001",workday.AddHours(8),workday.AddHours(16),"Di lam",null)],Guid.NewGuid());
            var payroll=new PayrollService(new PayrollRepository(factory),new EmployeeRepository(factory),permissions);var wage=new CafeManager.Models.Requests.HumanResources.PayrollRequest(worker,workday.Month,workday.Year,8,0,0,0,0,0);var wagePreview=await payroll.PreviewAsync(session,wage);var wageId=int.Parse(await payroll.CommitAsync(session,wage,wagePreview.Token,Guid.NewGuid()));var wageRow=await payroll.GetAsync(session,wageId)??throw new InvalidOperationException("Missing wage");await Task.Delay(1200);await payroll.PayAsync(session,wageRow,Now(),Guid.NewGuid());
            // Mỗi lần tạo/chốt đều qua contract sản phẩm; không tắt trigger hay UPDATE ngày trả.
            var reports=new ReportService(new ReportRepository(factory),permissions);
            await CheckedInventoryRead();
            await GenerateBills(b.ConnectionString,session);
            await Execute(owner,File.ReadAllText(Path.Combine(FindRoot(),"database/tests/g07/performance-fixture.sql")));
            manifest["Dataset"]=(string?)await Scalar(owner,"SELECT (SELECT COUNT(*) Bills,SUM(CASE WHEN TrangThai=N'Da thanh toan' THEN 1 ELSE 0 END) Paid,SUM(CASE WHEN TrangThai=N'Chua thanh toan' THEN 1 ELSE 0 END) OpenBills,SUM(CASE WHEN TrangThai=N'Huy' THEN 1 ELSE 0 END) Cancelled,MIN(NgayThanhToan) FirstPaid,MAX(NgayThanhToan) LastPaid FROM dbo.HOADON FOR JSON PATH)");
            if(Convert.ToInt32(await Scalar(owner,"SELECT COUNT(*) FROM dbo.HOADON"))!=10000)throw new InvalidOperationException("Dataset incomplete");
            manifest["Machine"]=new{OS=Environment.OSVersion.ToString(),CPU=Environment.ProcessorCount,MemoryBytes=GC.GetGCMemoryInfo().TotalAvailableMemoryBytes,Framework=System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription};
            manifest["SQL"]=(string?)await Scalar(owner,"SELECT CONVERT(nvarchar(max),@@VERSION)");
            manifest["BaselineBusinessHash"]=await BusinessHash(owner);
            await CheckedInventoryRead();
            var sales=new SalesService(new SalesRepository(factory),permissions);var invoices=new InvoiceService(new InvoiceRepository(factory),permissions);
            var scenarios=new List<(string Name,Func<Task> Run)>();var today=DateOnly.FromDateTime(DateTime.Today);
            foreach(var kind in Enum.GetValues<ReportKind>())foreach(int days in new[]{1,30,90}){var f=new ReportFilter(kind,today.AddDays(1-days),today);scenarios.Add(($"Report{kind}_{days}d",async()=>{await reports.ReadAsync(session,f);}));}
            scenarios.Add(("Dashboard",async()=>{await reports.DashboardAsync(session);}));scenarios.Add(("POSMenu",async()=>{await sales.MenuAsync(session,"",null,0);}));scenarios.Add(("Tables",async()=>{await tables.ListAsync(session,"",0);}));
            foreach(int skip in new[]{0,4950,9950}){int offset=skip;scenarios.Add(($"InvoicePage_{offset/50+1}",async()=>{await invoices.ListAsync(session,new("Paid",today.AddDays(-89),today,Skip:offset));}));}
            var phases=new Dictionary<string,List<Timing>>();phases["baseline"]=await Measure(scenarios,Path.Combine(output,"baseline.json"));
            var writes=new Dictionary<string,List<double>>();writes["baseline"]=await MeasureWrites(sales,session,Path.Combine(output,"baseline-writes.json"));
            await Diagnostics(b.ConnectionString,session,output,"baseline");
            manifest["ReadParityBeforeCandidates"]=await ReportParity(reports,session,today);
            var candidates=new[]{new Candidate("IX_G07_HD_Paid","CREATE INDEX IX_G07_HD_Paid ON dbo.HOADON(NgayThanhToan,TrangThai) INCLUDE(TongTien,GiamGia,PhiShip,PhiDoiTac,LoaiDon,ThanhTien)","dbo.HOADON"),new Candidate("IX_G07_Receipt_OperationSource","CREATE INDEX IX_G07_Receipt_OperationSource ON dbo.G03OperationReceipt(Operation,EntityCode) INCLUDE(CompletedAt)","dbo.G03OperationReceipt")};
            var decisions=new List<object>();
            foreach(var candidate in candidates){
                var baselineName=candidate.Name+"-before";phases[baselineName]=await Measure(scenarios,Path.Combine(output,baselineName+".json"));await Diagnostics(b.ConnectionString,session,output,baselineName);
                await Execute(owner,candidate.Sql);var name=candidate.Name;
                phases[name]=await Measure(scenarios,Path.Combine(output,name+".json"));writes[name]=await MeasureWrites(sales,session,Path.Combine(output,name+"-writes.json"));await Diagnostics(b.ConnectionString,session,output,name);
                // So read parity trên cùng snapshot sau write trước/sau khi drop candidate.
                var afterParity=await ReportParity(reports,session,today);await Execute(owner,$"DROP INDEX {name} ON {candidate.Table}");var withoutParity=await ReportParity(reports,session,today);
                if(afterParity!=withoutParity)throw new InvalidOperationException("Index changed report results");
                decisions.Add(new{Name=name,Sql=candidate.Sql,Table=candidate.Table,Status="MeasuredCandidate",ReadParity=afterParity==withoutParity,Baseline=baselineName,Note="Same dataset for candidate read pair; writes append valid transactions and are separately timed"});
                manifest["Candidates"]=decisions;Save();
            }
            manifest["FinalBusinessHash"]=await BusinessHash(owner);manifest["CandidateDecisions"]=decisions;manifest["Status"]="Succeeded";Save();
            void Save()=>File.WriteAllText(Path.Combine(output,"results.json"),JsonSerializer.Serialize(manifest,Json));
            async Task CheckedInventoryRead(){
                try{await reports.ReadAsync(session,new(ReportKind.Inventory,DateOnly.FromDateTime(DateTime.Today),DateOnly.FromDateTime(DateTime.Today)));}
                catch(UserOperationException e){manifest["InventoryError"]=e.Error.Number;await using var raw=new SqlConnection(b.ConnectionString);await raw.OpenAsync();await Execute(raw,"EXECUTE AS USER='G07Runtime'");await using var cmd=raw.CreateCommand();cmd.CommandText="EXEC dbo.sp_ReportInventory @Actor=@a,@Stamp=@s,@From=@d,@To=@d";cmd.Parameters.AddWithValue("@a",session.Identity.AccountId);cmd.Parameters.Add("@s",System.Data.SqlDbType.Binary,8).Value=session.Identity.SecurityStamp;cmd.Parameters.AddWithValue("@d",DateTime.Today);
                    try{await using var reader=await cmd.ExecuteReaderAsync();do{while(await reader.ReadAsync()){} }while(await reader.NextResultAsync());}catch(SqlException sql){manifest["RawInventoryError"]=new{sql.Number,sql.Message,sql.LineNumber};}throw;
                }
            }
        }
        catch(Exception e){manifest["Status"]="Failed";manifest["Error"]=e.ToString();if(e is UserOperationException u)manifest["ErrorNumber"]=u.Error.Number;File.WriteAllText(Path.Combine(output,"results.json"),JsonSerializer.Serialize(manifest,Json));Environment.ExitCode=1;}
    }
    private static void Progress(object sender,SqlInfoMessageEventArgs e)=>Console.WriteLine(e.Message);
    private static string FindRoot(){var dir=new DirectoryInfo(AppContext.BaseDirectory);while(dir is not null&&!File.Exists(Path.Combine(dir.FullName,"CafeManager.slnx")))dir=dir.Parent;return dir?.FullName??throw new DirectoryNotFoundException("Repository root missing");}
    private static DateTime Now(){var n=DateTime.Now;return n.AddTicks(-(n.Ticks%TimeSpan.TicksPerSecond));}
    private sealed record Candidate(string Name,string Sql,string Table);
    private sealed record Timing(string Scenario,double[] Milliseconds,double Median,double P95);
    private static async Task GenerateBills(string connection,SessionContext session)
    {
        await using var c=new SqlConnection(connection);await c.OpenAsync();await Execute(c,"EXECUTE AS USER='G07Runtime'");
        await using var create=c.CreateCommand();create.CommandText="DECLARE @codes TABLE(Kind varchar(20),Code char(10));INSERT @codes EXEC dbo.sp_CapMa 'HOADON';DECLARE @code char(10)=(SELECT Code FROM @codes),@recipient nvarchar(100)=CASE WHEN @channel=N'Giao hang' THEN N'Khách benchmark' END,@phone varchar(15)=CASE WHEN @channel=N'Giao hang' THEN '0900000000' END,@address nvarchar(250)=CASE WHEN @channel=N'Giao hang' THEN N'Địa chỉ benchmark' END;EXEC dbo.sp_TaoHoaDon @Actor=@a,@Stamp=@s,@Code=@code,@RequestId=@id,@Channel=@channel,@TableCode=@table,@Customer=@customer,@Recipient=@recipient,@Phone=@phone,@Address=@address,@Ship=@ship,@Product=@product,@Quantity=1,@Note=N'Ít đường';";
        Actor(create);create.Parameters.Add("@id",System.Data.SqlDbType.UniqueIdentifier);create.Parameters.Add("@channel",System.Data.SqlDbType.NVarChar,20);create.Parameters.Add("@table",System.Data.SqlDbType.VarChar,5);create.Parameters.Add("@customer",System.Data.SqlDbType.VarChar,5);create.Parameters.Add("@ship",System.Data.SqlDbType.Decimal).Value=0m;create.Parameters.Add("@product",System.Data.SqlDbType.VarChar,5);
        await using var line=c.CreateCommand();line.CommandText="EXEC dbo.sp_SuaDongHoaDon @Actor=@a,@Stamp=@s,@Code=@code,@ExpectedRevision=@revision,@RequestId=@id,@Action='Add',@Product=@product,@Quantity=1,@Note=@note";Actor(line);CodeRevision(line);line.Parameters.Add("@id",System.Data.SqlDbType.UniqueIdentifier);line.Parameters.Add("@product",System.Data.SqlDbType.VarChar,5);line.Parameters.Add("@note",System.Data.SqlDbType.NVarChar,100);
        await using var preview=c.CreateCommand();preview.CommandText="EXEC dbo.sp_SalesCheckoutPreview @Actor=@a,@Stamp=@s,@Code=@code,@ExpectedRevision=@revision";Actor(preview);CodeRevision(preview);
        await using var checkout=c.CreateCommand();checkout.CommandText="EXEC dbo.sp_ThanhToanHoaDon @Actor=@a,@Stamp=@s,@Code=@code,@ExpectedRevision=@revision,@RequestId=@id,@ExpectedPreviewToken=@token,@ThanhToan=@payments";Actor(checkout);CodeRevision(checkout);checkout.Parameters.Add("@id",System.Data.SqlDbType.UniqueIdentifier);checkout.Parameters.Add("@token",System.Data.SqlDbType.Binary,32);checkout.Parameters.Add(new SqlParameter("@payments",System.Data.SqlDbType.Structured){TypeName="dbo.DanhSachThanhToan"});
        await using var cancel=c.CreateCommand();cancel.CommandText="EXEC dbo.sp_HuyHoaDon @Actor=@a,@Stamp=@s,@Code=@code,@ExpectedRevision=@revision,@RequestId=@id";Actor(cancel);CodeRevision(cancel);cancel.Parameters.Add("@id",System.Data.SqlDbType.UniqueIdentifier);
        for(int i=1;i<=10000;i++){
            var channel=i>8000?"Mang di":i%3==0?"Tai quan":i%3==1?"Mang di":"Giao hang";var product=$"SP{1+i%200:000}";
            create.Parameters["@id"].Value=Guid.NewGuid();create.Parameters["@channel"].Value=channel;create.Parameters["@table"].Value=channel=="Tai quan"?"B0001":DBNull.Value;create.Parameters["@customer"].Value=$"KH{1+i%100:000}";create.Parameters["@ship"].Value=channel=="Giao hang"?15000m:0m;create.Parameters["@product"].Value=product;
            var result=await WriteResult(create);
            for(int n=0;n<2;n++){line.Parameters["@code"].Value=result.Code;line.Parameters["@revision"].Value=result.Revision;line.Parameters["@id"].Value=Guid.NewGuid();line.Parameters["@product"].Value=n==0?product:$"SP{1+(i+1)%200:000}";line.Parameters["@note"].Value=n==0?"Không đá":DBNull.Value;result=await WriteResult(line);}
            if(i<=8000){preview.Parameters["@code"].Value=result.Code;preview.Parameters["@revision"].Value=result.Revision;decimal payable;byte[] token;await using(var r=await preview.ExecuteReaderAsync()){await r.ReadAsync();payable=r.GetDecimal(5);token=(byte[])r[11];}
                var payments=new System.Data.DataTable();payments.Columns.Add("MaPT",typeof(string));payments.Columns.Add("SoTien",typeof(decimal));payments.Columns.Add("MaThamChieu",typeof(string));
                if(i<=6000){payments.Rows.Add("PT001",decimal.Floor(payable/2),DBNull.Value);payments.Rows.Add("PT002",payable-decimal.Floor(payable/2),"BENCH");}else payments.Rows.Add("PT001",payable,DBNull.Value);
                checkout.Parameters["@code"].Value=result.Code;checkout.Parameters["@revision"].Value=result.Revision;checkout.Parameters["@id"].Value=Guid.NewGuid();checkout.Parameters["@token"].Value=token;checkout.Parameters["@payments"].Value=payments;await WriteResult(checkout);payments.Dispose();
            }else if(i>9000){cancel.Parameters["@code"].Value=result.Code;cancel.Parameters["@revision"].Value=result.Revision;cancel.Parameters["@id"].Value=Guid.NewGuid();await WriteResult(cancel);}
            if(i%500==0)Console.WriteLine($"Benchmark fixture: {i}/10000");
        }
        void Actor(SqlCommand cmd){cmd.CommandTimeout=120;cmd.Parameters.AddWithValue("@a",session.Identity.AccountId);cmd.Parameters.Add("@s",System.Data.SqlDbType.Binary,8).Value=session.Identity.SecurityStamp;}
        static void CodeRevision(SqlCommand cmd){cmd.Parameters.Add("@code",System.Data.SqlDbType.VarChar,10);cmd.Parameters.Add("@revision",System.Data.SqlDbType.Binary,8);}
        static async Task<(string Code,byte[] Revision)> WriteResult(SqlCommand cmd){await using var r=await cmd.ExecuteReaderAsync();if(!await r.ReadAsync())throw new InvalidOperationException("Missing write receipt");return(r.GetString(0).TrimEnd(),(byte[])r[1]);}
    }
    private static async Task<List<Timing>> Measure(List<(string Name,Func<Task> Run)> scenarios,string file)
    {
        var results=new List<Timing>();foreach(var scenario in scenarios){for(int i=0;i<3;i++)await scenario.Run();var samples=new List<double>();for(int i=0;i<20;i++){var clock=Stopwatch.StartNew();await scenario.Run();samples.Add(clock.Elapsed.TotalMilliseconds);}var sorted=samples.Order().ToArray();results.Add(new(scenario.Name,samples.ToArray(),(sorted[9]+sorted[10])/2,sorted[18]));File.WriteAllText(file,JsonSerializer.Serialize(results,Json));Console.WriteLine($"{Path.GetFileName(file)}: {scenario.Name} median={results[^1].Median:F1}ms p95={sorted[18]:F1}ms");}return results;
    }
    private static async Task<List<double>> MeasureWrites(SalesService sales,SessionContext session,string file)
    {
        var samples=new List<double>();for(int i=0;i<12;i++){
            var code=await sales.AllocateAsync(session);var result=await sales.CreateAsync(session,new(code,Guid.NewGuid(),new("Mang di"),"SP001",1,"Benchmark checkout20"));
            for(int line=1;line<20;line++)result=await sales.LineAsync(session,new(code,result.Revision,Guid.NewGuid(),"Add",Product:$"SP{1+line%200:000}",Quantity:1,Note:$"Dòng{line}"));
            var order=await sales.GetAsync(session,code)??throw new InvalidOperationException("Missing benchmark bill");var preview=await sales.PreviewAsync(session,order.Header);var request=new CheckoutRequest(code,order.Header.Revision,Guid.NewGuid(),preview.Token,[new("PT001",preview.Payable/2,null),new("PT002",preview.Payable-preview.Payable/2,"BENCH")]);
            var clock=Stopwatch.StartNew();await sales.CheckoutAsync(session,request);if(i>=2)samples.Add(clock.Elapsed.TotalMilliseconds);
        }File.WriteAllText(file,JsonSerializer.Serialize(samples,Json));return samples;
    }
    private static async Task Diagnostics(string connection,SessionContext session,string output,string phase)
    {
        await using var c=new SqlConnection(connection);await c.OpenAsync();await Execute(c,"EXECUTE AS USER='G07Runtime'");var logs=new List<string>();c.InfoMessage+=(_,e)=>logs.Add(e.Message);
        foreach(var kind in new[]{"SalesDay","Products","Inventory","Debts","CashFlow","Labor","Assets"}){
            await using var cmd=c.CreateCommand();cmd.CommandTimeout=120;cmd.CommandText="SET STATISTICS IO ON;SET STATISTICS TIME ON;SET STATISTICS XML ON;EXEC dbo.sp_ReportRead @Actor=@a,@Stamp=@s,@Kind=@k,@From=@f,@To=@t;SET STATISTICS XML OFF;SET STATISTICS TIME OFF;SET STATISTICS IO OFF";
            cmd.Parameters.AddWithValue("@a",session.Identity.AccountId);cmd.Parameters.Add("@s",System.Data.SqlDbType.Binary,8).Value=session.Identity.SecurityStamp;cmd.Parameters.AddWithValue("@k",kind);cmd.Parameters.AddWithValue("@f",DateTime.Today.AddDays(-89));cmd.Parameters.AddWithValue("@t",DateTime.Today);
            // Endpoint nội bộ không được grant runtime; public wrapper giữ cùng query body.
            var publicName=kind=="SalesDay"?"Sales":kind;
            cmd.CommandText=cmd.CommandText.Replace("sp_ReportRead @Actor=@a,@Stamp=@s,@Kind=@k","sp_Report"+publicName+" @Actor=@a,@Stamp=@s");cmd.Parameters.RemoveAt("@k");
            await using var reader=await cmd.ExecuteReaderAsync();int n=0;do{while(await reader.ReadAsync()){if(reader.FieldCount==1&&reader.GetValue(0) is string value&&value.Contains("<ShowPlanXML"))File.WriteAllText(Path.Combine(output,$"{phase}-{kind}-{n++}.sqlplan"),value);}}while(await reader.NextResultAsync());
            File.WriteAllLines(Path.Combine(output,$"{phase}-{kind}-io-time.log"),logs);logs.Clear();
        }
    }
    private static async Task<string> ReportParity(ReportService reports,SessionContext session,DateOnly today){var p=new List<object>();foreach(var k in Enum.GetValues<ReportKind>()){var r=await reports.ReadAsync(session,new(k,today.AddDays(-89),today));p.Add(new{Kind=k.ToString(),r.Totals,Rows=r.Rows.Select(x=>JsonSerializer.Serialize(x,x.GetType())).ToArray()});}return Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(p)));}
    private static async Task<string?> BusinessHash(SqlConnection c)=>Convert.ToString(await Scalar(c,"SELECT CONVERT(varchar(64),HASHBYTES('SHA2_256',CONVERT(varbinary(max),(SELECT (SELECT MaNVL,SoLuongTon FROM dbo.NGUYENVATLIEU ORDER BY MaNVL FOR JSON PATH) Stock,(SELECT MaKH,DiemTichLuy FROM dbo.KHACHHANG ORDER BY MaKH FOR JSON PATH) Points,(SELECT MaHD,TongTien,GiamGia,ThanhTien,TrangThai FROM dbo.HOADON ORDER BY MaHD FOR JSON PATH) Bills,(SELECT MaCN,TienNo FROM dbo.CONGNO_NCC ORDER BY MaCN FOR JSON PATH) Debts FOR JSON PATH))),2)"));
    private static async Task<object?> Scalar(SqlConnection c,string sql){await using var cmd=c.CreateCommand();cmd.CommandTimeout=120;cmd.CommandText=sql;return await cmd.ExecuteScalarAsync();}
    private static async Task Execute(SqlConnection c,string sql){await using var cmd=c.CreateCommand();cmd.CommandTimeout=120;cmd.CommandText=sql;await cmd.ExecuteNonQueryAsync();}
}



