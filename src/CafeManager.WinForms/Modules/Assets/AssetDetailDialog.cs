using System.Globalization;
using CafeManager.Business.Security;
using CafeManager.Business.Services;
using CafeManager.Models.Dtos.Assets;
using CafeManager.WinForms.Forms;
using CafeManager.WinForms.UI;
namespace CafeManager.WinForms.Modules.Assets;

public sealed class AssetDetailDialog : OrbitDialogForm
{
    private readonly CancellationTokenSource lifetime=new();
    public AssetDetailDialog(AssetRow row,AssetService service,SessionContext session,DocumentLinks links)
    {
        Text=L.T("Tài sản — ")+row.Code;Tag="Asset:"+row.Code;AutoScaleDimensions=new(96,96);AutoScaleMode=AutoScaleMode.Dpi;ClientSize=new(900,580);
        var summary=new TextBox{ReadOnly=true,Multiline=true,Dock=DockStyle.Top,Height=110};var dates=new OrbitDateRangeField{Dock=DockStyle.Top,Height=44};dates.From.Value=DateTime.Today.AddYears(-1);dates.To.Value=DateTime.Today;
        var grid=new OrbitDataGridView{Dock=DockStyle.Fill,ReadOnly=true,AutoGenerateColumns=true,AllowUserToAddRows=false};var body=new Panel{Dock=DockStyle.Fill};body.Controls.Add(grid);body.Controls.Add(dates);body.Controls.Add(summary);
        var state=new Label();var actions=new FlowLayoutPanel();var load=new OrbitButton{Text=L.T("Xem kỳ / Khấu hao")};var source=new OrbitButton{Text=L.T("Mở phiếu chi")};var close=new OrbitButton{Text=L.T("Đóng")};var pager=new OrbitPagination{Dock=DockStyle.Bottom};body.Controls.Add(pager);
        actions.Controls.AddRange([load,source,close]);var layout=new OrbitDialogLayout(this,body,state,actions);Controls.Add(layout);layout.EnableResponsiveActions(this,false);int page=0,generation=0;CancellationTokenSource? reading=null;
        async Task Read(){reading?.Cancel();var own=CancellationTokenSource.CreateLinkedTokenSource(lifetime.Token,session.Token);reading=own;var current=++generation;source.Enabled=false;state.Text=L.T("Đang tải chi liên quan…");try{var from=DateOnly.FromDateTime(dates.From.Value);var to=DateOnly.FromDateTime(dates.To.Value);var value=AssetDepreciationCalculator.Calculate(row.Value,row.Months,row.PurchasedOn,to);var expenses=await service.ExpensesAsync(session,row.Code,from,to,page*50,own.Token);if(IsDisposed||current!=generation)return;
                summary.Text=L.F("{0} · {1}\r\nNgày mua {2} · Nguyên giá {3} ₫ · {4} tháng\r\nKhấu hao THAM KHẢO đến {5}: {6} ₫; tháng này {7} ₫. Không phải dòng tiền/giá vốn.\r\nChi liên quan trong kỳ: {8} ₫ · Ước tính theo hồ sơ hiện tại.", row.Code, row.Name, string.Format(global::System.Globalization.CultureInfo.GetCultureInfo("vi-VN"), "{0:dd/MM/yyyy}", row.PurchasedOn), string.Format(global::System.Globalization.CultureInfo.GetCultureInfo("vi-VN"), "{0:N0}", row.Value), row.Months, string.Format(global::System.Globalization.CultureInfo.GetCultureInfo("vi-VN"), "{0:MM/yyyy}", to), string.Format(global::System.Globalization.CultureInfo.GetCultureInfo("vi-VN"), "{0:N0}", value.Accumulated), string.Format(global::System.Globalization.CultureInfo.GetCultureInfo("vi-VN"), "{0:N0}", value.Monthly), string.Format(global::System.Globalization.CultureInfo.GetCultureInfo("vi-VN"), "{0:N0}", expenses.TotalAmount));
                grid.DataSource=expenses.Rows;
                foreach(DataGridViewColumn col in grid.Columns){col.HeaderText=col.DataPropertyName switch{"Code"=>L.T("Mã phiếu"),"Date"=>L.T("Ngày"),"Category"=>L.T("Danh mục"),"Amount"=>L.T("Số tiền"),"Note"=>L.T("Ghi chú"),_=>col.HeaderText};if(col.DataPropertyName=="Amount"){col.DefaultCellStyle.Format="#,##0' ₫'";col.DefaultCellStyle.FormatProvider=CultureInfo.GetCultureInfo("vi-VN");col.DefaultCellStyle.Alignment=DataGridViewContentAlignment.MiddleRight;}if(col.DataPropertyName=="Date")col.DefaultCellStyle.Format="dd/MM/yyyy HH:mm";}
                pager.SetPage(page,50,expenses.TotalCount,L.T("phiếu chi"));state.Text=L.F("{0} phiếu · Đọc {1}", expenses.TotalCount, string.Format(global::System.Globalization.CultureInfo.GetCultureInfo("vi-VN"), "{0:dd/MM/yyyy HH:mm}", expenses.ReadAt));source.Enabled=true;
            }catch(OperationCanceledException){}catch(Exception e)when(e is ArgumentException or UserOperationException){if(!IsDisposed&&current==generation)state.Text=L.T("Chưa cập nhật: ")+e.Message;}finally{if(ReferenceEquals(reading,own))reading=null;own.Dispose();}}
        load.Click+=async(_,_)=>{page=0;await Read();};pager.Previous.Click+=async(_,_)=>{page=Math.Max(0,page-1);await Read();};pager.Next.Click+=async(_,_)=>{page++;await Read();};source.Click+=async(_,_)=>{if(grid.CurrentRow?.DataBoundItem is AssetExpense e)await links.ShowFinanceAsync(this,e.Code);};close.Click+=(_,_)=>Close();Shown+=async(_,_)=>await Read();
        dates.From.ValueChanged+=(_,_)=>{reading?.Cancel();generation++;source.Enabled=false;state.Text=L.T("Đã đổi kỳ; chọn Xem kỳ để tải lại.");};dates.To.ValueChanged+=(_,_)=>{reading?.Cancel();generation++;source.Enabled=false;state.Text=L.T("Đã đổi kỳ; chọn Xem kỳ để tải lại.");};Disposed+=(_,_)=>reading?.Cancel();
    }
    protected override void Dispose(bool disposing){if(disposing){lifetime.Cancel();lifetime.Dispose();}base.Dispose(disposing);}
}
