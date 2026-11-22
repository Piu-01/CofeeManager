using System.Globalization;
using CafeManager.Business.Security;
using CafeManager.Business.Services;
using CafeManager.Models.Dtos.Loyalty;
using CafeManager.Models.Dtos.Products;
using CafeManager.Models.Dtos.Customers;
using CafeManager.Models.Requests.Loyalty;
using CafeManager.Models.States;
using CafeManager.WinForms.Forms;
using CafeManager.WinForms.UI;
namespace CafeManager.WinForms.Modules.Loyalty;
public sealed class LoyaltyControl : UserControl
{
    public LoyaltyControl(){}
    public LoyaltyControl(LoyaltyService service,CustomerService customers,ProductService products,SessionContext session)
    {
        var tabs=new OrbitManagementTabs();
        tabs.AddPage(L.T("Khuyến mãi"),new PromotionList(service,customers,products,session));
        if(session.Identity.Role=="ADMIN"){tabs.AddPage(L.T("Hạng thành viên"),new PolicyList(service,session));}
        Controls.Add(tabs);
    }
    internal static decimal ReadMoney(TextBox input){if(!decimal.TryParse(input.Text,NumberStyles.Number,CultureInfo.GetCultureInfo("vi-VN"),out var amount))throw new ArgumentException(L.T("Tiền không hợp lệ; dùng dấu phẩy cho phần thập phân."));LoyaltyService.Money(amount);return amount;}
}
internal sealed class PromotionList : OrbitRecordListControl
{
    private readonly LoyaltyService service;private readonly CustomerService customers;private readonly ProductService products;private readonly SessionContext session;
    public PromotionList(LoyaltyService service,CustomerService customers,ProductService products,SessionContext session)
    {
        this.service=service;this.customers=customers;this.products=products;this.session=session;
        Configure("Khuyến mãi và preview SQL",session,async(q,skip,t)=>await service.PromotionsAsync(session,q,skip,null,t),("Code",L.T("Mã")),("Name",L.T("Chương trình")),("StartDate",L.T("Từ ngày")),("EndDate",L.T("Đến ngày")),("Discount",L.T("Giảm %")),("Status",L.T("Trạng thái")),("Audience",L.T("Đối tượng")),("Scope",L.T("Phạm vi")),("ItemCount",L.T("Số món")));
        Grid.Columns["StartDate"]!.DefaultCellStyle.Format=Grid.Columns["EndDate"]!.DefaultCellStyle.Format="dd/MM/yyyy";
        Grid.Columns["Discount"]!.DefaultCellStyle.Format="0' %'";
        Grid.CellFormatting+=(_,e)=>
        {
            if(e.RowIndex<0||e.ColumnIndex<0)return;
            if(Grid.Columns[e.ColumnIndex].Name=="EndDate"&&e.Value is null){e.Value=L.T("Không giới hạn");e.FormattingApplied=true;return;}
            if(e.Value is not string value||Grid.Columns[e.ColumnIndex].Name is not ("Status" or "Audience" or "Scope"))return;
            e.Value=Grid.Columns[e.ColumnIndex].Name switch
            {
                "Status"=>value switch{"Nhap"=>L.T("Nháp"),"DangChay"=>L.T("Đang chạy"),"TamDung"=>L.T("Tạm dừng"),"HetHan"=>L.T("Hết hạn"),_=>value},
                "Audience"=>value switch{"ALL"=>L.T("Mọi khách"),"FIRST"=>L.T("Khách lần đầu"),"MEMBER"=>L.T("Khách có hồ sơ"),"TIER"=>L.T("Theo hạng"),_=>value},
                _=>value=="ITEM"?L.T("Theo món"):L.T("Tất cả món")
            };
            e.FormattingApplied=true;
        };
        var admin=session.Identity.Role=="ADMIN";
        AddAction("Thêm",()=>Edit(true),admin);AddAction("Sửa",()=>Edit(false),admin);AddAction("Xóa",Delete,admin);
        AddAction("Bật / Tạm dừng",ToggleState,admin,true);AddAction("Preview ưu đãi",Preview);
    }
    private async Task Edit(bool create)
    {
        if(!create&&Selected is not PromotionRow)return;var row=create?null:(PromotionRow)Selected!;
        var detail=row is null?null:await service.PromotionDetailAsync(session,row.Code);
        var current=detail?.Promotion??row;
        var tiers=await service.PolicyAsync(session);
        var catalog=(await products.ListAsync(session,"",0,null,true)).ToList();
        using var dialog=new OperationEditorDialog(create?L.T("Thêm khuyến mãi"):L.F("Sửa khuyến mãi · {0}",row!.Code),id=>service.ReconcileAsync(session,id));
        var name=dialog.AddText(L.T("Tên chương trình"),current?.Name??"");
        var start=new OrbitDateField{Value=(current?.StartDate??DateOnly.FromDateTime(DateTime.Today)).ToDateTime(TimeOnly.MinValue)};
        var end=new OrbitDateField{Value=(current?.EndDate??DateOnly.FromDateTime(DateTime.Today.AddMonths(1))).ToDateTime(TimeOnly.MinValue),Enabled=current?.EndDate is not null};
        var noEnd=new CheckBox{Text=L.T("Không đặt ngày kết thúc"),AutoSize=true,Checked=current?.EndDate is null};
        if(create)noEnd.Checked=false;end.Enabled=!noEnd.Checked;
        noEnd.CheckedChanged+=(_,_)=>end.Enabled=!noEnd.Checked;
        dialog.AddField(L.T("Ngày bắt đầu"),start);dialog.AddField(L.T("Ngày kết thúc (bao gồm ngày này)"),end);dialog.AddField("",noEnd);
        var discount=dialog.AddText(L.T("Tỷ lệ giảm 0–100%"),current?.Discount.ToString()??"0");
        var statusChoices=new[]{(Code:"Nhap",Label:L.T("Nháp")),(Code:"DangChay",Label:L.T("Đang chạy")),(Code:"TamDung",Label:L.T("Tạm dừng"))};
        var status=new ComboBox{DropDownStyle=ComboBoxStyle.DropDownList,DataSource=statusChoices.ToList(),DisplayMember="Label",ValueMember="Code"};
        status.SelectedValue=current?.Status switch{"Nhap"=>"Nhap","TamDung"=>"TamDung",_=>"DangChay"};dialog.AddField(L.T("Trạng thái"),status);
        var audienceChoices=new[]{(Code:"ALL",Label:L.T("Mọi khách (kể cả vãng lai)")),(Code:"FIRST",Label:L.T("Khách lần đầu (chưa hóa đơn đã trả)")),(Code:"MEMBER",Label:L.T("Khách có hồ sơ")),(Code:"TIER",Label:L.T("Theo hạng thành viên"))};
        var audience=new ComboBox{DropDownStyle=ComboBoxStyle.DropDownList,DataSource=audienceChoices.ToList(),DisplayMember="Label",ValueMember="Code"};
        audience.SelectedValue=current?.Audience??"ALL";dialog.AddField(L.T("Đối tượng khách"),audience);
        var tierList=new CheckedListBox{CheckOnClick=true,Height=110};
        foreach(var tier in tiers)tierList.Items.Add(tier.Code);
        for(var index=0;index<tiers.Count;index++)tierList.SetItemChecked(index,detail?.Tiers.Contains(tiers[index].Code)??false);
        dialog.AddField(L.T("Hạng được áp (chỉ khi chọn Theo hạng)"),tierList);
        void SyncTier(){var enabled=audience.SelectedValue as string=="TIER";tierList.Enabled=enabled;}
        SyncTier();audience.SelectedValueChanged+=(_,_)=>SyncTier();
        var table=new global::System.Data.DataTable();table.Columns.Add("Selected",typeof(bool));table.Columns.Add("Code",typeof(string));table.Columns.Add("Name",typeof(string));table.Columns.Add("Price",typeof(decimal));
        var chosen=new HashSet<string>(detail?.Items.Select(x=>x.Code)??[],StringComparer.Ordinal);
        foreach(var product in catalog)table.Rows.Add(chosen.Contains(product.Code),product.Code,product.Name,product.Price);
        var itemGrid=new DataGridView{DataSource=table,AutoGenerateColumns=false,AllowUserToAddRows=false,AllowUserToDeleteRows=false,RowHeadersVisible=false,Height=220,SelectionMode=DataGridViewSelectionMode.FullRowSelect,MultiSelect=false};
        itemGrid.Columns.Add(new DataGridViewCheckBoxColumn{Name="Selected",HeaderText=L.T("Chọn"),DataPropertyName="Selected",Width=60});
        itemGrid.Columns.Add(new DataGridViewTextBoxColumn{Name="Code",HeaderText=L.T("Mã"),DataPropertyName="Code",ReadOnly=true,Width=70});
        itemGrid.Columns.Add(new DataGridViewTextBoxColumn{Name="Name",HeaderText=L.T("Món"),DataPropertyName="Name",ReadOnly=true,AutoSizeMode=DataGridViewAutoSizeColumnMode.Fill});
        itemGrid.Columns.Add(new DataGridViewTextBoxColumn{Name="Price",HeaderText=L.T("Giá"),DataPropertyName="Price",ReadOnly=true,Width=110,DefaultCellStyle=new DataGridViewCellStyle{Format="0' ₫'",Alignment=DataGridViewContentAlignment.MiddleRight}});
        var itemSearch=new TextBox{Dock=DockStyle.Top};
        itemSearch.TextChanged+=(_,_)=>table.DefaultView.RowFilter=string.IsNullOrWhiteSpace(itemSearch.Text)?"":"Code LIKE '%"+itemSearch.Text.Trim().Replace("'","''")+"%' OR Name LIKE '%"+itemSearch.Text.Trim().Replace("'","''")+"%'";
        var itemHost=new Panel{Height=260};itemHost.Controls.Add(itemGrid);itemGrid.Dock=DockStyle.Fill;itemHost.Controls.Add(itemSearch);itemSearch.Dock=DockStyle.Top;
        dialog.AddField(L.T("Món được giảm (không chọn = tất cả món)"),itemHost);
        dialog.CaptureWrite=()=>
        {
            if(!int.TryParse(discount.Text,out var percent))throw new ArgumentException(L.T("Tỷ lệ phải nguyên 0–100."));
            if(!itemGrid.EndEdit())throw new ArgumentException(L.T("Hoàn tất chọn món."));
            var selectedItems=table.Rows.Cast<global::System.Data.DataRow>().Where(x=>x.RowState!=global::System.Data.DataRowState.Deleted&&x["Selected"] is true).Select(x=>Convert.ToString(x["Code"])??"").Where(x=>x.Length>0).ToArray();
            var selectedTiers=tierList.CheckedItems.Cast<string>().ToArray();
            var request=LoyaltyService.Normalize(new PromotionRequest(current?.Code??"",name.Text,DateOnly.FromDateTime(start.Value),
                noEnd.Checked?null:DateOnly.FromDateTime(end.Value),percent,status.SelectedValue as string??"DangChay",
                audience.SelectedValue as string??"ALL",selectedTiers,selectedItems),create);
            return id=>service.SavePromotionAsync(session,request,current?.Revision,id);
        };
        if(await CafeManager.WinForms.UI.OrbitDialogHost.ShowAsync(dialog, this)==DialogResult.OK)await Reload();
    }
    private async Task ToggleState()
    {
        if(Selected is not PromotionRow row)return;
        var resume=row.Status is "TamDung" or "Nhap";
        using var dialog=new OperationEditorDialog(resume?L.T("Bật khuyến mãi"):L.T("Tạm dừng khuyến mãi"),id=>service.ReconcileAsync(session,id));
        dialog.AddNote(resume?L.F("{0} · {1}: đơn mới sẽ tự áp theo điều kiện; đơn đang mở phải xác nhận lại số mới khi chốt.",row.Code,row.Name)
            :L.F("{0} · {1}: ngừng tự áp cho đơn mới; đơn đang mở/đã chốt giữ nguyên.",row.Code,row.Name));
        dialog.Confirmation=resume?L.F("Bật {0}?",row.Code):L.F("Tạm dừng {0}?",row.Code);
        dialog.CaptureWrite=()=>id=>service.SetStateAsync(session,row,resume,id);
        if(await CafeManager.WinForms.UI.OrbitDialogHost.ShowAsync(dialog, this)==DialogResult.OK)await Reload();
    }
    private async Task Delete(){if(Selected is not PromotionRow row)return;using var dialog=new OperationEditorDialog(L.T("Xóa khuyến mãi"),id=>service.ReconcileAsync(session,id));dialog.AddNote(L.F("{0} · {1}; có hóa đơn tham chiếu thì không xóa được.", row.Code, row.Name));dialog.Confirmation=L.F("Xóa {0}?", row.Code);dialog.CaptureWrite=()=>id=>service.DeletePromotionAsync(session,row,id);if(await CafeManager.WinForms.UI.OrbitDialogHost.ShowAsync(dialog, this)==DialogResult.OK)await Reload();}
    private async Task Preview()
    {
        var catalog=(await products.ListAsync(session,"",0,null,true)).ToList();
        using var dialog=new OperationEditorDialog(L.T("Preview ưu đãi từ SQL"),id=>service.ReconcileAsync(session,id));dialog.SetReadOnly();
        var ship=dialog.AddText(L.T("Phí ship nguyên VND"),"0");
        var date=new OrbitDateField();dialog.AddField(L.T("Ngày lập cần preview"),date);
        CustomerRow? customer=null;var customerButton=new OrbitButton{Text=L.T("Khách vãng lai · Chọn khách"),AutoSize=true};dialog.AddField(L.T("Khách hàng"),customerButton);
        customerButton.Click+=async(_,_)=>{using var picker=new CustomerPickerDialog(customers,session,true);if(await CafeManager.WinForms.UI.OrbitDialogHost.ShowAsync(picker, dialog)==DialogResult.OK){customer=picker.SelectedCustomer;customerButton.Text=customer is null?L.T("Khách vãng lai"):customer.Name+" · "+customer.Code;}};
        var table=new global::System.Data.DataTable();table.Columns.Add("Product",typeof(string));table.Columns.Add("Name",typeof(string));table.Columns.Add("Price",typeof(decimal));table.Columns.Add("Quantity",typeof(int));
        var lineGrid=new DataGridView{DataSource=table,AutoGenerateColumns=false,AllowUserToAddRows=true,AllowUserToDeleteRows=true,RowHeadersVisible=false,Height=190,EditMode=DataGridViewEditMode.EditOnEnter};
        lineGrid.Columns.Add(new DataGridViewComboBoxColumn{Name="Product",HeaderText=L.T("Món"),DataPropertyName="Product",DataSource=catalog,DisplayMember="Name",ValueMember="Code",FlatStyle=FlatStyle.Flat,AutoSizeMode=DataGridViewAutoSizeColumnMode.Fill});
        lineGrid.Columns.Add(new DataGridViewTextBoxColumn{Name="Name",HeaderText=L.T("Tên"),DataPropertyName="Name",ReadOnly=true,Visible=false});
        lineGrid.Columns.Add(new DataGridViewTextBoxColumn{Name="Price",HeaderText=L.T("Giá"),DataPropertyName="Price",ReadOnly=true,Width=110,DefaultCellStyle=new DataGridViewCellStyle{Format="0' ₫'",Alignment=DataGridViewContentAlignment.MiddleRight}});
        lineGrid.Columns.Add(new DataGridViewTextBoxColumn{Name="Quantity",HeaderText=L.T("SL"),DataPropertyName="Quantity",Width=70});
        lineGrid.CurrentCellDirtyStateChanged+=(_,_)=>{if(lineGrid.IsCurrentCellDirty)lineGrid.CommitEdit(DataGridViewDataErrorContexts.Commit);};
        lineGrid.DataError+=(_,e)=>{e.ThrowException=false;};
        dialog.AddField(L.T("Dòng món giả lập"),lineGrid);
        var totalLabel=new Label{AutoSize=true};dialog.AddField(L.T("Tiền món"),totalLabel);
        var preview=new OrbitButton{Text=L.T("Xem preview SQL"),AutoSize=true};dialog.AddField(L.T("Kiểm tra số tiền"),preview);
        var result=new Label{AutoSize=true,MaximumSize=new Size(560,0)};dialog.AddField(L.T("Kết quả"),result);
        var version=0;decimal TotalMoney(){return table.Rows.Cast<global::System.Data.DataRow>().Where(x=>x.RowState!=global::System.Data.DataRowState.Deleted).Sum(x=>Convert.ToDecimal(x["Price"])*Convert.ToDecimal(x["Quantity"] is DBNull?0:x["Quantity"]));}
        void InvalidatePreview(){version++;result.Text=L.T("Cần tải preview mới.");}
        lineGrid.CellValueChanged+=(_,e)=>
        {
            if(e.RowIndex<0||e.ColumnIndex<0)return;
            if(lineGrid.Columns[e.ColumnIndex].Name=="Product")
            {
                var data=table.Rows[e.RowIndex];var picked=catalog.FirstOrDefault(x=>x.Code==Convert.ToString(data["Product"]));
                if(picked is not null){data["Name"]=picked.Name;data["Price"]=picked.Price;}
            }
            totalLabel.Text=TotalMoney().ToString("N0",CultureInfo.GetCultureInfo("vi-VN"))+" ₫";InvalidatePreview();
        };
        table.RowDeleted+=(_,_)=>InvalidatePreview();
        ship.TextChanged+=(_,_)=>InvalidatePreview();date.ValueChanged+=(_,_)=>InvalidatePreview();customerButton.Click+=(_,_)=>InvalidatePreview();
        preview.Click+=async(_,_)=>
        {
            if(!preview.Enabled)return;preview.Enabled=false;var current=++version;result.Text=L.T("Đang tải…");
            try
            {
                if(!lineGrid.EndEdit())throw new ArgumentException(L.T("Hoàn tất dòng món."));
                var lines=table.Rows.Cast<global::System.Data.DataRow>().Where(x=>x.RowState!=global::System.Data.DataRowState.Deleted)
                    .Select(x=>(Code:Convert.ToString(x["Product"])??"",Quantity:Convert.ToInt32(x["Quantity"] is DBNull?0:x["Quantity"])))
                    .Where(x=>x.Code.Length>0&&x.Quantity>0).Select(x=>new PreviewLine(x.Code,x.Quantity)).ToArray();
                var total=TotalMoney();var shipFee=LoyaltyControl.ReadMoney(ship);
                var response=await service.DiscountAsync(session,new DiscountPreviewRequest(total,shipFee,DateOnly.FromDateTime(date.Value),customer?.Code,lines));
                if(!dialog.IsDisposed&&current==version)
                {
                    var money=CultureInfo.GetCultureInfo("vi-VN");
                    var sourceText=response.Source=="TierAndPromotion"?L.F("{0}: hạng {1}% + khuyến mãi {2}% = {3}%",SalesStates.Label(response.Source),response.TierPercent,response.PromotionPercent,response.PercentDiscount):L.F("{0}: {1}%",SalesStates.Label(response.Source),response.PercentDiscount);
                    var promos=response.Promotions.Count==0?L.T("Không có ưu đãi nào thỏa điều kiện."):string.Join("\n",response.Promotions.Select(x=>L.F("• {0} {1}: {2}% trên {3} ₫ → −{4} ₫",x.Code,x.Name,x.Percent,x.BaseAmount.ToString("N0",money),x.Discount.ToString("N0",money))));
                    result.Text=L.F("Nguồn {0}\nHạng {1} ₫ · Ưu đãi {2} ₫\n{3}\nGiảm {4} ₫\nPhải trả {5} ₫\nĐọc SQL {6}; chưa là kết quả checkout.",
                        sourceText,response.TierDiscount.ToString("N0",money),response.PromotionDiscount.ToString("N0",money),promos,
                        response.Discount.ToString("N0",money),response.Payable.ToString("N0",money),string.Format(money,"{0:HH:mm:ss}",response.ReadAt));
                }
            }
            catch(Exception ex)when(ex is UserOperationException or UnauthorizedAccessException or ArgumentException or OperationCanceledException){if(!dialog.IsDisposed&&current==version)result.Text=L.T("Chưa có preview hợp lệ · ")+ex.Message;}
            finally{if(!preview.IsDisposed)preview.Enabled=true;}
        };
        await CafeManager.WinForms.UI.OrbitDialogHost.ShowAsync(dialog, this);version++;
    }
}
internal sealed class PolicyList : OrbitRecordListControl
{
    private readonly LoyaltyService service;private readonly SessionContext session;private bool rowBusy;
    public PolicyList(LoyaltyService service,SessionContext session)
    {
        this.service=service;this.session=session;
        Configure("Chính sách hạng",session,async(_,skip,t)=>skip==0?await service.PolicyAsync(session,t):new List<TierRow>(),("Code",L.T("Hạng")),("MinimumPoints",L.T("Ngưỡng điểm")),("Discount",L.T("Giảm %")));Search.Visible=false;Paginated=false;
        AddAction("Thêm",AddTier);AddRowDeleteColumn();
        RowActivated=item=>{_=RunRowAction(()=>EditTier((TierRow)item));};
        RowDeleteRequested=item=>{_=RunRowAction(()=>DeleteTier((TierRow)item));};
    }
    private async Task RunRowAction(Func<Task> action)
    {
        if(rowBusy)return;rowBusy=true;
        try{await action();}
        catch(Exception ex)when(ex is UserOperationException or UnauthorizedAccessException or ArgumentException or OperationCanceledException){if(!IsDisposed)State.Text=ex.Message;}
        finally{rowBusy=false;}
    }
    private static TierInput Input(TierRow row)=>new(row.Code,row.MinimumPoints,row.Discount);
    private Task AddTier()=>ShowEditor(null);
    private Task EditTier(TierRow row)=>ShowEditor(row);
    private async Task ShowEditor(TierRow? row)
    {
        var current=await service.PolicyAsync(session);
        using var dialog=new OperationEditorDialog(row is null?L.T("Thêm hạng thành viên"):L.T("Sửa hạng thành viên"),id=>service.ReconcileAsync(session,id));
        var code=dialog.AddText(L.T("Mã hạng (A–Z, 0–9, tối đa 5 ký tự)"),row?.Code??"");
        var threshold=dialog.AddText(L.T("Ngưỡng điểm"),row?.MinimumPoints.ToString()??"");
        var discount=dialog.AddText(L.T("Giảm % (0–100)"),row?.Discount.ToString()??"0");
        var view=new OrbitButton{Text=L.T("Xem lại từ SQL"),AutoSize=true};dialog.AddField(L.T("Preview trước khi lưu"),view);
        var info=new Label{AutoSize=true,MaximumSize=new Size(520,0)};dialog.AddField(L.T("Ảnh hưởng"),info);
        PolicyPreview? preview=null;IReadOnlyList<TierInput>? captured=null;var version=0;
        void Invalidate(){version++;preview=null;captured=null;info.Text=L.T("Cần xem lại SQL.");}
        code.TextChanged+=(_,_)=>Invalidate();threshold.TextChanged+=(_,_)=>Invalidate();discount.TextChanged+=(_,_)=>Invalidate();
        IReadOnlyList<TierInput> ReadRows()
        {
            if(!int.TryParse(threshold.Text.Trim(),out var points)||points<0)throw new ArgumentException(L.T("Ngưỡng điểm phải là số nguyên không âm."));
            if(!int.TryParse(discount.Text.Trim(),out var percent)||percent<0||percent>100)throw new ArgumentException(L.T("Giảm % phải nguyên 0–100."));
            return current.Where(x=>row is null||x.Code!=row.Code).Select(Input).Append(new TierInput(code.Text.Trim().ToUpperInvariant(),points,percent)).OrderBy(x=>x.MinimumPoints).ToArray();
        }
        view.Click+=async(_,_)=>{if(!view.Enabled)return;view.Enabled=false;info.Text=L.T("Đang tải…");try{var rows=ReadRows();var currentVersion=version;var response=await service.PolicyPreviewAsync(session,rows);if(!dialog.IsDisposed&&currentVersion==version){preview=response;captured=rows;info.Text=L.F("{0} khách sẽ đổi hạng. Sau thay đổi: {1}", response.AffectedCustomers, string.Join("; ",rows.Select(x=>$"{x.Code}: {x.MinimumPoints}/{x.Discount}%")));}}catch(Exception ex)when(ex is UserOperationException or UnauthorizedAccessException or ArgumentException or OperationCanceledException){if(!dialog.IsDisposed){preview=null;captured=null;info.Text=ex.Message;}}finally{if(!view.IsDisposed)view.Enabled=true;}};
        dialog.CaptureWrite=()=>{var rows=ReadRows();if(preview is null||captured is null||!rows.SequenceEqual(captured))throw new ArgumentException(L.T("Cần xem lại SQL cho nội dung hiện tại."));var snapshot=preview;var payload=rows.ToArray();dialog.Confirmation=L.F("{0} hạng và tính lại hạng toàn bộ khách trong một transaction?",row is null?L.T("Thêm"):L.T("Sửa"));return async id=>{try{return await service.PolicySaveAsync(session,payload,snapshot,id);}catch{preview=null;captured=null;throw;}};};
        if(await OrbitDialogHost.ShowAsync(dialog,this)==DialogResult.OK)await Reload();
    }
    private async Task DeleteTier(TierRow row)
    {
        var current=await service.PolicyAsync(session);
        if(current.Count<=1||row.MinimumPoints==0)throw new ArgumentException(L.T("Phải giữ một hạng có ngưỡng 0 điểm."));
        var rows=current.Where(x=>x.Code!=row.Code).Select(Input).OrderBy(x=>x.MinimumPoints).ToArray();
        var preview=await service.PolicyPreviewAsync(session,rows);
        using var dialog=new OperationEditorDialog(L.T("Xóa hạng thành viên"),id=>service.ReconcileAsync(session,id));
        dialog.AddNote(L.F("{0} · ngưỡng {1} · giảm {2}%. {3} khách sẽ được xếp lại hạng theo ngưỡng còn lại.",row.Code,row.MinimumPoints,row.Discount,preview.AffectedCustomers));
        dialog.Confirmation=L.F("Xóa hạng {0}?",row.Code);
        dialog.CaptureWrite=()=>id=>service.PolicySaveAsync(session,rows,preview,id);
        if(await OrbitDialogHost.ShowAsync(dialog,this)==DialogResult.OK)await Reload();
    }
}
