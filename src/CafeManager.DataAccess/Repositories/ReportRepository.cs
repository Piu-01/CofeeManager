using System.Data;
using CafeManager.Models.Dtos;
using CafeManager.Models.Dtos.Reports;
using CafeManager.Models.Requests.Reports;
using Microsoft.Data.SqlClient;
namespace CafeManager.DataAccess.Repositories;

public sealed class ReportRepository(ConnectionFactory factory)
{
    private readonly SalesRepository executor=new(factory);
    public Task<ReportPage> ReadAsync(AccountIdentity a,ReportFilter f,CancellationToken t)
    {
        var procedure=f.Kind is ReportKind.SalesDay or ReportKind.SalesChannel?"sp_ReportSales":"sp_Report"+f.Kind;
        var parameters=Filter(a,f);
        if(f.Kind is ReportKind.SalesDay or ReportKind.SalesChannel)parameters=[..parameters,P("@Group",SqlDbType.VarChar,f.Kind==ReportKind.SalesDay?"Day":"Channel",10)];
        return executor.Call(procedure,parameters,async(r,ct)=>{
            if(!await r.ReadAsync(ct))throw new InvalidDataException(L.T("Thiếu metadata báo cáo."));
            var metadata=new ReportMetadata(Enum.Parse<ReportKind>(r.GetString(0)),DateOnly.FromDateTime(r.GetDateTime(1)),DateOnly.FromDateTime(r.GetDateTime(2)),r.GetDateTime(3),r.GetInt32(4),r.GetString(5),r.GetString(6),r.GetBoolean(7));
            await r.NextResultAsync(ct);if(!await r.ReadAsync(ct))throw new InvalidDataException(L.T("Thiếu tổng báo cáo."));
            var totals=new ReportTotals(M(r,0),M(r,1),M(r,2),M(r,3),M(r,4),M(r,5),M(r,6),M(r,7),M(r,8),M(r,9),M(r,10),M(r,11));
            await r.NextResultAsync(ct);return new ReportPage(metadata,totals,await SalesRepository.Rows(r,x=>Map(f.Kind,x),ct));
        },t);
    }
    public Task<List<ReportOption>> OptionsAsync(AccountIdentity a,CancellationToken t)=>executor.Call("sp_ReportOptions",CatalogRepository.Actor(a),(r,ct)=>SalesRepository.Rows(r,x=>new ReportOption(x.GetString(0),x.GetString(1).TrimEnd(),x.GetString(2)),ct),t);
    public Task<DashboardRow> DashboardAsync(AccountIdentity a,CancellationToken t)=>executor.Call("sp_ReportDashboard",CatalogRepository.Actor(a),async(r,ct)=>{if(!await r.ReadAsync(ct))throw new InvalidDataException(L.T("Thiếu tổng quan."));return new DashboardRow(DateOnly.FromDateTime(r.GetDateTime(0)),r.GetDateTime(1),M(r,2),r.GetInt32(3),M(r,4));},t);
    public async Task<(IReadOnlyDictionary<string,ReportPage> Pages,IReadOnlyDictionary<string,decimal?> Indicators,
        (DateTime From,DateTime Until,DateTime? PreviousFrom,DateTime? PreviousUntil) Period)> ReadDashboardAnalyticsAsync(AccountIdentity actor,bool month,CancellationToken token)
    {
        await EnsureAnalyticsAsync(token);
        return await executor.Call("sp_DashboardAnalyticsRead",[..CatalogRepository.Actor(actor),P("@Month",SqlDbType.Bit,month)],async(r,ct)=>
        {
            if(!await r.ReadAsync(ct))throw new InvalidDataException(L.T("Thiếu kỳ tổng quan."));
            var period=(From:r.GetDateTime(0),Until:r.GetDateTime(1),PreviousFrom:D(r,2),PreviousUntil:D(r,3));var readAt=r.GetDateTime(4);
            await r.NextResultAsync(ct);var indicators=new Dictionary<string,decimal?>();
            while(await r.ReadAsync(ct))indicators.Add(r.GetString(0),N(r,1));
            await r.NextResultAsync(ct);var rows=new Dictionary<string,List<IReportRow>>();
            foreach(var key in new[]{"trend.current","channels","products"})rows[key]=[];
            if(period.PreviousFrom.HasValue)rows["trend.previous"]=[];
            while(await r.ReadAsync(ct))
            {
                var key=r.GetString(0);if(!rows.TryGetValue(key,out var list))throw new InvalidDataException(L.T("Nhóm tổng quan không hợp lệ."));
                var code=r.GetString(1);var name=r.GetString(2);var bills=r.GetInt32(3);
                list.Add(key=="products"?new ProductReportRow(code,name,bills,M(r,4),M(r,8)):
                    new SalesReportRow(code,name,bills,M(r,5),M(r,6),M(r,10),M(r,7),M(r,8),M(r,9)));
            }
            await r.NextResultAsync(ct);var pages=new Dictionary<string,ReportPage>();
            while(await r.ReadAsync(ct))
            {
                var key=r.GetString(0);if(!rows.TryGetValue(key,out var list))continue;
                var from=key=="trend.previous"?period.PreviousFrom!.Value:period.From;
                var until=key=="trend.previous"?period.PreviousUntil!.Value:period.Until;
                var kind=key=="products"?ReportKind.Products:key=="channels"?ReportKind.SalesChannel:ReportKind.SalesDay;
                var note=key=="products"?L.T("Lượng bán từ hóa đơn đã trả; tiền món trước giảm toàn bill."):L.T("Theo dữ liệu đã ghi nhận; đã giảm và gồm phí ship, chưa đối soát đối tác.");
                var metadata=new ReportMetadata(kind,DateOnly.FromDateTime(from),DateOnly.FromDateTime(until),readAt,list.Count,
                    L.T("Ngày thanh toán hóa đơn · ")+(month?L.T("theo ngày"):L.T("theo giờ")),note,true);
                pages.Add(key,new(metadata,new ReportTotals(M(r,1),M(r,2),M(r,3),M(r,4),M(r,5),0,0,0,0,0,0,0),list));
            }
            if(!pages.ContainsKey("trend.current")||!pages.ContainsKey("channels")||!pages.ContainsKey("products"))throw new InvalidDataException(L.T("Thiếu khối dữ liệu tổng quan."));
            return ((IReadOnlyDictionary<string,ReportPage>)pages,(IReadOnlyDictionary<string,decimal?>)indicators,period);
        },token);
    }
    public async Task<ReportSourcePage> DashboardAnalyticsSourcesAsync(AccountIdentity actor,string kind,DateTime? from,DateTime? until,string? channel,string? product,int skip,CancellationToken token)
    {
        await EnsureAnalyticsAsync(token);
        return await executor.Call("sp_DashboardAnalyticsSources",[..CatalogRepository.Actor(actor),P("@Kind",SqlDbType.VarChar,kind,20),
            AnalyticsTime("@From",from),AnalyticsTime("@Until",until),P("@Channel",SqlDbType.NVarChar,channel,20),P("@Product",SqlDbType.VarChar,product,5),P("@Skip",SqlDbType.Int,skip)],async(r,ct)=>
        {
            if(!await r.ReadAsync(ct))throw new InvalidDataException(L.T("Thiếu nguồn tổng quan."));
            var count=r.GetInt32(0);var at=r.GetDateTime(1);await r.NextResultAsync(ct);
            return new ReportSourcePage(count,at,await SalesRepository.Rows(r,x=>new ReportSource(x.GetString(0),x.GetString(1).TrimEnd(),x.GetString(2),D(x,3),N(x,4),N(x,5)),ct));
        },token);
    }
    private async Task EnsureAnalyticsAsync(CancellationToken token)
    {
        if((await new AuthRepository(factory).ReadStateAsync(token)).SchemaVersion<28)
            throw new DataAccessException(new(Models.Errors.DataErrorKind.BusinessRule,53502,L.T("Cần cập nhật dữ liệu lên schema028 để mở Tổng quan ngày/tháng. Liên hệ người triển khai.")));
    }
    private static SqlParameter AnalyticsTime(string name,DateTime? value)=>new(name,SqlDbType.DateTime2){Value=value.HasValue?value.Value:DBNull.Value,Scale=7};
    public Task<ReportSourcePage> SourcesAsync(AccountIdentity a,ReportFilter f,string groupCode,int skip,CancellationToken t)=>executor.Call("sp_ReportSourceList",
        [..Filter(a,f with{Skip=skip}),P("@Kind",SqlDbType.VarChar,f.Kind.ToString(),20),P("@GroupCode",SqlDbType.NVarChar,groupCode,100)],async(r,ct)=>{
            await r.ReadAsync(ct);var count=r.GetInt32(0);var at=r.GetDateTime(1);await r.NextResultAsync(ct);return new ReportSourcePage(count,at,await SalesRepository.Rows(r,x=>new ReportSource(x.GetString(0),x.GetString(1).TrimEnd(),x.GetString(2),D(x,3),N(x,4),N(x,5)),ct));
        },t);
    private static SqlParameter P(string n,SqlDbType k,object? v,int size=0)=>AuthRepository.Parameter(n,k,v,size);
    private static SqlParameter[] Filter(AccountIdentity a,ReportFilter f)=>[..CatalogRepository.Actor(a),P("@From",SqlDbType.Date,f.From.ToDateTime(TimeOnly.MinValue)),P("@To",SqlDbType.Date,f.To.ToDateTime(TimeOnly.MinValue)),P("@Channel",SqlDbType.NVarChar,f.Channel,20),P("@Method",SqlDbType.VarChar,f.Method,5),P("@Supplier",SqlDbType.VarChar,f.Supplier,5),P("@Employee",SqlDbType.VarChar,f.Employee,5),P("@Material",SqlDbType.VarChar,f.Material,5),P("@Asset",SqlDbType.VarChar,f.Asset,5),P("@Skip",SqlDbType.Int,f.Skip)];
    private static decimal M(SqlDataReader r,int i)=>AssetRepository.Money(r,i);
    private static decimal? N(SqlDataReader r,int i)=>r.IsDBNull(i)?null:M(r,i);
    private static DateTime? D(SqlDataReader r,int i)=>r.IsDBNull(i)?null:r.GetDateTime(i);
    private static IReportRow Map(ReportKind kind,SqlDataReader r)
    {
        var code=r.GetString(0).TrimEnd();var name=r.GetString(1);
        return kind switch{
            ReportKind.SalesDay or ReportKind.SalesChannel=>new SalesReportRow(code,name,r.GetInt32(3),M(r,5),M(r,6),M(r,5)-M(r,6),M(r,7),M(r,8),M(r,9)),
            ReportKind.Products=>new ProductReportRow(code,name,r.GetInt32(3),M(r,4),M(r,8)),
            ReportKind.Payments=>new PaymentReportRow(code,name,r.GetInt32(3),M(r,8)),
            ReportKind.Debts=>new DebtReportRow(code,name,D(r,2),M(r,5),M(r,21),M(r,24)),
            ReportKind.CashFlow=>new CashFlowReportRow(code,name,M(r,10),M(r,11),M(r,8)),
            ReportKind.Labor=>new LaborReportRow(code,name,r.GetInt32(3),M(r,19),M(r,4),M(r,16),M(r,20),M(r,21),r.GetString(25)),
            ReportKind.Assets=>new AssetReportRow(code,name,D(r,2),M(r,5),M(r,11),M(r,22),M(r,23),M(r,24)),
            ReportKind.Inventory=>new InventoryReportRow(code,name,D(r,2),N(r,12),M(r,13),M(r,14),M(r,15),M(r,16),N(r,17),M(r,18),r.GetString(25)),
            _=>throw new InvalidDataException(L.T("Loại báo cáo không được hỗ trợ."))};
    }
}

