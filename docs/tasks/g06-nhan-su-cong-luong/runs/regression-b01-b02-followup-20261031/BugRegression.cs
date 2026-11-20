using System.Data;
using System.Data.SqlTypes;
using System.Reflection;
using System.Globalization;
using CafeManager.Models.Dtos.HumanResources;
using CafeManager.Models.Requests.HumanResources;
using CafeManager.DataAccess.Repositories;
using Microsoft.Data.SqlClient;
sealed partial class Acceptance
{
 async Task BugRegression()
 {
  foreach(var value in new[]{0m,12000000m,99999999999m,999999999999m,999999999999999999m})
   await Case("G06-B01","EmployeeMoney"+value,"Create/update exact money; receipt present; replay once",async()=>{
    var id=Guid.NewGuid();var req=new EmployeeRequest(new string('x',50),null,"Full-time",value,value,"CV001");var code=await employees.SaveAsync(admin,req,null,id);var replay=await employees.SaveAsync(admin,req,null,id);var row=await Row(code);Require(row.MonthlyRate==value&&row.HourlyRate==value&&replay==code,"Create changed money");Require((await employees.ReceiptAsync(admin,id,"EmployeeCreate"))?.Code==code,"Create receipt missing");
    var updateId=Guid.NewGuid();await employees.SaveAsync(admin,req with{Name="Sửa biên tiền"},row,updateId);row=await Row(code);Require(row.MonthlyRate==value&&row.HourlyRate==value,"Update changed money");Require((await employees.ReceiptAsync(admin,updateId,"EmployeeUpdate"))?.Code==code,"Update receipt missing");return new{row.Code,row.MonthlyRate,row.HourlyRate};});
  await Case("G06-B01","PublicMaxNamePhone","Original valid50name/15phone/maxMoney succeeds",async()=>{var c=await employees.SaveAsync(admin,new(new string('x',50),"123456789012345","Full-time",999999999999999999m,0,"CV001"),null,Guid.NewGuid());var r=await Row(c);Require(r.Name.Length==50&&r.Phone?.Length==15&&r.MonthlyRate==999999999999999999m,"Boundary mismatch");return r.Code;});
  // Đi qua helper sản phẩm, kiểm số truyền nguyên vẹn thay vì helper của harness.
  var money=typeof(EmployeeRepository).GetMethod("Money",BindingFlags.Static|BindingFlags.NonPublic)!;
  foreach(decimal? value in new decimal?[]{null,0m,.01m,.001m,.5m,-1m,99999999.99m,999999999999999999m,1000000000000000000m})
   await Case("G06-B01","Parameter"+(value?.ToString(CultureInfo.InvariantCulture)??"Null"),"Parameter arrives exact, including invalid fractions before SQL validation",async()=>{
    await using var c=await Open(true);using var q=c.CreateCommand();q.CommandText="SELECT @v";q.Parameters.Add((SqlParameter)money.Invoke(null,new object?[]{"@v",value})!);await using var reader=await q.ExecuteReaderAsync();Require(await reader.ReadAsync(),"No parameter result");if(value is null){Require(reader.IsDBNull(0),"Null changed");return "NULL";}var actual=reader.GetSqlDecimal(0);Require((actual==new SqlDecimal(value.Value)).IsTrue,"Parameter rounded/truncated");return actual.ToString();
   });
  await Case("G06-B01","SQLRejectsFractionThroughHelper","SQL still rejects noninteger pay; no mutation",async()=>{var c=await Employee();var r=await Row(c);var id=Guid.NewGuid();var req=new EmployeeRequest(r.Name,r.Phone,r.Contract,.5m,r.HourlyRate,r.Position);await Error(()=>new EmployeeRepository(factory).WriteAsync(admin.Identity,"Update",r,req,null,id,default),53101);Require((await Row(c)).MonthlyRate==r.MonthlyRate,"Rejected request changed amount");Require(await employees.ReceiptAsync(admin,id,"EmployeeUpdate")==null,"Rejected request has receipt");return "Rejected53101; unchanged";});
  await Case("G06-B01","MaxPayrollCommitPayReplay","18-digit SQL preview/commit/pay exact; one source and receipts",async()=>{
   var c=await Employee("Full-time",999999999999999999m,0);var req=new PayrollRequest(c,8,2026,0,0,0,0);var preview=await payroll.PreviewAsync(admin,req);Require(preview.Snapshot.Amount==999999999999999999m,"Max preview wrong");var commitId=Guid.NewGuid();var code=await payroll.CommitAsync(admin,req,preview.Token,commitId);Require((await payroll.ReceiptAsync(admin,commitId,"PayrollCommit"))?.Code==code,"Commit receipt missing");var row=(await payroll.GetAsync(admin,int.Parse(code)))!;var invalidDate=row.CommittedAt.AddSeconds(-1);await Error(()=>payroll.PayAsync(admin,row,invalidDate,Guid.NewGuid()),53302,53301);await Task.Delay(2100);var id=Guid.NewGuid();var now=DateTime.Now;now=now.AddTicks(-(now.Ticks%TimeSpan.TicksPerSecond));Require(now>=row.CommittedAt,"Fixture date before commit");var expense=await payroll.PayAsync(admin,row,now,id);Require(await payroll.PayAsync(admin,row,now,id)==expense,"Pay replay mismatch");Require((await payroll.ReceiptAsync(admin,id,"PayrollPay"))?.Code==expense,"Pay receipt missing");Require(Convert.ToInt32(await Scalar("SELECT COUNT(*) FROM dbo.PHIEUTHUCHI WHERE MaBL=@i AND SoTien=999999999999999999",P("@i",row.Id)))==1,"Expense not exact/unique");Require((await Row(c)).MonthlyRate==row.Snapshot!.MonthlyRate,"Snapshot wrong");return new{Payroll=code,Expense=expense,Amount=row.Amount,CommittedAt=row.CommittedAt,PaymentDate=now,RejectedDate=invalidDate};
  });
  await Case("G06-B01","FractionalHoursPreserved","0.01regular+0.01OT exactly, SQL amount1",async()=>{
   var c=await Employee(hourly:100);var d=new DateTime(2026,8,10);await attendance.SaveAsync(admin,[new(1,"Insert",c,d,"CA001",d.AddHours(8),d.AddHours(8).AddSeconds(72),"Di lam",null)],Guid.NewGuid());var req=new PayrollRequest(c,8,2026,.01m,.01m,0,0);var p=await payroll.PreviewAsync(admin,req);Require(p.Snapshot.RegularHours==.01m&&p.Snapshot.OvertimeHours==.01m&&p.Snapshot.Amount==1m,"Hours rounded");await Commit(req);return new{p.Snapshot.RegularHours,p.Snapshot.OvertimeHours,p.Snapshot.Amount};});
  await Case("G06-B02","RealGridsFormattedValues","Both employee money columns and payroll text match vi-VN under en-US current culture",async()=>{
   var emp=await Row(full);var pay=(await payroll.ListAsync(admin,new(9,2026,full))).Single();return GridFormatting(emp,pay);
  });
 }
 object GridFormatting(EmployeeRow employee,PayrollRow payrollRow)
 {
  object? result=null;Exception? error=null;var thread=new Thread(()=>{try{
   CultureInfo.CurrentCulture=CultureInfo.GetCultureInfo("en-US");System.Windows.Forms.Application.SetHighDpiMode(System.Windows.Forms.HighDpiMode.PerMonitorV2);
   var assembly=Assembly.Load("CafeManager.WinForms");var values=new[]{0m,35000m,12000000m,999999999999999999m};var expected=new[]{"0 ₫","35.000 ₫","12.000.000 ₫","999.999.999.999.999.999 ₫"};var items=new List<object>();int dpi=0;
   foreach(var name in new[]{"EmployeesControl","PayrollControl"}){
    var type=assembly.GetType("CafeManager.WinForms.Modules.HumanResources."+name)!;object?[] args=name=="EmployeesControl"?[employees,accounts,admin,null,false]:[payroll,employees,admin,null,null];using var control=(System.Windows.Forms.Control)Activator.CreateInstance(type,BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic,null,args,null)!;
    using var host=new System.Windows.Forms.Form();host.Controls.Add(control);host.CreateControl();control.CreateControl();dpi=control.DeviceDpi;var grid=(System.Windows.Forms.DataGridView)type.BaseType!.GetField("Grid",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(control)!;grid.BindingContext=new System.Windows.Forms.BindingContext();
    for(int i=0;i<values.Length;i++){
     grid.DataSource=name=="EmployeesControl"?new List<EmployeeRow>{employee with{MonthlyRate=values[i],HourlyRate=values[i]}}:(object)new List<PayrollRow>{payrollRow with{Amount=values[i]}};
     Require(grid.Rows.Count==1,"Grid not bound");foreach(var property in name=="EmployeesControl"?new[]{"MonthlyRate","HourlyRate"}:new[]{"Amount"}){var cell=grid.Rows[0].Cells[property];Require(cell.Value is decimal d&&d==values[i],"Typed value changed");var text=Convert.ToString(cell.FormattedValue);Require(text==expected[i],$"{name}.{property}: {text} != {expected[i]}");items.Add(new{Control=name,Column=property,Raw=values[i],Text=text});}
    }
   }
   result=new{DeviceDpi=dpi,Values=items};
  }catch(Exception e){error=e;}});thread.SetApartmentState(ApartmentState.STA);thread.Start();thread.Join();if(error is not null)throw error;return result!;
 }
}



