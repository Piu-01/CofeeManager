using System.Text.Json;
using CafeManager.Business.Security;
using CafeManager.Business.Services;
using CafeManager.DataAccess;
using CafeManager.DataAccess.Repositories;
using CafeManager.Models.Dtos.Finance;
using Microsoft.Data.SqlClient;

if(args.Length!=3||args[0]!="CoffeeManager_Test_G05_20261030_035847_cdac8a8e_demo"||args[1]!="20261030_035847_cdac8a8e")throw new ArgumentException("Exact fixture required");
var cs=new SqlConnectionStringBuilder{DataSource="localhost",InitialCatalog=args[0],IntegratedSecurity=true,Encrypt=true,TrustServerCertificate=true}.ConnectionString;
await using(var c=new SqlConnection(cs)){await c.OpenAsync();await using var cmd=c.CreateCommand();cmd.CommandText="SELECT RunId FROM dbo.G05TestMarker";if((string?)await cmd.ExecuteScalarAsync()!=args[1])throw new Exception("Wrong marker");}
var factory=new ConnectionFactory(cs);var auth=new AuthRepository(factory);var permission=new PermissionService(auth);
var finance=new FinanceService(new FinanceRepository(factory),permission);var purchase=new PurchaseService(new PurchaseRepository(factory),permission);var issue=new StockIssueService(new StockIssueRepository(factory),permission);var stocktake=new StocktakeService(new StocktakeRepository(factory),permission);var debt=new SupplierDebtService(new SupplierDebtRepository(factory),permission);
using var cash=new SessionContext((await auth.ReadIdentityAsync(9001003,default))!);
var results=new List<object>();
foreach(var (name,read) in new (string,Func<Task>)[]{("Finance",async()=>{await finance.ListAsync(cash,new(DateTime.Today.AddDays(-30),DateTime.Today,"",null,null,0));}),("Purchases",async()=>{await purchase.ListAsync(cash,"",0);}),("Issues",async()=>{await issue.ListAsync(cash,"",0);}),("Stocktakes",async()=>{await stocktake.ListAsync(cash,"",0);}),("Debts",async()=>{await debt.ListAsync(cash,"",null,0);})})
{
    bool denied=false;string? actual=null;
    try{await read();}catch(UserOperationException e){denied=e.Error.Number==51605;actual=e.Error.Number.ToString();}
    results.Add(new{Id="CASH.BLL."+name,Result=denied?"PASS":"FAIL",Actual=actual});if(!denied)Environment.ExitCode=1;
}
File.WriteAllText(args[2],JsonSerializer.Serialize(results,new JsonSerializerOptions{WriteIndented=true}));
