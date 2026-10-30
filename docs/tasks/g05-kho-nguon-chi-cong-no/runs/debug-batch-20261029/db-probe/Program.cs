using System.Text.Json;
using CafeManager.Business.Security;
using CafeManager.Business.Services;
using CafeManager.DataAccess;
using CafeManager.DataAccess.Repositories;
using CafeManager.Models.Requests.Inventory;
using Microsoft.Data.SqlClient;

if(args.Length!=3||!System.Text.RegularExpressions.Regex.IsMatch(args[0],@"^CoffeeManager_Test_G05_\d{8}_\d{6}_[a-f0-9]{8}_demo$"))throw new ArgumentException("Exact owned test DB, marker, output required");
var cs=new SqlConnectionStringBuilder{DataSource="localhost",InitialCatalog=args[0],IntegratedSecurity=true,Encrypt=true,TrustServerCertificate=true}.ConnectionString;
var evidence=new List<object>();
void Check(string part,bool condition,object actual){evidence.Add(new{Part=part,Result=condition?"PASS":"FAIL",Actual=actual});}
await using var connection=new SqlConnection(cs);await connection.OpenAsync();
async Task<object?> Scalar(string sql){await using var cmd=connection.CreateCommand();cmd.CommandText=sql;return await cmd.ExecuteScalarAsync();}
if((string?)await Scalar("SELECT RunId FROM dbo.G05TestMarker")!=args[1])throw new InvalidOperationException("Wrong marker");
var factory=new ConnectionFactory(cs);var auth=new AuthRepository(factory);var permissions=new PermissionService(auth);
var actor=Convert.ToInt32(await Scalar("SELECT MIN(MaTK) FROM dbo.TAIKHOAN WHERE MaVT='ADMIN' AND HoatDong=1"));
using var session=new SessionContext(await auth.ReadIdentityAsync(actor,default)??throw new InvalidOperationException("Missing actor"));
var finance=new FinanceService(new FinanceRepository(factory),permissions);var purchases=new PurchaseService(new PurchaseRepository(factory),permissions);
var issues=new StockIssueService(new StockIssueRepository(factory),permissions);var stocktakes=new StocktakeService(new StocktakeRepository(factory),permissions);var debts=new SupplierDebtService(new SupplierDebtRepository(factory),permissions);
try
{
    var pn=await purchases.CreateAsync(session,new(Guid.NewGuid(),"NCC01",DateTime.Today.AddDays(-2),null,0,10000,[new("NVL01",1,10000)]));
    var source=await purchases.GetAsync(session,pn)??throw new InvalidOperationException("Missing purchase");var debtCode=source.DebtCode!;
    var unpaid=await debts.GetAsync(session,debtCode);var list=await debts.ListAsync(session,debtCode,null,0);
    Check("B03.UnpaidGetAndList",unpaid is{Original:10000,Paid:0,Remaining:10000}&&unpaid.History.Count==0&&list.Count==1&&list[0].History.Count==0,new{debtCode,unpaid?.Original,unpaid?.Paid,unpaid?.Remaining,History=unpaid?.History.Count});
    await debts.PayAsync(session,new(Guid.NewGuid(),debtCode,"PT001",1000,DateTime.Today));
    var partial=await debts.GetAsync(session,debtCode);var past=await debts.ListAsync(session,debtCode,DateTime.Today.AddDays(-1),0);
    Check("B03.AsOfBeforeFirstPayment",past.Count==1&&past[0].Paid==0&&past[0].Remaining==10000&&past[0].History.Count==0,new{Count=past.Count,History=past.FirstOrDefault()?.History.Count,Remaining=past.FirstOrDefault()?.Remaining});
    Check("B03.PartialHistory",partial?.Remaining==9000&&partial.History.Count==1,new{partial?.Remaining,History=partial?.History.Count});
    await debts.PayAsync(session,new(Guid.NewGuid(),debtCode,"PT001",9000,DateTime.Today));var paid=await debts.GetAsync(session,debtCode);
    Check("B03.FullHistory",paid?.Remaining==0&&paid.History.Count==2,new{paid?.Remaining,History=paid?.History.Count});
    await Scalar("INSERT dbo.PHIEUKIEMKE(MaKK,NgayKK,MaNV,DaDieuChinh,CutoverKind) VALUES('KKTEST0001',CONVERT(date,SYSDATETIME()),'NV001',0,'Legacy');SELECT 1;");
    var empty=await stocktakes.GetAsync(session,"KKTEST0001");var emptyList=await stocktakes.ListAsync(session,"KKTEST0001",0);
    Check("B02.EmptyLegacyGetAndList",empty?.Lines.Count==0&&emptyList.Count==1&&emptyList[0].Lines.Count==0,new{Code="KKTEST0001",Lines=empty?.Lines.Count});
    var rejection=0;try{await stocktakes.WriteAsync(session,new(Guid.NewGuid(),"StocktakeCommit",empty!.Code,empty.Revision,[],[]));}catch(UserOperationException e){rejection=e.Error.Number;}
    Check("B02.EmptyStillCannotCommit",rejection==52902,new{SqlError=rejection});
    var kk=await stocktakes.WriteAsync(session,new(Guid.NewGuid(),"StocktakeCreate",null,null,["NVL02"],[]));var snapshot=await stocktakes.GetAsync(session,kk);
    Check("B02.UncountedRemainsNull",snapshot?.Lines.Count==1&&snapshot.Lines[0].Actual is null,new{Code=kk,Actual=snapshot?.Lines[0].Actual});
    await stocktakes.WriteAsync(session,new(Guid.NewGuid(),"StocktakeSave",kk,snapshot!.Revision,[],[new("NVL02",0,"Regression zero") ]));var counted=await stocktakes.GetAsync(session,kk);
    Check("B02.CountedZeroRemainsZero",counted?.Lines[0].Actual==0,new{Code=kk,Actual=counted?.Lines[0].Actual});
    foreach(var role in new[]{"STORE","CASH"})
    {
        var roleActor=Convert.ToInt32(await Scalar($"SELECT MIN(MaTK) FROM dbo.TAIKHOAN WHERE MaVT='{role}' AND HoatDong=1"));using var roleSession=new SessionContext((await auth.ReadIdentityAsync(roleActor,default))!);
        var error=0;try{await debts.PayAsync(roleSession,new(Guid.NewGuid(),debtCode,"PT001",1,DateTime.Today));}catch(UserOperationException e){error=e.Error.Number;}
        Check("B03.PayPermission."+role,error==51605,new{Error=error});
    }
    // Dữ liệu hợp lệ >50 dòng để regression pager thật của năm màn.
    for(var i=0;i<60;i++)
    {
        await finance.CreateAsync(session,new(Guid.NewGuid(),DateTime.Today,"TC001",1000,"G05 pager fixture "+i,null));
        await purchases.CreateAsync(session,new(Guid.NewGuid(),"NCC01",DateTime.Today,null,0,10000,[new("NVL01",1,10000)]));
        await issues.CreateAsync(session,new(Guid.NewGuid(),DateTime.Today,"Huy do hong","G05 pager fixture",[new("NVL01",0.01m)]));
        await stocktakes.WriteAsync(session,new(Guid.NewGuid(),"StocktakeCreate",null,null,["NVL02"],[]));
    }
    Check("B01.PaginationFixtures",true,new{Finance=60,Purchases=60,Issues=60,Stocktakes=60});
}
catch(Exception e){evidence.Add(new{Part="Harness",Result="FAIL",Error=e.ToString()});Environment.ExitCode=1;}
finally{File.WriteAllText(args[2],JsonSerializer.Serialize(evidence,new JsonSerializerOptions{WriteIndented=true}));}
