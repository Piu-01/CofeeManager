using CafeManager.Business.Security;
using CafeManager.Business.Services;
using CafeManager.Models.Dtos.Reports;
using CafeManager.Models.Requests.Reports;
using CafeManager.WinForms.Forms;
using CafeManager.WinForms.UI;
namespace CafeManager.WinForms.Modules.Reports;

internal sealed class ReportSourceDialog : OrbitDialogForm
{
    public ReportSourceDialog(ReportService service,SessionContext session,ReportFilter filter,string code,ReportSourceNavigation navigation)
    {
        Text=L.T("Chứng từ nguồn — ")+code;AutoScaleDimensions=new(96,96);AutoScaleMode=AutoScaleMode.Dpi;ClientSize=new(900,580);Controls.Add(new Sources(service,session,filter,code,navigation){Dock=DockStyle.Fill});
    }
    public ReportSourceDialog(SessionContext session,string caption,string description,Func<int,CancellationToken,Task<ReportSourcePage>> loader,ReportSourceNavigation navigation)
    {
        Text=caption;AutoScaleDimensions=new(96,96);AutoScaleMode=AutoScaleMode.Dpi;ClientSize=new(900,580);
        Controls.Add(new Sources(session,caption,description,loader,navigation){Dock=DockStyle.Fill});
    }
    private sealed class Sources : OrbitRecordListControl
    {
        private readonly bool dashboard;
        protected override bool IsActionAvailable(string command)=>base.IsActionAvailable(command)&&(!dashboard||Selected is ReportSource {SourceType: not "Material"});
        public Sources(SessionContext session,string caption,string description,Func<int,CancellationToken,Task<ReportSourcePage>> loader,ReportSourceNavigation navigation)
        {
            dashboard=true;
            Configure(caption,session,async(_,skip,t)=>{var p=await loader(skip,t);if(!IsDisposed&&!session.IsRevoked)Workspace.Description.Text=description+L.T("\r\nĐọc lại lúc ")+p.ReadAt.ToString("HH:mm:ss · dd/MM/yyyy",global::System.Globalization.CultureInfo.GetCultureInfo("vi-VN"))+L.T(" theo SQL Server.");return new OrbitPageRows<ReportSource>(p.Rows,p.TotalCount);},
                ("SourceType",L.T("Nguồn")),("Code",L.T("Mã")),("Name",L.T("Nội dung")),("SourceDate",L.T("Ngày")),("Amount",L.T("Số tiền")),("Quantity",L.T("Lượng")));
            TotalCountSelector=rows=>((OrbitPageRows<ReportSource>)rows).TotalCount;UseStackedSearch(L.T("chứng từ"));Search.Visible=false;Workspace.Description.Text=description;
            Grid.Columns["SourceDate"]!.DefaultCellStyle.Format="dd/MM/yyyy HH:mm:ss";
            Grid.Columns["Amount"]!.DefaultCellStyle.Format="#,##0' ₫'";Grid.Columns["Amount"]!.DefaultCellStyle.FormatProvider=global::System.Globalization.CultureInfo.GetCultureInfo("vi-VN");
            AddAction("Xem chi tiết",async()=>{if(Selected is ReportSource {SourceType:not "Material"} source)await navigation.OpenAsync(this,source);},needsRow:true);
        }
        public Sources(ReportService service,SessionContext session,ReportFilter filter,string code,ReportSourceNavigation navigation)
        {Configure("Chứng từ nguồn",session,async(_,skip,t)=>{var p=await service.SourcesAsync(session,filter,code,skip,t);return new OrbitPageRows<ReportSource>(p.Rows,p.TotalCount);},("SourceType",L.T("Nguồn")),("Code",L.T("Mã")),("Name",L.T("Nội dung / Kỳ")),("SourceDate",L.T("Ngày")),("Amount",L.T("Số tiền")),("Quantity",L.T("Lượng / Giờ")));TotalCountSelector=rows=>((OrbitPageRows<ReportSource>)rows).TotalCount;UseStackedSearch(L.T("chứng từ"));Search.Visible=false;Workspace.Description.Text=L.T("Chỉ đọc · Giữ bộ lọc và kỳ báo cáo khi quay lại.");Grid.Columns["SourceDate"]!.DefaultCellStyle.Format="dd/MM/yyyy HH:mm";Grid.Columns["Amount"]!.DefaultCellStyle.Format="#,##0' ₫'";Grid.Columns["Amount"]!.DefaultCellStyle.FormatProvider=global::System.Globalization.CultureInfo.GetCultureInfo("vi-VN");AddAction("Xem chi tiết",async()=>{if(Selected is ReportSource source)await navigation.OpenAsync(this,source);},needsRow:true);}
    }
}


