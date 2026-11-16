using CafeManager.Business.Security;
using CafeManager.DataAccess.Repositories;
using CafeManager.Models.Dtos.Reports;
using CafeManager.Models.Requests.Reports;
namespace CafeManager.Business.Services;

public sealed class ReportService(ReportRepository repository,PermissionService permissions)
{
    public static ReportFilter Normalize(ReportFilter f)
    {
        if(!Enum.IsDefined(f.Kind)||f.From>f.To||f.Skip<0)throw new ArgumentException(L.T("Loại báo cáo, kỳ hoặc trang không hợp lệ."));
        string? Code(string? v){v=string.IsNullOrWhiteSpace(v)?null:v.Trim();if(v is not null&&(v.Length!=5||v.Any(c=>!char.IsAsciiLetterOrDigit(c))))throw new ArgumentException(L.T("Mã bộ lọc không hợp lệ."));return v;}
        f=f with{Channel=string.IsNullOrWhiteSpace(f.Channel)?null:f.Channel.Trim(),Method=Code(f.Method),Supplier=Code(f.Supplier),Employee=Code(f.Employee),Material=Code(f.Material),Asset=Code(f.Asset)};
        if(f.Channel is not null&&(f.Kind is not(ReportKind.SalesDay or ReportKind.SalesChannel or ReportKind.Products or ReportKind.Payments)||f.Channel is not("Tai quan" or "Mang di" or "Giao hang"))
          ||f.Method is not null&&f.Kind!=ReportKind.Payments||f.Supplier is not null&&f.Kind!=ReportKind.Debts||f.Employee is not null&&f.Kind!=ReportKind.Labor||f.Material is not null&&f.Kind!=ReportKind.Inventory||f.Asset is not null&&f.Kind!=ReportKind.Assets)throw new ArgumentException(L.T("Bộ lọc không áp dụng cho loại báo cáo này."));
        return f;
    }
    private Task<T> Run<T>(SessionContext s,Func<CancellationToken,Task<T>> action,CancellationToken t)=>HumanResourcesAccess.Run(permissions,s,AppPermission.FinancialReports,action,t);
    public Task<ReportPage> ReadAsync(SessionContext s,ReportFilter f,CancellationToken t=default)=>Run(s,ct=>repository.ReadAsync(s.Identity,Normalize(f),ct),t);
    public Task<DashboardRow> DashboardAsync(SessionContext s,CancellationToken t=default)=>Run(s,ct=>repository.DashboardAsync(s.Identity,ct),t);
    public Task<(IReadOnlyDictionary<string,ReportPage> Pages,IReadOnlyDictionary<string,decimal?> Indicators,
        (DateTime From,DateTime Until,DateTime? PreviousFrom,DateTime? PreviousUntil) Period)> ReadDashboardAnalyticsAsync(SessionContext session,bool month,CancellationToken token=default)
        =>Run(session,ct=>repository.ReadDashboardAnalyticsAsync(session.Identity,month,ct),token);
    public Task<ReportSourcePage> DashboardAnalyticsSourcesAsync(SessionContext session,string kind,DateTime? from,DateTime? until,string? channel=null,string? product=null,int skip=0,CancellationToken token=default)
    {
        if(kind is not("revenue" or "invoices" or "average" or "trend" or "channel" or "product" or "expense" or "orders" or "delivery" or "material" or "debt")||skip<0)
            throw new ArgumentException(L.T("Nguồn tổng quan không hợp lệ."));
        if(kind is not("orders" or "delivery" or "material" or "debt")&&(from is null||until is null||from>until))
            throw new ArgumentException(L.T("Khoảng thời gian nguồn không hợp lệ."));
        if(channel is not null&&(kind!="channel"||channel is not("Tai quan" or "Mang di" or "Giao hang"))||kind=="channel"&&channel is null)
            throw new ArgumentException(L.T("Kênh bán không hợp lệ."));
        if(product is not null&&(kind!="product"||product.Length!=5||product.Any(c=>!char.IsAsciiLetterOrDigit(c)))||kind=="product"&&product is null)
            throw new ArgumentException(L.T("Món nguồn không hợp lệ."));
        return Run(session,ct=>repository.DashboardAnalyticsSourcesAsync(session.Identity,kind,from,until,channel,product,skip,ct),token);
    }
    public Task<List<ReportOption>> OptionsAsync(SessionContext s,CancellationToken t=default)=>Run(s,ct=>repository.OptionsAsync(s.Identity,ct),t);
    public Task<ReportSourcePage> SourcesAsync(SessionContext s,ReportFilter f,string groupCode,int skip=0,CancellationToken t=default)
    {
        if(groupCode.Length is <1 or >100||skip<0)throw new ArgumentException(L.T("Nguồn báo cáo không hợp lệ."));
        return Run(s,ct=>repository.SourcesAsync(s.Identity,Normalize(f),groupCode,skip,ct),t);
    }
}
