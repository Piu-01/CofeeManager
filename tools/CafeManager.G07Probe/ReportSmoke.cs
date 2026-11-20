using CafeManager.Business.Security;
using CafeManager.Business.Services;
using CafeManager.DataAccess;
using CafeManager.DataAccess.Repositories;
using CafeManager.Models.Requests.Reports;

internal static class ReportSmoke
{
    public static async Task Run(ConnectionFactory factory,PermissionService permissions,SessionContext session,Dictionary<string,object?> evidence,Microsoft.Data.SqlClient.SqlConnection owner)
    {
        var reports=new ReportService(new ReportRepository(factory),permissions);var results=new List<object>();
        var purchase=new PurchaseService(new PurchaseRepository(factory),permissions);
        var now=DateTime.Now;now=now.AddTicks(-(now.Ticks%TimeSpan.TicksPerSecond));
        await purchase.CreateAsync(session,new(Guid.NewGuid(),"NCC01",now,null,0,1,[new("NVL01",0.01m,100)]));
        foreach(var kind in Enum.GetValues<ReportKind>()){
            var filter=new ReportFilter(kind,new(2025,1,1),new(9999,12,31));
            CafeManager.Models.Dtos.Reports.ReportPage p;
            try{p=await reports.ReadAsync(session,filter);}
            catch(UserOperationException e){evidence["FailedReport"]=kind.ToString();evidence["ErrorNumber"]=e.Error.Number;
                await using var command=owner.CreateCommand();var endpoint=kind is ReportKind.SalesDay or ReportKind.SalesChannel?"Sales":kind.ToString();
                command.CommandText="EXECUTE AS USER='G07Runtime';EXEC dbo.sp_Report"+endpoint+" @Actor=@Actor,@Stamp=@Stamp,@From='20250101',@To='99991231';REVERT;";command.Parameters.AddWithValue("@Actor",session.Identity.AccountId);command.Parameters.Add("@Stamp",System.Data.SqlDbType.Binary,8).Value=session.Identity.SecurityStamp;
                try{await using var reader=await command.ExecuteReaderAsync();while(await reader.NextResultAsync()){} }catch(Microsoft.Data.SqlClient.SqlException raw){evidence["SqlDiagnostic"]=new{raw.Number,raw.Message,raw.LineNumber};}throw;}
            if(p.Metadata.Kind!=kind||p.Rows.Count>50)throw new InvalidOperationException("Report mapping failed: "+kind);
            if(p.Rows.Count>0){var sources=await reports.SourcesAsync(session,filter,p.Rows[0].Code);if(sources.Rows.Count>50)throw new InvalidOperationException("Source paging failed");}
            results.Add(new{Kind=kind.ToString(),p.Metadata.TotalCount,p.Totals.Amount,p.Metadata.Complete});
            try{await reports.ReadAsync(session,new(kind,DateOnly.FromDateTime(DateTime.Today),DateOnly.FromDateTime(DateTime.Today)));}
            catch(UserOperationException e){evidence["BoundedFailure"]=new{Kind=kind.ToString(),e.Error.Number};
                await using var raw=owner.CreateCommand();var endpoint=kind is ReportKind.SalesDay or ReportKind.SalesChannel?"Sales":kind.ToString();raw.CommandText="EXECUTE AS USER='G07Runtime';EXEC dbo.sp_Report"+endpoint+" @Actor=@a,@Stamp=@s,@From=@d,@To=@d;REVERT;";raw.Parameters.AddWithValue("@a",session.Identity.AccountId);raw.Parameters.Add("@s",System.Data.SqlDbType.Binary,8).Value=session.Identity.SecurityStamp;raw.Parameters.AddWithValue("@d",DateTime.Today);
                try{await using var reader=await raw.ExecuteReaderAsync();do{while(await reader.ReadAsync()){} }while(await reader.NextResultAsync());}catch(Microsoft.Data.SqlClient.SqlException x){evidence["BoundedSqlDiagnostic"]=new{x.Number,x.Message,x.LineNumber};}throw;}
        }
        var dashboard=await reports.DashboardAsync(session);var options=await reports.OptionsAsync(session);
        evidence["ReportMappings"]=results;evidence["Dashboard"]=dashboard;evidence["OptionCount"]=options.Count;
    }
}
