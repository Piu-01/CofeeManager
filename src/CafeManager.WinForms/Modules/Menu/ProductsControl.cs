using System.Globalization;
using CafeManager.Business.Security;
using CafeManager.Business.Services;
using CafeManager.Models.Dtos.Products;
using CafeManager.Models.Requests.Products;
using CafeManager.WinForms.Forms;
using CafeManager.WinForms.UI;
using CafeManager.WinForms.Theme;
namespace CafeManager.WinForms.Modules.Menu;
public sealed class ProductsControl : OrbitRecordListControl
{
    private readonly ProductService? service;
    private readonly SessionContext? session;
    private readonly ComboBox categoryFilter=new(){Width=170,DropDownStyle=ComboBoxStyle.DropDownList};
    private readonly CheckBox selling=new(){Text=L.T("Chỉ đang bán"),AutoSize=true};
    private OrbitRowEditor inline=null!;
    private List<CafeManager.Models.Dtos.Catalogs.NameLookup> categories=[];
    public ProductsControl(){}
    public ProductsControl(ProductService service,SessionContext session)
    {
        this.service=service;this.session=session;
        Configure("Menu và công thức",session,async(q,skip,t)=>await service.ListAsync(session,q,skip,string.IsNullOrEmpty(categoryFilter.SelectedValue as string)?null:categoryFilter.SelectedValue as string,selling.Checked,t),("Code",L.T("Mã")),("Name",L.T("Sản phẩm")),("Category",L.T("Danh mục")),("Price",L.T("Giá bán")),("Status",L.T("Trạng thái")));
        Grid.Columns["Price"]!.DefaultCellStyle.Format="#,##0' ₫'";Grid.Columns["Price"]!.DefaultCellStyle.FormatProvider=CultureInfo.GetCultureInfo("vi-VN");Grid.Columns["Price"]!.DefaultCellStyle.Alignment=DataGridViewContentAlignment.MiddleRight;
        AddFilter(categoryFilter,146); AddFilter(selling,124);
        categoryFilter.SelectedValueChanged+=async(_,_)=>{if(categoryFilter.DataSource is not null)await ResetAndReload();};
        selling.CheckedChanged+=async(_,_)=>await ResetAndReload();
        var admin=session.Identity.Role=="ADMIN";
        AddAction("Thêm",()=>Edit(true),admin);AddAction("Công thức",Recipe,session.Identity.Role is "ADMIN" or "STORE");
        AddRowDeleteColumn();
        inline=CreateInlineEditor();inline.CanWrite=_=>admin;inline.RowKey=item=>((ProductRow)item).Code;
        inline.ColumnFor=(item,cell)=>Column(cell.OwningColumn?.Name ?? "");
        inline.Edit=item=>Edit(false,(ProductRow)item);inline.Delete=DeleteInline;
        inline.Reconcile=async(_,id,t)=>await service.ReconcileAsync(session,id,t) is not null;inline.Reload=()=>Reload();
        Load+=async(_,_)=>{try{var rows=await service.CategoriesAsync(session);categories=rows;categoryFilter.DisplayMember="Name";categoryFilter.ValueMember="Code";categoryFilter.DataSource=new[]{new CafeManager.Models.Dtos.Catalogs.NameLookup("",L.T("Tất cả danh mục"))}.Concat(rows).ToList();}catch(Exception ex)when(ex is UserOperationException or UnauthorizedAccessException){State.Text=ex.Message;}};
    }
    private async Task Edit(bool create, ProductRow? row = null)
    {
        if(service is null||session is null)return;
        if(!create&&row is null)row=Selected as ProductRow;
        if(!create&&row is null)return;
        var categories=await service.CategoriesAsync(session);
        using var dialog=new OperationEditorDialog(create?L.T("Thêm sản phẩm"):L.F("Sửa sản phẩm · {0}",row!.Code),id=>service.ReconcileAsync(session,id));
        var name=dialog.AddText(L.T("Tên sản phẩm"),row?.Name??"");
        var category=new ComboBox{DropDownStyle=ComboBoxStyle.DropDownList,DataSource=categories,DisplayMember="Name",ValueMember="Code"};if(row is not null)category.SelectedValue=row.Category;dialog.AddField(L.T("Danh mục"),category);
        var price=dialog.AddText(L.T("Giá bán nguyên VND"),row?.Price.ToString("0",CultureInfo.InvariantCulture)??"");
        var status=new ComboBox{DropDownStyle=ComboBoxStyle.DropDownList};status.Items.AddRange([L.T("Ngừng bán"),L.T("Đang bán")]);status.SelectedIndex=row?.Status=="Dang ban"?1:0;dialog.AddField(L.T("Trạng thái"),status);
        dialog.CaptureWrite=()=>{if(!decimal.TryParse(price.Text,NumberStyles.Number,CultureInfo.GetCultureInfo("vi-VN"),out var amount))throw new ArgumentException(L.T("Giá không hợp lệ."));var request=ProductService.Normalize(new(row?.Code??"",name.Text,category.SelectedValue as string??"",amount,status.SelectedIndex==1?"Dang ban":"Ngung ban",[]),create);return id=>service.SaveAsync(session,request,row?.Revision,id);};
        if(await CafeManager.WinForms.UI.OrbitDialogHost.ShowAsync(dialog, this)==DialogResult.OK)await Reload();
    }
    private async Task Recipe()
    {
        if(service is null||session is null||Selected is not ProductRow row)return;
        var lines=await service.RecipeAsync(session,row.Code);var materials=await service.MaterialsAsync(session);
        var admin=session.Identity.Role=="ADMIN";
        using var dialog=new OperationEditorDialog(L.T("Công thức · ")+row.Name,id=>service.ReconcileAsync(session,id)){Width=900};
        var table=new global::System.Data.DataTable();table.Columns.Add("MaterialCode",typeof(string));table.Columns.Add("RecipeQuantity",typeof(string));table.Columns.Add("Unit",typeof(string));
        foreach(var line in lines)table.Rows.Add(line.Code,line.Quantity.ToString("0.####",CultureInfo.GetCultureInfo("vi-VN")),line.Unit);
        var grid=new DataGridView{DataSource=table,AutoSizeColumnsMode=DataGridViewAutoSizeColumnsMode.Fill,ReadOnly=!admin,AllowUserToAddRows=admin,AllowUserToDeleteRows=admin,SelectionMode=DataGridViewSelectionMode.FullRowSelect,MultiSelect=false,EditMode=DataGridViewEditMode.EditOnEnter};
        grid.AutoGenerateColumns=false;
        grid.Columns.Add(new DataGridViewComboBoxColumn{Name="Material",HeaderText=L.T("Nguyên liệu"),DataPropertyName="MaterialCode",DataSource=materials,DisplayMember="Name",ValueMember="Code",FlatStyle=FlatStyle.Flat,DisplayStyle=DataGridViewComboBoxDisplayStyle.DropDownButton,FillWeight=105,DefaultCellStyle=new DataGridViewCellStyle{NullValue=L.T("Chọn nguyên liệu…"),Padding=new Padding(OrbitManagementTheme.Px(grid,10),0,OrbitManagementTheme.Px(grid,10),0)}});
        grid.Columns.Add(new DataGridViewTextBoxColumn{Name="Quantity",HeaderText=L.T("Định mức · nhập lượng"),DataPropertyName="RecipeQuantity",ToolTipText=L.T("Nhập lượng theo đơn vị chuẩn, dùng dấu phẩy, tối đa 4 số lẻ (ví dụ 0,0180)."),FillWeight=55,DefaultCellStyle=new DataGridViewCellStyle{NullValue=L.T("Nhập lượng…"),Alignment=DataGridViewContentAlignment.MiddleRight,Padding=new Padding(OrbitManagementTheme.Px(grid,10),0,OrbitManagementTheme.Px(grid,10),0)}});
        grid.Columns.Add(new DataGridViewTextBoxColumn{Name="Unit",HeaderText=L.T("Đơn vị"),DataPropertyName="Unit",ReadOnly=true,FillWeight=30});
        // Cột thao tác xóa đúng dòng đang chọn; chỉ ADMIN thấy và bấm được.
        if(admin)grid.Columns.Add(new DataGridViewTextBoxColumn{Name="Delete",HeaderText="",ReadOnly=true,AutoSizeMode=DataGridViewAutoSizeColumnMode.None});
        // Chốt lựa chọn combo ngay để đơn vị không đợi rời ô hoặc bấm lần hai.
        grid.CurrentCellDirtyStateChanged+=(_,_)=>{if(grid.IsCurrentCellDirty&&grid.CurrentCell is DataGridViewComboBoxCell)grid.CommitEdit(DataGridViewDataErrorContexts.Commit);};
        grid.CellValueChanged+=(_,e)=>{dialog.MarkDirty();if(e.RowIndex>=0&&e.ColumnIndex==0){var selected=grid.Rows[e.RowIndex];var codeValue=Convert.ToString(selected.Cells[0].Value);selected.Cells[2].Value=materials.FirstOrDefault(m=>m.Code==codeValue)?.Unit??"";}};
        grid.UserDeletedRow+=(_,_)=>dialog.MarkDirty();
        // Control native đang edit cũng dùng cùng nền/chữ, không để combo trắng mặc định.
        grid.EditingControlShowing+=(_,e)=>
        {
            e.Control.BackColor=OrbitManagementTheme.Input;e.Control.ForeColor=OrbitManagementTheme.Text;e.Control.Font=OrbitManagementTheme.Body;
            // Chỉ chọn từ danh sách, không hiện ô edit lồng; danh sách xổ rộng bằng cột.
            if(e.Control is ComboBox combo){combo.DropDownStyle=ComboBoxStyle.DropDownList;combo.FlatStyle=FlatStyle.Flat;combo.DrawMode=DrawMode.OwnerDrawFixed;combo.ItemHeight=OrbitManagementTheme.Px(grid,26);combo.DropDownWidth=Math.Max(combo.Width,grid.Columns["Material"]!.Width);combo.DrawItem-=DrawRecipeMaterial;combo.DrawItem+=DrawRecipeMaterial;}
            if(e.Control is TextBox input){input.BorderStyle=BorderStyle.FixedSingle;input.PlaceholderText=L.T("Nhập lượng, ví dụ 0,0180");}
        };
        grid.DataError+=(_,e)=>{e.ThrowException=false;if(e.RowIndex>=0)grid.Rows[e.RowIndex].ErrorText=L.T("Chọn nguyên liệu có trong danh sách.");};
        OrbitManagementTheme.DialogGrid(grid);
        // Icon thùng rác từng dòng: hover đỏ, bấm chỉ bỏ dòng nháp, không ghi DB tới khi Lưu.
        var hoveredDelete=-1;
        if(admin)
        {
            grid.CellPainting+=(_,e)=>
            {
                if(e.RowIndex<0||e.ColumnIndex<0||grid.Columns[e.ColumnIndex].Name!="Delete")return;
                e.PaintBackground(e.CellBounds,true);
                if(!grid.Rows[e.RowIndex].IsNewRow)
                {
                    e.Graphics!.SmoothingMode=global::System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                    var icon=OrbitManagementTheme.Px(grid,16);
                    OrbitManagementTheme.DrawIcon(e.Graphics,new Rectangle(e.CellBounds.Left+(e.CellBounds.Width-icon)/2,e.CellBounds.Top+(e.CellBounds.Height-icon)/2,icon,icon),"trash",hoveredDelete==e.RowIndex?AppTheme.Error:OrbitManagementTheme.Muted);
                }
                e.Paint(e.CellBounds,DataGridViewPaintParts.Border);e.Handled=true;
            };
            grid.CellMouseEnter+=(_,e)=>{if(e.RowIndex==hoveredDelete)return;var old=hoveredDelete;hoveredDelete=e.RowIndex;if(old>=0&&old<grid.Rows.Count)grid.InvalidateRow(old);if(e.RowIndex>=0&&e.RowIndex<grid.Rows.Count)grid.InvalidateRow(e.RowIndex);};
            grid.CellMouseLeave+=(_,_)=>{if(hoveredDelete<0)return;var old=hoveredDelete;hoveredDelete=-1;if(old<grid.Rows.Count)grid.InvalidateRow(old);};
            grid.CellContentClick+=(_,e)=>
            {
                if(e.RowIndex<0||grid.Columns[e.ColumnIndex].Name!="Delete"||grid.Rows[e.RowIndex].IsNewRow)return;
                if(grid.Rows[e.RowIndex].DataBoundItem is not global::System.Data.DataRowView view)return;
                grid.EndEdit();dialog.MarkDirty();
                if(view.IsNew)view.CancelEdit();else view.Delete();
                grid.Focus();
            };
        }
        // Bảng co theo số dòng (dòng nhập mới tính là 1), tối đa 8 dòng rồi cuộn để dialog không còn khoảng trống.
        var host=new Panel{Dock=DockStyle.Top,BackColor=OrbitManagementTheme.Surface};
        host.Controls.Add(grid);grid.Dock=DockStyle.Fill;
        void FitHost(){var perRow=OrbitManagementTheme.Px(grid,40);var rows=grid.Rows.Cast<DataGridViewRow>().Count(r=>!r.IsNewRow)+(admin?1:0);host.Height=perRow*(Math.Min(8,Math.Max(2,rows))+1)+OrbitManagementTheme.Px(grid,2);}
        FitHost();grid.RowsAdded+=(_,_)=>FitHost();grid.RowsRemoved+=(_,_)=>FitHost();grid.DpiChangedAfterParent+=(_,_)=>FitHost();
        dialog.AddField(L.T("Nguyên liệu và định mức"),host);
        grid.Columns["Unit"]!.ReadOnly=true;
        if(session.Identity.Role!="ADMIN"){dialog.SetReadOnly();}
        else dialog.CaptureWrite=()=>{if(!grid.EndEdit())throw new ArgumentException(L.T("Hoàn tất dòng công thức."));var recipe=table.Rows.Cast<global::System.Data.DataRow>().Where(x=>x.RowState!=global::System.Data.DataRowState.Deleted).Select(x=>new RecipeInput(Convert.ToString(x[0])??"",decimal.TryParse(Convert.ToString(x[1]),NumberStyles.Number,CultureInfo.GetCultureInfo("vi-VN"),out var amount)?amount:0)).ToArray();ProductService.Normalize(new(row.Code,row.Name,row.Category,row.Price,row.Status,recipe));return id=>service.RecipeSaveAsync(session,row,recipe,id);};
        if(await CafeManager.WinForms.UI.OrbitDialogHost.ShowAsync(dialog, this)==DialogResult.OK)await Reload();
    }
    private static void DrawRecipeMaterial(object? sender,DrawItemEventArgs e)
    {
        if(sender is not ComboBox combo)return;
        var selected=(e.State&DrawItemState.Selected)!=0;
        using var fill=new SolidBrush(selected?OrbitManagementTheme.Selected:OrbitManagementTheme.Input);e.Graphics.FillRectangle(fill,e.Bounds);
        var text=e.Index>=0?combo.GetItemText(combo.Items[e.Index]):L.T("Chọn nguyên liệu…");
        TextRenderer.DrawText(e.Graphics,text,combo.Font,Rectangle.Inflate(e.Bounds,-4,0),OrbitManagementTheme.Text,TextFormatFlags.Left|TextFormatFlags.VerticalCenter|TextFormatFlags.EndEllipsis);
        e.DrawFocusRectangle();
    }
    private static InlineColumn? Column(string property)=>property switch
    {
        "Name"=>new("Name",InlineCellKind.Text),
        "Category"=>new("Category",InlineCellKind.Dropdown),
        "Price"=>new("Price",InlineCellKind.Money),
        "Status"=>new("Status",InlineCellKind.Dropdown),
        _=>null
    };
    // Sản phẩm thuần bán hàng: xóa ngay, không hộp xác nhận; SQL chặn khi có lịch sử.
    private async Task<bool> DeleteInline(object item,Guid id,CancellationToken token)
    {
        if(service is null||session is null)return false;
        await service.DeleteAsync(session,(ProductRow)item,id,token);return true;
    }
}







