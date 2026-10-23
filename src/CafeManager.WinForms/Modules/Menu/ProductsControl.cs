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
    private readonly CheckBox selling=new(){Text="Chỉ đang bán",AutoSize=true};
    public ProductsControl(){}
    public ProductsControl(ProductService service,SessionContext session)
    {
        this.service=service;this.session=session;
        Configure("Menu và công thức",session,async(q,skip,t)=>await service.ListAsync(session,q,skip,string.IsNullOrEmpty(categoryFilter.SelectedValue as string)?null:categoryFilter.SelectedValue as string,selling.Checked,t),("Code","Mã"),("Name","Sản phẩm"),("Category","Danh mục"),("Price","Giá bán"),("Status","Trạng thái"));
        Grid.Columns["Price"]!.DefaultCellStyle.Format="#,##0' ₫'";Grid.Columns["Price"]!.DefaultCellStyle.FormatProvider=CultureInfo.GetCultureInfo("vi-VN");Grid.Columns["Price"]!.DefaultCellStyle.Alignment=DataGridViewContentAlignment.MiddleRight;
        AddFilter(categoryFilter,146); AddFilter(selling,124);
        categoryFilter.SelectedValueChanged+=async(_,_)=>{if(categoryFilter.DataSource is not null)await ResetAndReload();};
        selling.CheckedChanged+=async(_,_)=>await ResetAndReload();
        AddAction("Thêm",()=>Edit(true),session.Identity.Role=="ADMIN");AddAction("Sửa",()=>Edit(false),session.Identity.Role=="ADMIN");
        AddAction("Công thức",Recipe,session.Identity.Role is "ADMIN" or "STORE");AddAction("Xóa",Delete,session.Identity.Role=="ADMIN");
        Load+=async(_,_)=>{try{var rows=await service.CategoriesAsync(session);categoryFilter.DisplayMember="Name";categoryFilter.ValueMember="Code";categoryFilter.DataSource=new[]{new CafeManager.Models.Dtos.Catalogs.NameLookup("","Tất cả danh mục")}.Concat(rows).ToList();}catch(Exception ex)when(ex is UserOperationException or UnauthorizedAccessException){State.Text=ex.Message;}};
    }
    private async Task Edit(bool create)
    {
        if(service is null||session is null||(!create&&Selected is not ProductRow))return;
        var row=create?null:(ProductRow)Selected!;var categories=await service.CategoriesAsync(session);
        using var dialog=new OperationEditorDialog(create?"Thêm sản phẩm":"Sửa sản phẩm",id=>service.ReconcileAsync(session,id));
        var code=dialog.AddText("Mã SP001–SP999",row?.Code??"",!create);var name=dialog.AddText("Tên sản phẩm",row?.Name??"");
        var category=new ComboBox{DropDownStyle=ComboBoxStyle.DropDownList,DataSource=categories,DisplayMember="Name",ValueMember="Code"};if(row is not null)category.SelectedValue=row.Category;dialog.AddField("Danh mục",category);
        var price=dialog.AddText("Giá bán nguyên VND",row?.Price.ToString("0",CultureInfo.InvariantCulture)??"");
        var status=new ComboBox{DropDownStyle=ComboBoxStyle.DropDownList};status.Items.AddRange(["Ngừng bán","Đang bán"]);status.SelectedIndex=row?.Status=="Dang ban"?1:0;dialog.AddField("Trạng thái",status);
        dialog.CaptureWrite=()=>{if(!decimal.TryParse(price.Text,NumberStyles.Number,CultureInfo.GetCultureInfo("vi-VN"),out var amount))throw new ArgumentException("Giá không hợp lệ.");var request=ProductService.Normalize(new(code.Text,name.Text,category.SelectedValue as string??"",amount,status.SelectedIndex==1?"Dang ban":"Ngung ban",[]));return id=>service.SaveAsync(session,request,row?.Revision,id);};
        if(dialog.ShowDialog(this)==DialogResult.OK)await Reload();
    }
    private async Task Recipe()
    {
        if(service is null||session is null||Selected is not ProductRow row)return;
        var lines=await service.RecipeAsync(session,row.Code);var materials=await service.MaterialsAsync(session);
        using var dialog=new OperationEditorDialog("Công thức · "+row.Name,id=>service.ReconcileAsync(session,id)){Width=900,Height=620};
        var table=new global::System.Data.DataTable();table.Columns.Add("Mã nguyên liệu",typeof(string));table.Columns.Add("Định mức",typeof(string));table.Columns.Add("Đơn vị",typeof(string));
        foreach(var line in lines)table.Rows.Add(line.Code,line.Quantity.ToString("0.####",CultureInfo.GetCultureInfo("vi-VN")),line.Unit);
        var grid=new DataGridView{DataSource=table,Height=300,AutoSizeColumnsMode=DataGridViewAutoSizeColumnsMode.Fill,ReadOnly=session.Identity.Role!="ADMIN",AllowUserToAddRows=session.Identity.Role=="ADMIN",AllowUserToDeleteRows=session.Identity.Role=="ADMIN",SelectionMode=DataGridViewSelectionMode.FullRowSelect,MultiSelect=false,EditMode=DataGridViewEditMode.EditOnEnter};
        grid.AutoGenerateColumns=false;
        grid.Columns.Add(new DataGridViewComboBoxColumn{Name="Material",HeaderText="Nguyên liệu",DataPropertyName="Mã nguyên liệu",DataSource=materials,DisplayMember="Name",ValueMember="Code",FlatStyle=FlatStyle.Flat,DisplayStyle=DataGridViewComboBoxDisplayStyle.DropDownButton,DefaultCellStyle=new DataGridViewCellStyle{NullValue="Chọn nguyên liệu…"}});
        grid.Columns.Add(new DataGridViewTextBoxColumn{Name="Quantity",HeaderText="Định mức · nhập lượng",DataPropertyName="Định mức",ToolTipText="Nhập lượng theo đơn vị chuẩn, dùng dấu phẩy, tối đa 4 số lẻ (ví dụ 0,0180).",DefaultCellStyle=new DataGridViewCellStyle{NullValue="Nhập lượng…",Alignment=DataGridViewContentAlignment.MiddleRight}});
        grid.Columns.Add(new DataGridViewTextBoxColumn{Name="Đơn vị",HeaderText="Đơn vị",DataPropertyName="Đơn vị",ReadOnly=true});
        // Chốt lựa chọn combo ngay để đơn vị không đợi rời ô hoặc bấm lần hai.
        grid.CurrentCellDirtyStateChanged+=(_,_)=>{if(grid.IsCurrentCellDirty&&grid.CurrentCell is DataGridViewComboBoxCell)grid.CommitEdit(DataGridViewDataErrorContexts.Commit);};
        grid.CellValueChanged+=(_,e)=>{if(e.RowIndex>=0&&e.ColumnIndex==0){var selected=grid.Rows[e.RowIndex];var codeValue=Convert.ToString(selected.Cells[0].Value);selected.Cells[2].Value=materials.FirstOrDefault(m=>m.Code==codeValue)?.Unit??"";}};
        // Control native đang edit cũng dùng cùng nền/chữ, không để combo trắng mặc định.
        grid.EditingControlShowing+=(_,e)=>
        {
            e.Control.BackColor=OrbitManagementTheme.Surface;e.Control.ForeColor=OrbitManagementTheme.Text;e.Control.Font=OrbitManagementTheme.Body;
            if(e.Control is ComboBox combo){combo.FlatStyle=FlatStyle.Flat;combo.DrawMode=DrawMode.OwnerDrawFixed;combo.DrawItem-=DrawRecipeMaterial;combo.DrawItem+=DrawRecipeMaterial;}
            if(e.Control is TextBox input){input.BorderStyle=BorderStyle.FixedSingle;input.PlaceholderText="Nhập lượng, ví dụ 0,0180";}
        };
        grid.DataError+=(_,e)=>{e.ThrowException=false;if(e.RowIndex>=0)grid.Rows[e.RowIndex].ErrorText="Chọn nguyên liệu có trong danh sách.";};
        dialog.AddField("Nguyên liệu và định mức",grid);
        if(session.Identity.Role=="ADMIN")
        {
            var removeLine=new OrbitButton{Text="Xóa dòng đang chọn",AccessibleName="Xóa dòng công thức đang chọn",Dock=DockStyle.Bottom,Height=36};
            dialog.AddField("",removeLine);
            OrbitManagementTheme.Button(removeLine,ManagementButtonStyle.Danger,"trash");
            removeLine.Click+=(_,_)=>
            {
                // Chỉ bỏ dòng nháp trong editor, không xóa nguyên liệu hoặc ghi DB.
                var view=grid.CurrentRow?.DataBoundItem as global::System.Data.DataRowView;
                if(view is null)return;
                grid.CancelEdit();
                if(view.IsNew)view.CancelEdit();else view.Delete();
                dialog.MarkDirty();grid.Focus();
            };
        }
        grid.Columns["Đơn vị"]!.ReadOnly=true;
        if(session.Identity.Role!="ADMIN"){dialog.SetReadOnly();}
        else dialog.CaptureWrite=()=>{if(!grid.EndEdit())throw new ArgumentException("Hoàn tất dòng công thức.");var recipe=table.Rows.Cast<global::System.Data.DataRow>().Where(x=>x.RowState!=global::System.Data.DataRowState.Deleted).Select(x=>new RecipeInput(Convert.ToString(x[0])??"",decimal.TryParse(Convert.ToString(x[1]),NumberStyles.Number,CultureInfo.GetCultureInfo("vi-VN"),out var amount)?amount:0)).ToArray();ProductService.Normalize(new(row.Code,row.Name,row.Category,row.Price,row.Status,recipe));return id=>service.RecipeSaveAsync(session,row,recipe,id);};
        if(dialog.ShowDialog(this)==DialogResult.OK)await Reload();
    }
    private static void DrawRecipeMaterial(object? sender,DrawItemEventArgs e)
    {
        if(sender is not ComboBox combo)return;
        var selected=(e.State&DrawItemState.Selected)!=0;
        using var fill=new SolidBrush(selected?OrbitManagementTheme.Selected:OrbitManagementTheme.Surface);e.Graphics.FillRectangle(fill,e.Bounds);
        var text=e.Index>=0?combo.GetItemText(combo.Items[e.Index]):"Chọn nguyên liệu…";
        TextRenderer.DrawText(e.Graphics,text,combo.Font,Rectangle.Inflate(e.Bounds,-4,0),OrbitManagementTheme.Text,TextFormatFlags.Left|TextFormatFlags.VerticalCenter|TextFormatFlags.EndEllipsis);
        e.DrawFocusRectangle();
    }
    private async Task Delete()
    {
        if(service is null||session is null||Selected is not ProductRow row)return;
        using var dialog=new OperationEditorDialog("Xóa sản phẩm",id=>service.ReconcileAsync(session,id));dialog.AddNote($"Xóa {row.Code} · {row.Name}; có lịch sử sẽ bị chặn.");dialog.Confirmation=$"Xóa {row.Code}?";dialog.CaptureWrite=()=>id=>service.DeleteAsync(session,row,id);if(dialog.ShowDialog(this)==DialogResult.OK)await Reload();
    }
}






