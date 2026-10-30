using System.Text.Json;
using CafeManager.Business.Security;
using CafeManager.Business.Services;
using CafeManager.DataAccess;
using CafeManager.DataAccess.Repositories;
using CafeManager.Models.Requests.Inventory;
using Microsoft.Data.SqlClient;

// Smoke typed phụ thuộc trên DB demo của runner, không thay nghiệm thu55ID/native/quyền runtime.
if(args.Length!=3||!args[0].StartsWith("CoffeeManager_Test_G05_",StringComparison.Ordinal))throw new ArgumentException("Cần DB test, marker và file output.");
var builder=new SqlConnectionStringBuilder{DataSource="localhost",InitialCatalog=args[0],IntegratedSecurity=true,Encrypt=true,TrustServerCertificate=true};
var evidence=new Dictionary<string,object?>{{"Kind","Typed dependency smoke; privileged connection; not acceptance"}};
try
{
    await using var connection=new SqlConnection(builder.ConnectionString);await connection.OpenAsync();
    await using var marker=connection.CreateCommand();marker.CommandText="SELECT RunId FROM dbo.G05TestMarker";
    if((string?)await marker.ExecuteScalarAsync()!=args[1])throw new InvalidOperationException("Marker không khớp.");
    await using var setup=connection.CreateCommand();setup.CommandText="UPDATE dbo.TAIKHOAN SET HoatDong=1 WHERE MaVT='ADMIN'; SELECT MIN(MaTK) FROM dbo.TAIKHOAN WHERE MaVT='ADMIN';";
    var actor=Convert.ToInt32(await setup.ExecuteScalarAsync());
    var factory=new ConnectionFactory(builder.ConnectionString);var auth=new AuthRepository(factory);var permissions=new PermissionService(auth);
    var identity=await auth.ReadIdentityAsync(actor,default)??throw new InvalidOperationException("Thiếu actor fixture.");using var session=new SessionContext(identity);
    var finance=new FinanceService(new FinanceRepository(factory),permissions);var purchase=new PurchaseService(new PurchaseRepository(factory),permissions);
    var issues=new StockIssueService(new StockIssueRepository(factory),permissions);var stocktakes=new StocktakeService(new StocktakeRepository(factory),permissions);var debts=new SupplierDebtService(new SupplierDebtRepository(factory),permissions);
    var financeCode=await finance.CreateAsync(session,new(Guid.NewGuid(),DateTime.Now,"TC001",1234,"Technical smoke",null));
    if((await finance.GetAsync(session,financeCode))?.Amount!=1234)throw new InvalidOperationException("Finance mapping sai.");evidence["Finance"]=financeCode;
    PurchaseLine[] input=[new("NVL01",1m,10000m)];var total=await purchase.PreviewAsync(session,input);
    var purchaseCode=await purchase.CreateAsync(session,new(Guid.NewGuid(),"NCC01",DateTime.Today,null,4000,total,input));var pn=await purchase.GetAsync(session,purchaseCode);
    if(pn?.DebtCode is null||pn.Immediate!=4000||pn.Debt!=6000||pn.Lines.Count!=1)throw new InvalidOperationException("Purchase mapping sai.");evidence["Purchase"]=purchaseCode;
    var issueCode=await issues.CreateAsync(session,new(Guid.NewGuid(),DateTime.Now,"Huy do hong","Technical smoke",[new("NVL01",0.1m)]));
    if((await issues.GetAsync(session,issueCode))?.Lines.Count!=1)throw new InvalidOperationException("Issue mapping sai.");evidence["Issue"]=issueCode;
    var kk=await stocktakes.WriteAsync(session,new(Guid.NewGuid(),"StocktakeCreate",null,null,["NVL01"],[]));var snapshot=await stocktakes.GetAsync(session,kk)??throw new InvalidOperationException("Thiếu snapshot.");
    await stocktakes.WriteAsync(session,new(Guid.NewGuid(),"StocktakeSave",kk,snapshot.Revision,[],snapshot.Lines.Select(l=>new StocktakeCount(l.Code,l.Expected,null)).ToList()));
    snapshot=await stocktakes.GetAsync(session,kk)??throw new InvalidOperationException("Thiếu số đếm.");await stocktakes.WriteAsync(session,new(Guid.NewGuid(),"StocktakeCommit",kk,snapshot.Revision,[],[]));
    if((await stocktakes.GetAsync(session,kk))?.Closed!=true)throw new InvalidOperationException("Stocktake mapping sai.");evidence["Stocktake"]=kk;
    var debtExpense=await debts.PayAsync(session,new(Guid.NewGuid(),pn.DebtCode,"PT001",1000,DateTime.Now));var debt=await debts.GetAsync(session,pn.DebtCode);
    if(debt?.Remaining!=5000||debt.History.Count!=1)throw new InvalidOperationException("Debt mapping sai.");evidence["DebtExpense"]=debtExpense;
    await debts.ListAsync(session,pn.DebtCode,DateTime.Today,0);await finance.ListAsync(session,new(DateTime.Today,DateTime.Today),default);
    evidence["Status"]="Succeeded";
}
catch(Exception e){evidence["Status"]="Failed";evidence["Error"]=e.ToString();Environment.ExitCode=1;}
finally{File.WriteAllText(args[2],JsonSerializer.Serialize(evidence,new JsonSerializerOptions{WriteIndented=true}));}
