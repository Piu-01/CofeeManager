using System.Globalization;
using CafeManager.Business.Security;
using CafeManager.Business.Services;
using CafeManager.Models.Dtos.Loyalty;
using CafeManager.Models.Dtos.Products;
using CafeManager.Models.Requests.Loyalty;
using CafeManager.WinForms.Forms;
using CafeManager.WinForms.Theme;
using CafeManager.WinForms.UI;
namespace CafeManager.WinForms.Modules.Loyalty;
public sealed class LoyaltyControl : UserControl
{
    public LoyaltyControl(){}
    public LoyaltyControl(LoyaltyService service,CustomerService customers,ProductService products,SessionContext session)
    {
        var tabs=new OrbitManagementTabs();
        tabs.AddPage(L.T("Khuyến mãi"),new PromotionList(service,products,session));
        if(session.Identity.Role=="ADMIN"){tabs.AddPage(L.T("Hạng thành viên"),new PolicyList(service,session));}
        Controls.Add(tabs);
    }
    internal static decimal ReadMoney(TextBox input){if(!decimal.TryParse(input.Text,NumberStyles.Number,CultureInfo.GetCultureInfo("vi-VN"),out var amount))throw new ArgumentException(L.T("Tiền không hợp lệ; dùng dấu phẩy cho phần thập phân."));LoyaltyService.Money(amount);return amount;}
}
internal sealed class PromotionList : OrbitRecordListControl
{
    private readonly LoyaltyService service;private readonly ProductService products;private readonly SessionContext session;
    public PromotionList(LoyaltyService service,ProductService products,SessionContext session)
    {
        this.service=service;this.products=products;this.session=session;
        Configure("Khuyến mãi",session,async(q,skip,t)=>await service.PromotionsAsync(session,q,skip,null,t),("Code",L.T("Mã")),("Name",L.T("Chương trình")),("Discount",L.T("Ưu đãi")),("StartDate",L.T("Thời gian")),("Audience",L.T("Đối tượng")),("Status",L.T("Trạng thái")));
        Grid.Columns["StartDate"]!.ToolTipText=L.T("Lặp hằng năm theo ngày/tháng.");
        Grid.CellFormatting+=(_,e)=>
        {
            if(e.RowIndex<0||e.ColumnIndex<0||Grid.Rows[e.RowIndex].DataBoundItem is not PromotionRow row)return;
            switch(Grid.Columns[e.ColumnIndex].Name)
            {
                case "Discount":
                    e.Value=row.Kind=="Tang"?L.F("Tặng {0} món · {1} phần",row.GiftItemCount,row.GiftQuantity):L.F("Giảm {0}%",row.Discount);break;
                case "StartDate":e.Value=Period(row);Grid.Rows[e.RowIndex].Cells[e.ColumnIndex].ToolTipText=L.T("Lặp hằng năm theo ngày/tháng.");break;
                case "Audience":e.Value=row.Audience switch{"TIER"=>L.T("Theo hạng"),"WALKIN"=>L.T("Khách vãng lai"),_=>L.T("Tất cả khách")};break;
                case "Status":
                    var state=StatusLabel(row);e.Value=state.Text;
                    e.CellStyle.ForeColor=state.Color;e.CellStyle.SelectionForeColor=state.Color;break;
                default:return;
            }
            e.FormattingApplied=true;
        };
        var admin=session.Identity.Role=="ADMIN";
        AddAction("Thêm",()=>Edit(true),admin);AddAction("Sửa",()=>Edit(false),admin);AddAction("Xóa",Delete,admin);
    }
    private static string Period(PromotionRow row)
    {
        var start=row.StartDate.ToString("dd/MM",CultureInfo.InvariantCulture);
        return row.EndDate is DateOnly end?L.F("{0} – {1}",start,end.ToString("dd/MM",CultureInfo.InvariantCulture)):L.F("{0} – hết năm",start);
    }
    // Trạng thái lưu chỉ Áp dụng/Tạm ngưng; ngoài khung ngày hiển thị suy ra cho người xem.
    private static (string Text,global::System.Drawing.Color Color) StatusLabel(PromotionRow row)
    {
        if(row.Status=="TamDung")return (L.T("Tạm ngưng"),OrbitManagementTheme.Muted);
        var md=DateTime.Today.Month*100+DateTime.Today.Day;
        var start=row.StartDate.Month*100+row.StartDate.Day;
        var end=row.EndDate is DateOnly last?last.Month*100+last.Day:int.MaxValue;
        var inside=end>=start?md>=start&&md<=end:md>=start||md<=end;
        if(inside)return (L.T("Áp dụng"),OrbitManagementTheme.Caramel);
        return md<start&&end>=start?(L.T("Chưa tới kỳ"),OrbitManagementTheme.Muted):(L.T("Hết hạn"),global::System.Drawing.Color.FromArgb(255,180,180));
    }
    private async Task Edit(bool create)
    {
        if(!create&&Selected is not PromotionRow)return;var row=create?null:(PromotionRow)Selected!;
        var detail=row is null?null:await service.PromotionDetailAsync(session,row.Code);
        var current=detail?.Promotion??row;
        var tiers=await service.PolicyAsync(session);
        var catalog=(await products.ListAsync(session,"",0,null,true)).ToList();
        using var dialog=new OperationEditorDialog(create?L.T("Thêm khuyến mãi"):L.F("Sửa khuyến mãi · {0}",row!.Code),id=>service.ReconcileAsync(session,id));
        dialog.AddNote(L.T("THÔNG TIN CHUNG"));
        var name=dialog.AddText(L.T("Tên chương trình"),current?.Name??"");
        var statusChoices=new[]{(Code:"DangChay",Label:L.T("Áp dụng")),(Code:"TamDung",Label:L.T("Tạm ngưng"))};
        var status=new ComboBox{DropDownStyle=ComboBoxStyle.DropDownList,DataSource=statusChoices.ToList(),DisplayMember="Label",ValueMember="Code"};
        status.SelectedValue=current?.Status=="TamDung"?"TamDung":"DangChay";dialog.AddField(L.T("Trạng thái"),status);
        dialog.AddNote(L.T("THỜI GIAN ÁP DỤNG · LẶP HẰNG NĂM"));
        var start=new MaskedTextBox{Mask="00/00",Text=(current?.StartDate??DateOnly.FromDateTime(DateTime.Today)).ToString("dd/MM",CultureInfo.InvariantCulture)};
        var end=new MaskedTextBox{Mask="00/00",Text=(current?.EndDate??DateOnly.FromDateTime(DateTime.Today.AddMonths(1))).ToString("dd/MM",CultureInfo.InvariantCulture)};
        var noEnd=new CheckBox{Text=L.T("Không đặt ngày kết thúc (áp đến hết năm)"),AutoSize=true,Checked=current?.EndDate is null&&!create};
        end.Enabled=!noEnd.Checked;noEnd.CheckedChanged+=(_,_)=>end.Enabled=!noEnd.Checked;
        dialog.AddField(L.T("Từ ngày (dd/MM)"),start);dialog.AddField(L.T("Đến ngày (dd/MM)"),end);dialog.AddField("",noEnd);
        dialog.AddNote(L.T("Ví dụ 20/12 – 10/01 sẽ áp qua năm mới; so sánh theo ngày/tháng, không theo năm."));
        dialog.AddNote(L.T("NỘI DUNG ƯU ĐÃI"));
        var giftKind=current?.Kind=="Tang";
        var offerTabs=new OrbitSectionTabs{Height=OrbitManagementTheme.Px(dialog,44)};offerTabs.Add(L.T("Giảm giá"));offerTabs.Add(L.T("Tặng món"));
        dialog.AddField(L.T("Loại ưu đãi"),offerTabs);
        var discount=dialog.AddText(L.T("Tỷ lệ giảm 0–100%"),current?.Discount.ToString(CultureInfo.InvariantCulture)??"0");
        var itemsPicker=new PromotionItemPicker(catalog,false){Visible=!giftKind};
        var giftsPicker=new PromotionItemPicker(catalog,true){Visible=giftKind};
        var pickerHost=new Panel{Dock=DockStyle.Fill,Height=OrbitManagementTheme.Px(dialog,300)};
        pickerHost.Controls.Add(itemsPicker);pickerHost.Controls.Add(giftsPicker);
        dialog.AddField(L.T("Món áp dụng"),pickerHost);
        itemsPicker.SetItems(detail?.Items.Select(x=>x.Code)??[]);
        giftsPicker.SetGifts(detail?.Gifts.Select(x=>new PromotionGift(x.Code,x.Quantity))??[]);
        void SyncKind()
        {
            var gift=offerTabs.SelectedIndex==1;discount.Enabled=!gift;itemsPicker.Visible=!gift;giftsPicker.Visible=gift;
        }
        offerTabs.SelectedIndex=giftKind?1:0;offerTabs.SelectedIndexChanged+=(_,_)=>SyncKind();SyncKind();
        dialog.AddNote(L.T("ĐỐI TƯỢNG KHÁCH"));
        var allGuest=new RadioButton{Text=L.T("Tất cả khách"),AutoSize=true,Checked=(current?.Audience??"ALL") is not ("TIER" or "WALKIN")};
        var tierGuest=new RadioButton{Text=L.T("Theo hạng"),AutoSize=true,Checked=current?.Audience=="TIER"};
        var walkinGuest=new RadioButton{Text=L.T("Khách vãng lai"),AutoSize=true,Checked=current?.Audience=="WALKIN"};
        var audience=new FlowLayoutPanel{AutoSize=true,Dock=DockStyle.Top,FlowDirection=FlowDirection.LeftToRight,WrapContents=false,Margin=Padding.Empty};
        audience.Controls.AddRange([allGuest,tierGuest,walkinGuest]);
        dialog.AddField(L.T("Đối tượng khách"),audience);
        var allTiers=new CheckBox{Text=L.T("Tất cả hạng"),AutoSize=true,Dock=DockStyle.Top,Checked=(detail?.Tiers.Count??0)==0};
        var tierList=new CheckedListBox{CheckOnClick=true,Dock=DockStyle.Fill};
        foreach(var tier in tiers)tierList.Items.Add(tier.Code);
        for(var index=0;index<tiers.Count;index++)tierList.SetItemChecked(index,detail?.Tiers.Contains(tiers[index].Code)??false);
        var tierPanel=new Panel{Dock=DockStyle.Fill,Height=OrbitManagementTheme.Px(dialog,150)};
        tierPanel.Controls.Add(tierList);tierPanel.Controls.Add(allTiers);
        dialog.AddField(L.T("Hạng được áp (bỏ trống hoặc chọn Tất cả hạng = mọi hạng)"),tierPanel);
        var syncing=false;
        string Audience()=>tierGuest.Checked?"TIER":walkinGuest.Checked?"WALKIN":"ALL";
        void SyncAudience(){var byTier=Audience()=="TIER";allTiers.Enabled=byTier;tierList.Enabled=byTier&&!allTiers.Checked;}
        allTiers.CheckedChanged+=(_,_)=>
        {
            if(syncing)return;syncing=true;
            if(allTiers.Checked)for(var index=0;index<tierList.Items.Count;index++)tierList.SetItemChecked(index,false);
            syncing=false;SyncAudience();
        };
        tierList.ItemCheck+=(_,e)=>
        {
            if(syncing||e.NewValue!=CheckState.Checked||!allTiers.Checked)return;
            syncing=true;allTiers.Checked=false;syncing=false;SyncAudience();
        };
        allGuest.CheckedChanged+=(_,_)=>SyncAudience();tierGuest.CheckedChanged+=(_,_)=>SyncAudience();walkinGuest.CheckedChanged+=(_,_)=>SyncAudience();
        SyncAudience();
        dialog.CaptureWrite=()=>
        {
            var gift=offerTabs.SelectedIndex==1;var percent=0;
            if(!gift&&(!int.TryParse(discount.Text.Trim(),out percent)||percent is <0 or >100))throw new ArgumentException(L.T("Tỷ lệ giảm phải nguyên 0–100."));
            var startDay=Day(start.Text,L.T("Từ ngày"));DateOnly? endDay=noEnd.Checked?null:Day(end.Text,L.T("Đến ngày"));
            var account=Audience();
            var selectedItems=gift?global::System.Array.Empty<string>():itemsPicker.ReadItems();
            var selectedGifts=gift?giftsPicker.ReadGifts():global::System.Array.Empty<PromotionGift>();
            var selectedTiers=account=="TIER"&&!allTiers.Checked?tierList.CheckedItems.Cast<string>().ToArray():[];
            var request=LoyaltyService.Normalize(new PromotionRequest(current?.Code??"",name.Text,startDay,endDay,percent,
                status.SelectedValue as string??"DangChay",account,selectedTiers,selectedItems,gift?"Tang":"Giam",selectedGifts),create);
            return id=>service.SavePromotionAsync(session,request,current?.Revision,id);
        };
        if(await OrbitDialogHost.ShowAsync(dialog,this)==DialogResult.OK)await Reload();
    }
    private static DateOnly Day(string text,string label)
    {
        if(!DateTime.TryParseExact((text??"").Trim(),"dd/MM",CultureInfo.InvariantCulture,DateTimeStyles.None,out var value))
            throw new ArgumentException(L.F("{0} không hợp lệ; nhập ngày/tháng dd/MM.",label));
        return new DateOnly(2000,value.Month,value.Day);
    }
    private async Task Delete(){if(Selected is not PromotionRow row)return;using var dialog=new OperationEditorDialog(L.T("Xóa khuyến mãi"),id=>service.ReconcileAsync(session,id));dialog.AddNote(L.F("{0} · {1}; có hóa đơn tham chiếu thì không xóa được.", row.Code, row.Name));dialog.Confirmation=L.F("Xóa {0}?", row.Code);dialog.CaptureWrite=()=>id=>service.DeletePromotionAsync(session,row,id);if(await CafeManager.WinForms.UI.OrbitDialogHost.ShowAsync(dialog, this)==DialogResult.OK)await Reload();}
}
// Chọn món cho ưu đãi: chế độ giảm tick nhiều món, chế độ tặng tick kèm số lượng.
internal sealed class PromotionItemPicker : UserControl
{
    private readonly global::System.Data.DataTable table=new();
    private readonly DataGridView grid=new();
    private readonly TextBox search=new();
    private readonly Label summary=new();
    private readonly bool gifts;
    public PromotionItemPicker(IEnumerable<ProductRow> catalog,bool gifts)
    {
        this.gifts=gifts;Dock=DockStyle.Fill;BackColor=OrbitManagementTheme.Surface;MinimumSize=new Size(0,OrbitManagementTheme.Px(this,220));
        table.Columns.Add("Selected",typeof(bool));table.Columns.Add("Code",typeof(string));table.Columns.Add("Name",typeof(string));table.Columns.Add("Price",typeof(decimal));table.Columns.Add("Quantity",typeof(int));
        foreach(var product in catalog)table.Rows.Add(false,product.Code,product.Name,product.Price,1);
        grid.DataSource=table;grid.AutoGenerateColumns=false;grid.AllowUserToAddRows=false;grid.AllowUserToDeleteRows=false;grid.RowHeadersVisible=false;grid.SelectionMode=DataGridViewSelectionMode.FullRowSelect;grid.MultiSelect=false;grid.EditMode=DataGridViewEditMode.EditOnEnter;grid.Dock=DockStyle.Fill;grid.BorderStyle=BorderStyle.FixedSingle;
        grid.Columns.Add(new DataGridViewCheckBoxColumn{Name="Selected",HeaderText=L.T("Chọn"),DataPropertyName="Selected",Width=60});
        grid.Columns.Add(new DataGridViewTextBoxColumn{Name="Code",HeaderText=L.T("Mã"),DataPropertyName="Code",ReadOnly=true,Width=80});
        grid.Columns.Add(new DataGridViewTextBoxColumn{Name="Name",HeaderText=L.T("Món"),DataPropertyName="Name",ReadOnly=true,AutoSizeMode=DataGridViewAutoSizeColumnMode.Fill});
        grid.Columns.Add(new DataGridViewTextBoxColumn{Name="Price",HeaderText=L.T("Giá"),DataPropertyName="Price",ReadOnly=true,Width=110,DefaultCellStyle=new DataGridViewCellStyle{Format="0' ₫'",Alignment=DataGridViewContentAlignment.MiddleRight}});
        grid.Columns.Add(new DataGridViewTextBoxColumn{Name="Quantity",HeaderText=L.T("SL"),DataPropertyName="Quantity",Visible=gifts,ReadOnly=!gifts,Width=70,DefaultCellStyle=new DataGridViewCellStyle{Alignment=DataGridViewContentAlignment.MiddleRight}});
        grid.CurrentCellDirtyStateChanged+=(_,_)=>{if(grid.IsCurrentCellDirty)grid.CommitEdit(DataGridViewDataErrorContexts.Commit);};
        grid.DataError+=(_,e)=>e.ThrowException=false;
        grid.CellValueChanged+=(_,e)=>
        {
            if(e.RowIndex<0||e.ColumnIndex<0)return;
            var row=table.Rows[e.RowIndex];
            if(grid.Columns[e.ColumnIndex].Name=="Selected"&&row["Selected"] is true&&(row["Quantity"] is not int quantity||quantity<1))row["Quantity"]=1;
            UpdateSummary();
        };
        search.PlaceholderText=L.T("Tìm mã hoặc tên món…");search.Dock=DockStyle.Top;
        var searchField=new OrbitField(search,true){Dock=DockStyle.Top,Height=OrbitManagementTheme.Px(this,36)};
        search.TextChanged+=(_,_)=>
        {
            var text=search.Text.Trim();
            table.DefaultView.RowFilter=text.Length==0?"":$"Code LIKE '%{Escape(text)}%' OR Name LIKE '%{Escape(text)}%'";
        };
        var note=new Label{Text=gifts?L.T("Tặng một lần mỗi hóa đơn, giá 0 ₫, vẫn trừ kho."):L.T("Món được giảm · bỏ trống = tất cả món."),AutoSize=true,Dock=DockStyle.Fill,ForeColor=OrbitManagementTheme.Muted,Font=OrbitManagementTheme.Table};
        summary.AutoSize=true;summary.Dock=DockStyle.Fill;summary.ForeColor=OrbitManagementTheme.Text;summary.Font=OrbitManagementTheme.Table;
        var layout=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=1,RowCount=4,BackColor=OrbitManagementTheme.Surface};
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent,100));layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.Controls.Add(note,0,0);layout.Controls.Add(searchField,0,1);layout.Controls.Add(grid,0,2);layout.Controls.Add(summary,0,3);
        Controls.Add(layout);
        UpdateSummary();OrbitField.ApplyInputBehavior(this);
    }
    private static string Escape(string text)=>text.Replace("[","[[]").Replace("%","[%]").Replace("_","[_]").Replace("'","''");
    private IEnumerable<global::System.Data.DataRow> SelectedRows()=>table.Rows.Cast<global::System.Data.DataRow>().Where(row=>row["Selected"] is true);
    private void UpdateSummary()
    {
        var rows=SelectedRows().ToArray();
        summary.Text=gifts?L.F("{0} món · {1} phần",rows.Length,rows.Sum(row=>row["Quantity"] is int quantity&&quantity>0?quantity:0)):L.F("Đang chọn {0} món",rows.Length);
    }
    public void SetItems(IEnumerable<string> codes)
    {
        var chosen=new HashSet<string>(codes,StringComparer.Ordinal);
        foreach(global::System.Data.DataRow row in table.Rows)row["Selected"]=chosen.Contains((string)row["Code"]);
        UpdateSummary();
    }
    public void SetGifts(IEnumerable<PromotionGift> gifts)
    {
        var chosen=gifts.ToDictionary(x=>x.Product,StringComparer.Ordinal);
        foreach(global::System.Data.DataRow row in table.Rows)
        {
            var code=(string)row["Code"];
            row["Selected"]=chosen.TryGetValue(code,out var gift);
            if(chosen.ContainsKey(code))row["Quantity"]=Math.Clamp(gift!.Quantity,1,999);
        }
        UpdateSummary();
    }
    public string[] ReadItems()
    {
        grid.EndEdit();
        return SelectedRows().Select(row=>(string)row["Code"]).ToArray();
    }
    public PromotionGift[] ReadGifts()
    {
        grid.EndEdit();
        var result=new List<PromotionGift>();
        foreach(var row in SelectedRows())
        {
            var code=(string)row["Code"];
            var quantity=row["Quantity"] is int value?value:0;
            if(quantity is <1 or >999)throw new ArgumentException(L.F("Số lượng tặng của {0} phải từ 1 đến 999.",code));
            result.Add(new PromotionGift(code,quantity));
        }
        if(result.Count==0)throw new ArgumentException(L.T("Cần chọn ít nhất một món tặng."));
        return result.ToArray();
    }
}
internal sealed class PolicyList : OrbitRecordListControl
{
    private readonly LoyaltyService service;private readonly SessionContext session;private readonly OrbitRowEditor inline;
    public PolicyList(LoyaltyService service,SessionContext session)
    {
        this.service=service;this.session=session;
        Configure("Chính sách hạng",session,async(_,skip,t)=>skip==0?await service.PolicyAsync(session,t):new List<TierRow>(),("Code",L.T("Hạng")),("MinimumPoints",L.T("Ngưỡng điểm")),("Discount",L.T("Giảm %")));Search.Visible=false;Paginated=false;
        AddAction("Thêm",AddTier);AddRowDeleteColumn();Workspace.ShowMeta=false;
        inline=CreateInlineEditor();
        inline.RowKey=item=>((TierRow)item).Code;
        inline.ColumnFor=(item,cell)=>Column(cell.OwningColumn?.Name ?? "");
        inline.Edit=item=>ShowEditor((TierRow)item);inline.Delete=DeleteInline;
        inline.Reconcile=async(item,id,t)=>await service.ReconcileAsync(session,id,t) is not null;
        inline.Reload=()=>Reload();
    }
    private static InlineColumn? Column(string property)=>property switch
    {
        "MinimumPoints"=>new("MinimumPoints",InlineCellKind.Integer),
        "Discount"=>new("Discount",InlineCellKind.Integer),
        _=>null
    };
    private static TierInput Input(TierRow row)=>new(row.Code,row.MinimumPoints,row.Discount);
    private Task AddTier()=>ShowEditor(null);
    private async Task<bool> DeleteInline(object item,Guid id,CancellationToken token)
    {
        var row=(TierRow)item;
        var current=await service.PolicyAsync(session,token);
        if(current.Count<=1||row.MinimumPoints==0)throw new ArgumentException(L.T("Phải giữ một hạng có ngưỡng 0 điểm."));
        var rows=current.Where(x=>x.Code!=row.Code).Select(Input).OrderBy(x=>x.MinimumPoints).ToArray();
        var preview=await service.PolicyPreviewAsync(session,rows,token);
        var confirm=await OrbitConfirmationDialog.ShowAsync(this,L.F("{0} khách sẽ được xếp lại hạng. Xóa hạng {1}?",preview.AffectedCustomers,row.Code),L.T("Xóa hạng thành viên"),MessageBoxButtons.YesNo,L.T("Xóa"),L.T("Hủy"));
        if(confirm!=DialogResult.Yes)return false;
        await service.PolicySaveAsync(session,rows,preview,id,token);
        return true;
    }
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
