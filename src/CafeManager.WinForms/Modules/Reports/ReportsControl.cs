using System.Collections;
using System.Globalization;
using CafeManager.Business.Security;
using CafeManager.Business.Services;
using CafeManager.Models.Dtos.Reports;
using CafeManager.Models.Requests.Reports;
using CafeManager.WinForms.Forms;
using CafeManager.WinForms.Printing;
using CafeManager.WinForms.Printing.FastReport;
using CafeManager.WinForms.Theme;
using CafeManager.WinForms.UI;
namespace CafeManager.WinForms.Modules.Reports;

public sealed class ReportsControl : UserControl
{
    private readonly ReportService service;
    private readonly SessionContext session;
    private readonly ReportSourceNavigation sources;
    private readonly CancellationTokenSource lifetime=new();
    private CancellationTokenSource? reading;
    private readonly ComboBox kind=new(){DropDownStyle=ComboBoxStyle.DropDownList,Width=200},channel=new(){DropDownStyle=ComboBoxStyle.DropDownList,Width=150},option=new(){DropDownStyle=ComboBoxStyle.DropDownList,Width=220};
    private readonly OrbitDateRangeField dates=new(){Width=300,Height=40};
    private readonly OrbitDataGridView grid=new(){Dock=DockStyle.Fill,ReadOnly=true,AllowUserToAddRows=false,AutoGenerateColumns=true};
    private readonly Label summary=new(){Dock=DockStyle.Top,AutoSize=true,MaximumSize=new Size(1200,0),Padding=new Padding(10)};
    private readonly OrbitButton view=new(){Text=L.T("Xem báo cáo")},detail=new(){Text=L.T("Chứng từ nguồn")},pdf=new(){Text=L.T("In / PDF")},export=new(){Text=L.T("Xuất CSV")};
    private readonly OrbitListWorkspace workspace;
    private List<ReportOption> options=[];
    private ReportFilter? applied;
    private int generation,page;
    private bool initializing=true,valid,exporting;
    public ReportsControl(ReportService service,SessionContext session,ReportSourceNavigation sources)
    {
        this.service=service;this.session=session;this.sources=sources;Dock=DockStyle.Fill;AutoScaleDimensions=new(96,96);AutoScaleMode=AutoScaleMode.Dpi;
        kind.Items.AddRange(Enum.GetValues<ReportKind>().Select(k=>(object)Label(k)).ToArray());kind.SelectedIndex=0;
        channel.Items.AddRange([L.T("Mọi kênh"),L.T("Tại quán"),L.T("Mang đi"),L.T("Giao hàng")]);channel.SelectedIndex=0;dates.From.Value=DateTime.Today.AddDays(-30);dates.To.Value=DateTime.Today;
        workspace=new(kind){StackedTools=true};workspace.Title.Text=L.T("Báo cáo");workspace.Description.Text=L.T("Chọn loại và bộ lọc, sau đó Xem báo cáo · Số liệu theo nguồn SQL đã ghi.");workspace.Filters.Controls.AddRange([dates,channel,option]);workspace.SearchActions.Controls.Add(view);workspace.RowActions.Controls.Add(detail);workspace.RowActions.Controls.Add(pdf);workspace.RowActions.Controls.Add(export);workspace.Body.Controls.Add(grid);workspace.Body.Controls.Add(summary);Controls.Add(workspace);
        AppTheme.Apply(this);summary.ForeColor=AppTheme.Text;summary.BackColor=AppTheme.Surface;OrbitField.ApplyInputBehavior(this);
        grid.CellFormatting+=(_,e)=>{if(e.ColumnIndex>=0&&e.Value is string text){if(grid.Columns[e.ColumnIndex].DataPropertyName=="Coverage"){e.Value=Models.States.ReportLabels.Text(text);e.FormattingApplied=true;}else if(grid.Columns[e.ColumnIndex].DataPropertyName=="Name"&&applied?.Kind==ReportKind.SalesChannel){e.Value=Models.States.SalesStates.Label(text);e.FormattingApplied=true;}else if(grid.Columns[e.ColumnIndex].DataPropertyName=="Name"&&applied?.Kind==ReportKind.CashFlow&&grid.Rows[e.RowIndex].DataBoundItem is CashFlowReportRow {Code:"Sales"}){e.Value=L.T("Thu bán hàng");e.FormattingApplied=true;}}};
        view.Click+=async(_,_)=>{try{if(options.Count==0){options=await service.OptionsAsync(session,lifetime.Token);if(IsDisposed)return;ChooseOptions();}applied=CaptureFilter();page=0;await Read();}catch(Exception e)when(e is ArgumentException or UserOperationException or OperationCanceledException){if(!IsDisposed)InvalidateData(e.Message);}};
        workspace.Pagination.Previous.Click+=async(_,_)=>{if(valid&&page>0){page--;await Read();}};workspace.Pagination.Next.Click+=async(_,_)=>{if(valid){page++;await Read();}};
        detail.Click+=async(_,_)=>{if(!valid||applied is null||grid.CurrentRow?.DataBoundItem is not IReportRow row)return;using var dialog=new ReportSourceDialog(service,session,applied,row.Code,sources);await OrbitDialogHost.ShowAsync(dialog,this);};
        pdf.Click+=async(_,_)=>await PrintReport();export.Click+=async(_,_)=>await ExportCsv();
        kind.SelectedIndexChanged+=(_,_)=>{if(initializing)return;ChooseOptions();InvalidateData(L.T("Đã đổi loại; chọn Xem báo cáo."));};
        foreach(var c in new[]{channel,option})c.SelectedIndexChanged+=(_,_)=>{if(!initializing)InvalidateData(L.T("Đã đổi bộ lọc; chọn Xem báo cáo."));};
        dates.From.ValueChanged+=(_,_)=>{if(!initializing)InvalidateData(L.T("Đã đổi kỳ; chọn Xem báo cáo."));};dates.To.ValueChanged+=(_,_)=>{if(!initializing)InvalidateData(L.T("Đã đổi kỳ; chọn Xem báo cáo."));};
        Load+=async(_,_)=>{try{options=await service.OptionsAsync(session,lifetime.Token);if(IsDisposed)return;ChooseOptions();workspace.Status.Text=L.T("Chọn Xem báo cáo để tải số liệu.");}catch(Exception e)when(e is UserOperationException or OperationCanceledException){if(!IsDisposed)workspace.Status.Text=L.T("Chưa tải được bộ lọc: ")+e.Message;}finally{initializing=false;}};
        VisibleChanged+=(_,_)=>{if(!Visible)InvalidateData(L.T("Đã rời báo cáo; chọn Xem báo cáo để cập nhật."));};detail.Enabled=pdf.Enabled=export.Enabled=false;
    }
    public static string Label(ReportKind kind)=>kind switch{ReportKind.SalesDay=>L.T("Doanh thu theo ngày"),ReportKind.SalesChannel=>L.T("Doanh thu theo kênh"),ReportKind.Products=>L.T("Món trước giảm toàn bill"),ReportKind.Payments=>L.T("Phương thức thanh toán"),ReportKind.Inventory=>L.T("Đối chiếu tồn kho"),ReportKind.Debts=>L.T("Công nợ tại ngày"),ReportKind.CashFlow=>L.T("Dòng tiền quản trị"),ReportKind.Labor=>L.T("Công, chốt và chi lương"),_=>L.T("Tài sản / Khấu hao tham khảo")};
    // Item combo là chuỗi nhãn theo đúng thứ tự Enum.GetValues nên chỉ số ánh xạ 1-1 sang ReportKind.
    private ReportKind SelectedKind=>kind.SelectedIndex<0?ReportKind.SalesDay:(ReportKind)kind.SelectedIndex;
    private void ChooseOptions()
    {
        var selected=SelectedKind;var category=selected switch{ReportKind.Payments=>"Method",ReportKind.Debts=>"Supplier",ReportKind.Labor=>"Employee",ReportKind.Inventory=>"Material",ReportKind.Assets=>"Asset",_=>""};
        option.DataSource=new[]{new ReportOption(category,"",L.T("Tất cả"))}.Concat(options.Where(o=>o.Kind==category)).ToList();option.DisplayMember=nameof(ReportOption.Name);option.ValueMember=nameof(ReportOption.Code);option.Visible=category.Length>0;
        channel.Visible=selected is ReportKind.SalesDay or ReportKind.SalesChannel or ReportKind.Products or ReportKind.Payments;dates.From.Enabled=selected!=ReportKind.Debts;
    }
    private ReportFilter CaptureFilter()
    {
        var selected=SelectedKind;var to=DateOnly.FromDateTime(dates.To.Value);var from=selected==ReportKind.Debts?to:DateOnly.FromDateTime(dates.From.Value);var code=option.Visible&&option.SelectedItem is ReportOption o&&o.Code.Length>0?o.Code:null;
        return ReportService.Normalize(new(selected,from,to,channel.Visible?channel.SelectedIndex switch{1=>"Tai quan",2=>"Mang di",3=>"Giao hang",_=>null}:null,
            selected==ReportKind.Payments?code:null,selected==ReportKind.Debts?code:null,selected==ReportKind.Labor?code:null,selected==ReportKind.Inventory?code:null,selected==ReportKind.Assets?code:null));
    }
    private void InvalidateData(string message){reading?.Cancel();generation++;valid=false;page=0;detail.Enabled=pdf.Enabled=export.Enabled=false;workspace.Status.Text=message;workspace.Pagination.SetPending(L.T("Cần tải lại"));summary.Text=L.T("Chưa cập nhật · ")+message;}
    private async Task Read()
    {
        if(applied is null||IsDisposed||session.IsRevoked)return;reading?.Cancel();var own=CancellationTokenSource.CreateLinkedTokenSource(lifetime.Token,session.Token);reading=own;var current=++generation;valid=false;detail.Enabled=pdf.Enabled=export.Enabled=false;workspace.Status.Text=L.T("Đang tải…");workspace.Pagination.SetPending(L.T("Đang tải…"));summary.Text=L.T("Đang tải; số liệu trước chưa cập nhật.");
        try{
            var response=await service.ReadAsync(session,applied with{Skip=page*50},own.Token);if(IsDisposed||current!=generation||session.IsRevoked)return;
            grid.DataSource=Bind(response);FormatGrid();valid=true;detail.Enabled=pdf.Enabled=export.Enabled=response.Rows.Count>0;var m=response.Metadata;
            summary.Text=Totals(response);workspace.Description.Text=Label(m.Kind)+$" · {m.From:dd/MM/yyyy} → {m.To:dd/MM/yyyy} · "+Models.States.ReportLabels.Text(m.DateBasis);
            workspace.Status.Text=L.F("{0} dòng · Đọc {1} · ", m.TotalCount, string.Format(global::System.Globalization.CultureInfo.GetCultureInfo("vi-VN"), "{0:dd/MM/yyyy HH:mm}", m.ReadAt))+(m.Complete?"":L.T("Nguồn chưa đầy đủ; xem cột phạm vi. "))+Models.States.ReportLabels.Text(m.Note);workspace.Pagination.SetPage(page,50,m.TotalCount,L.T("dòng"));
        }catch(OperationCanceledException){}catch(UserOperationException e){if(!IsDisposed&&current==generation){workspace.Status.Text=L.T("Chưa cập nhật: ")+e.Message;summary.Text=L.T("Lỗi tải; chọn Xem báo cáo để thử lại.");}}
        finally{if(ReferenceEquals(reading,own))reading=null;own.Dispose();}
    }
    internal static IList Bind(ReportPage p)=>p.Metadata.Kind switch{ReportKind.SalesDay or ReportKind.SalesChannel=>p.Rows.Cast<SalesReportRow>().ToList(),ReportKind.Products=>p.Rows.Cast<ProductReportRow>().ToList(),ReportKind.Payments=>p.Rows.Cast<PaymentReportRow>().ToList(),ReportKind.Debts=>p.Rows.Cast<DebtReportRow>().ToList(),ReportKind.CashFlow=>p.Rows.Cast<CashFlowReportRow>().ToList(),ReportKind.Labor=>p.Rows.Cast<LaborReportRow>().ToList(),ReportKind.Assets=>p.Rows.Cast<AssetReportRow>().ToList(),_=>p.Rows.Cast<InventoryReportRow>().ToList()};
    private void FormatGrid()
    {
        foreach(DataGridViewColumn c in grid.Columns){c.HeaderText=Header(c.DataPropertyName);if(c.ValueType==typeof(decimal)||c.ValueType==typeof(decimal?)){c.DefaultCellStyle.Format=c.DataPropertyName is "Quantity" or "Opening" or "Purchases" or "Issues" or "Consumption" or "Adjustment" or "Closing" or "Current" or "Hours" or "RegularSnapshotHours" or "OvertimeSnapshotHours"?"N4":"#,##0' ₫'";c.DefaultCellStyle.FormatProvider=CultureInfo.GetCultureInfo("vi-VN");c.DefaultCellStyle.Alignment=DataGridViewContentAlignment.MiddleRight;}if(c.ValueType==typeof(DateTime)||c.ValueType==typeof(DateTime?))c.DefaultCellStyle.Format="dd/MM/yyyy HH:mm";}
    }
    internal static string Header(string name)=>ReportColumns.Header(name);
    internal static string Totals(ReportPage p){string V(decimal n)=>n.ToString("#,##0",CultureInfo.GetCultureInfo("vi-VN"))+" ₫";var t=p.Totals;return p.Metadata.Kind switch{
        ReportKind.SalesDay or ReportKind.SalesChannel=>L.F("Món {0} · Giảm {1} · Món sau giảm {2} · Ship {3} · Khách trả {4} · Phí đối tác {5}", V(t.Gross), V(t.Discount), V(t.Gross-t.Discount), V(t.Ship), V(t.Amount), V(t.Fees)),
        ReportKind.Debts=>L.F("Nợ gốc {0} · Đã trả {1} · Còn nợ {2}", V(t.Gross), V(t.Paid), V(t.Remaining)),
        ReportKind.CashFlow=>L.F("Thu {0} · Chi {1} · Thu − Chi {2} · Dòng tiền quản trị", V(t.Inflow), V(t.Outflow), V(t.Inflow-t.Outflow)),
        ReportKind.Labor=>L.F("Lương chốt {0} · Thực chi {1} · Không cộng hai nguồn thành chi phí", V(t.Committed), V(t.Paid)),
        ReportKind.Assets=>L.F("Nguyên giá {0} · KH kỳ {1} · KH lũy kế {2} · Còn {3} · Chi liên quan {4}", V(t.Gross), V(t.Depreciation), V(t.Accumulated), V(t.Remaining), V(t.Outflow)),
        ReportKind.Inventory=>L.T("Đối chiếu theo từng nguyên liệu/đơn vị. Không cộng các đơn vị khác nhau; kiểm cột phạm vi nguồn."),_=>L.F("Tổng {0}", V(t.Amount))};}
    // In tối đa 500 dòng, CSV 1.000 dòng: mỗi trang phải gọi lại procedure báo cáo nên chặn kỳ quá lớn.
    private async Task PrintReport()
    {
        if(!valid||applied is null||exporting||session.IsRevoked)return;
        exporting=true;pdf.Enabled=export.Enabled=false;workspace.Status.Text=L.T("Đang chuẩn bị PDF báo cáo…");
        try
        {
            var first=await service.ReadAsync(session,applied with{Skip=0},lifetime.Token);if(IsDisposed||!valid)return;
            var (rows,complete)=await ReportOutput.LoadAsync(service,session,applied,first,500,lifetime.Token);if(IsDisposed||!valid)return;
            var path=await Task.Run(()=>TabularPdfReport.Write(first,rows,complete,FilterText()));if(IsDisposed){PrintOutput.TryDelete(path);return;}
            using var dialog=new PdfPreviewForm(path,L.T("Báo cáo · ")+Label(first.Metadata.Kind));
            await OrbitDialogHost.ShowAsync(dialog,this);
            if(!IsDisposed&&valid)workspace.Status.Text=complete?L.T("Đã in đủ số dòng báo cáo.")
                :L.F("Chỉ in {0}/{1} dòng đầu; thu hẹp kỳ hoặc bộ lọc để in đủ.",rows.Count,first.Metadata.TotalCount);
        }
        catch(OperationCanceledException){}
        catch(Exception e)when(e is UserOperationException or InvalidOperationException or IOException or UnauthorizedAccessException)
        {if(!IsDisposed&&valid)workspace.Status.Text=L.T("Chưa tạo được PDF báo cáo: ")+e.Message;}
        finally{exporting=false;if(!IsDisposed)pdf.Enabled=export.Enabled=valid;}
    }
    private async Task ExportCsv()
    {
        if(!valid||applied is null||exporting||session.IsRevoked)return;
        using var save=new SaveFileDialog
        {
            Filter=L.T("CSV (*.csv)|*.csv"),
            FileName=$"BaoCao_{applied.Kind}_{applied.From:yyyyMMdd}_{applied.To:yyyyMMdd}.csv",
            AddExtension=true,DefaultExt="csv",
        };
        if(save.ShowDialog(OrbitDialogHost.NativeOwner(this))!=DialogResult.OK)return;
        exporting=true;pdf.Enabled=export.Enabled=false;workspace.Status.Text=L.T("Đang xuất CSV…");
        try
        {
            var first=await service.ReadAsync(session,applied with{Skip=0},lifetime.Token);if(IsDisposed||!valid)return;
            var (rows,complete)=await ReportOutput.LoadAsync(service,session,applied,first,1000,lifetime.Token);if(IsDisposed||!valid)return;
            await Task.Run(()=>ReportOutput.WriteCsv(save.FileName,first,rows,complete));
            workspace.Status.Text=(complete?L.F("Đã xuất {0} dòng: ",rows.Count):L.F("Đã xuất {0}/{1} dòng đầu (giới hạn): ",rows.Count,first.Metadata.TotalCount))+save.FileName;
        }
        catch(OperationCanceledException){}
        catch(Exception e)when(e is UserOperationException or IOException or UnauthorizedAccessException or InvalidOperationException)
        {if(!IsDisposed)workspace.Status.Text=L.T("Chưa xuất được CSV: ")+e.Message;}
        finally{exporting=false;if(!IsDisposed)pdf.Enabled=export.Enabled=valid;}
    }
    private string FilterText()
    {
        if(applied is null)return string.Empty;
        var parts=new List<string>();
        if(applied.Channel is{ } channel)parts.Add(L.T("Kênh: ")+Models.States.SalesStates.Label(channel));
        var subject=applied.Method??applied.Supplier??applied.Employee??applied.Material??applied.Asset;
        if(subject is not null)parts.Add(L.T("Đối tượng: ")+subject);
        return string.Join(" · ",parts);
    }
    protected override void Dispose(bool disposing){if(disposing){lifetime.Cancel();reading?.Cancel();lifetime.Dispose();}base.Dispose(disposing);}
}



