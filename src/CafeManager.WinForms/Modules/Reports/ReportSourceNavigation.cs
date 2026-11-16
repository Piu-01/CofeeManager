using CafeManager.Business.Security;
using CafeManager.Business.Services;
using CafeManager.Models.Dtos.Reports;
using CafeManager.WinForms.Forms;
using CafeManager.WinForms.UI;
namespace CafeManager.WinForms.Modules.Reports;

public sealed class ReportSourceNavigation(SessionContext session,DocumentLinks links,InvoiceService invoices,StockIssueService issues,StocktakeService stocktakes,AttendanceService attendance)
{
    public async Task OpenAsync(Control owner,ReportSource source)
    {
        switch(source.SourceType){
            case "Finance":await links.ShowFinanceAsync(owner,source.Code);return;
            case "Purchase":await links.ShowPurchaseAsync(owner,source.Code);return;
            case "Debt":await links.ShowDebtAsync(owner,source.Code);return;
            case "Asset":await links.ShowAssetAsync(owner,source.Code);return;
            case "Payroll":if(!int.TryParse(source.Code,out var id))throw new ArgumentException(L.T("Mã bảng lương sai."));await links.ShowPayrollAsync(owner,id);return;
        }
        using var dialog=await Create();if(owner.IsDisposed||session.IsRevoked)return;await OrbitDialogHost.ShowAsync(dialog,owner);
        async Task<OrbitDialogForm> Create(){
            if(source.SourceType=="Invoice"){var row=await invoices.DetailAsync(session,source.Code)??throw new ArgumentException(L.T("Hóa đơn không còn tồn tại."));return new Modules.Sales.InvoiceDetailDialog(row,invoices,session);}
            if(source.SourceType=="Issue"){var row=await issues.GetAsync(session,source.Code)??throw new ArgumentException(L.T("Phiếu xuất không tồn tại."));return new Modules.Inventory.StockIssueDetailDialog(row);}
            string text;
            if(source.SourceType=="Stocktake"){var row=await stocktakes.GetAsync(session,source.Code)??throw new ArgumentException(L.T("Phiếu kiểm kê không tồn tại."));text=$"{row.Code} · {row.Date:dd/MM/yyyy} · "+(row.Closed?L.T("Đã chốt"):L.T("Chưa chốt"))+"\r\n"+string.Join("\r\n",row.Lines.Select(l=>L.F("{0} · {1}: hệ thống {2}, đếm {3} {4} · {5}", l.Code, l.Name, string.Format(global::System.Globalization.CultureInfo.GetCultureInfo("vi-VN"), "{0:N4}", l.Expected), string.Format(global::System.Globalization.CultureInfo.GetCultureInfo("vi-VN"), "{0:N4}", l.Actual), l.Unit, l.Reason)));}
            else if(source.SourceType=="Attendance"){
                var parts=source.Code.Split(':');if(parts.Length!=3||!DateTime.TryParseExact(parts[1],"yyyyMMdd",null,global::System.Globalization.DateTimeStyles.None,out var day))throw new ArgumentException(L.T("Nguồn công không hợp lệ."));
                Models.Dtos.HumanResources.AttendanceRow? found=null;for(int skip=0;;skip+=50){var rows=await attendance.ListAsync(session,new(day.Month,day.Year,parts[0],Skip:skip));found=rows.FirstOrDefault(r=>r.WorkDate.Date==day.Date&&r.Shift==parts[2]);if(found is not null||rows.Count<50)break;}
                if(found is null)throw new ArgumentException(L.T("Dòng công không còn tồn tại."));text=L.F("{0} · {1}\r\nNgày làm {2} · {3}\r\nVào {4}\r\nRa {5}\r\n{6} · {7}", found.Employee, found.EmployeeName, string.Format(global::System.Globalization.CultureInfo.GetCultureInfo("vi-VN"), "{0:dd/MM/yyyy}", found.WorkDate), found.ShiftName, string.Format(global::System.Globalization.CultureInfo.GetCultureInfo("vi-VN"), "{0:dd/MM/yyyy HH:mm:ss}", found.ClockIn), string.Format(global::System.Globalization.CultureInfo.GetCultureInfo("vi-VN"), "{0:dd/MM/yyyy HH:mm:ss}", found.ClockOut), found.StatusText, found.PeriodText);
            }else if(source.SourceType=="Opening")text=L.F("Mở sổ {0}\r\n{1}\r\nThời điểm {2}\r\nLượng {3}", source.Code, source.Name, string.Format(global::System.Globalization.CultureInfo.GetCultureInfo("vi-VN"), "{0:dd/MM/yyyy HH:mm:ss}", source.SourceDate), string.Format(global::System.Globalization.CultureInfo.GetCultureInfo("vi-VN"), "{0:N4}", source.Quantity));
            else throw new ArgumentException(L.T("Loại nguồn không được hỗ trợ."));
            var result=new OrbitDialogForm{Text=L.T("Chi tiết nguồn — ")+source.Code,AutoScaleDimensions=new(96,96),AutoScaleMode=AutoScaleMode.Dpi,ClientSize=new(760,500)};var body=new TextBox{ReadOnly=true,Multiline=true,Dock=DockStyle.Fill,ScrollBars=ScrollBars.Both,Text=text};var close=new OrbitButton{Text=L.T("Đóng")};close.Click+=(_,_)=>result.Close();var actions=new FlowLayoutPanel();actions.Controls.Add(close);var layout=new OrbitDialogLayout(result,body,new Label(),actions);result.Controls.Add(layout);layout.EnableResponsiveActions(result,false);return result;
        }
    }
}

