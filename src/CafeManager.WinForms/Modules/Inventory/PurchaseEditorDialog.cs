using System.ComponentModel;
using System.Globalization;
using System.Text;
using System.Text.Json;
using CafeManager.Business.Security;
using CafeManager.Business.Services;
using CafeManager.Models.Dtos.Inventory;
using CafeManager.Models.Requests.Inventory;
using CafeManager.WinForms.Forms;
using CafeManager.WinForms.Printing;
using CafeManager.WinForms.Theme;
using CafeManager.WinForms.UI;
namespace CafeManager.WinForms.Modules.Inventory;

// Lập phiếu đặt hàng: chỉ ghi phiếu Chờ nhận, không cộng tồn và không tạo chi/nợ.
public sealed class PurchaseEditorDialog : OrbitDialogForm
{
    private sealed class Draft
    {
        public string Code{get;set;}=""; public string Name{get;set;}="";public string Unit{get;set;}="";
        public decimal Stock{get;set;} public decimal Quantity{get;set;} public decimal Price{get;set;}
        public decimal Amount=>Quantity*Price;
    }
    private static readonly CultureInfo Vi=G05Display.Culture;
    private readonly PurchaseService service;private readonly SessionContext session;private readonly IPendingOperationStore pending;
    private readonly ComboBox supplier=new(){DropDownStyle=ComboBoxStyle.DropDownList};
    private readonly OrbitDateField date=new(),dueDate=new();
    private readonly CheckBox dueEnabled=new(){Text=L.T("Có hạn"),AutoSize=true,BackColor=OrbitManagementTheme.Surface,ForeColor=OrbitManagementTheme.Muted,Font=OrbitManagementTheme.Table,Margin=new Padding(0,0,8,0)};
    private readonly TextBox immediate=new(){Text="0",TextAlign=HorizontalAlignment.Right};
    private readonly OrbitDataGridView grid=new(){Dock=DockStyle.Fill,AutoGenerateColumns=false,AllowUserToAddRows=false,ReadOnly=false,EditMode=DataGridViewEditMode.EditOnEnter};
    private readonly OrbitSearchBox finder=new(){Height=40,Margin=new Padding(0,0,8,0),Dock=DockStyle.Fill};
    private readonly ListBox results=new(){Visible=false,IntegralHeight=false,DrawMode=DrawMode.OwnerDrawFixed,BorderStyle=BorderStyle.FixedSingle,BackColor=OrbitManagementTheme.Input,ForeColor=OrbitManagementTheme.Text,Font=OrbitManagementTheme.Table,TabStop=false};
    private readonly OrbitButton addMaterial=new(){Text=L.T("Thêm")};
    private readonly BindingList<Draft> lines=[]; private readonly Label status=new(){AutoSize=true};
    private readonly Label lineCount=new(){AutoSize=true,Font=OrbitManagementTheme.Small,ForeColor=OrbitManagementTheme.Muted,Margin=new Padding(10,10,0,0),TextAlign=ContentAlignment.MiddleLeft};
    private readonly Label empty=new(){Dock=DockStyle.Fill,BackColor=OrbitManagementTheme.Input,ForeColor=OrbitManagementTheme.Muted,Font=OrbitManagementTheme.Table,TextAlign=ContentAlignment.MiddleCenter,Text=L.T("Chưa có nguyên liệu")+"\r\n"+L.T("Gõ mã hoặc tên ở ô tìm phía trên để thêm dòng.")};
    private readonly Label totalValue=new(){AutoSize=false,Width=140,TextAlign=ContentAlignment.MiddleRight,ForeColor=OrbitManagementTheme.Text,Font=OrbitManagementTheme.Body};
    private readonly Label paidValue=new(){AutoSize=false,Width=140,TextAlign=ContentAlignment.MiddleRight,ForeColor=OrbitManagementTheme.Text,Font=OrbitManagementTheme.Body};
    private readonly Label debtValue=new(){AutoSize=false,Width=140,TextAlign=ContentAlignment.MiddleRight,ForeColor=OrbitManagementTheme.Muted,Font=OrbitManagementTheme.Body};
    private readonly TableLayoutPanel body=new(){Dock=DockStyle.Fill,ColumnCount=1,RowCount=1,BackColor=OrbitManagementTheme.Background};
    private readonly OrbitButton save=new(){Text=L.T("Lưu phiếu chờ nhận"),IsPrimary=true},check=new(){Text=L.T("Kiểm tra kết quả"),Visible=false},reprint=new(){Text=L.T("In phiếu"),Visible=false};
    private List<MaterialPurchaseLookup> materials=[];
    private PurchaseRequest? request;private PurchaseRow? savedRow;private bool busy,unknown,dirty,loadFailed;private readonly ErrorProvider errors=new();
    internal string? ResultCode{get;private set;}
    public PurchaseEditorDialog(PurchaseService service,SessionContext session,IPendingOperationStore pending)
    {
        this.service=service;this.session=session;this.pending=pending;
        Text=L.T("Lập phiếu nhập — Chờ nhận");AutoScaleDimensions=new SizeF(96,96);AutoScaleMode=AutoScaleMode.Dpi;ClientSize=new Size(1060,700);MinimumSize=new Size(760,500);errors.ContainerControl=this;
        dueDate.Value=DateTime.Today.AddDays(7);
        immediate.ReadOnly=session.Identity.Role!="ADMIN";
        var meta=new TableLayoutPanel{Dock=DockStyle.Fill,AutoSize=true,AutoSizeMode=AutoSizeMode.GrowAndShrink,ColumnCount=4,RowCount=1,BackColor=OrbitManagementTheme.Surface,Padding=new Padding(14,8,14,8),Margin=Padding.Empty};
        for(var i=0;i<4;i++)meta.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,25));
        meta.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        var supplierField=new OrbitDialogField(L.T("Nhà cung cấp")+" *",supplier){Margin=new Padding(0,0,12,0)};
        var dateField=new OrbitDialogField(L.T("Ngày nhập"),date){Margin=new Padding(0,0,12,0)};
        var dueHost=new TableLayoutPanel{Dock=DockStyle.Top,Height=40,ColumnCount=2,RowCount=1,Margin=Padding.Empty};
        dueHost.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));dueHost.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));
        dueHost.RowStyles.Add(new RowStyle(SizeType.Percent,100));
        dueEnabled.Anchor=AnchorStyles.Left;dueHost.Controls.Add(dueEnabled,0,0);
        dueDate.Dock=DockStyle.Fill;dueDate.Margin=new Padding(4,0,0,0);dueHost.Controls.Add(dueDate,1,0);
        var dueField=new OrbitDialogField(L.T("Hạn trả dự kiến"),dueHost){Margin=new Padding(0,0,12,0)};
        var immediateField=new OrbitDialogField(immediate.ReadOnly?L.T("Trả ngay (₫) · quản lý xử lý"):L.T("Trả ngay (₫)"),immediate){Margin=Padding.Empty};
        meta.Controls.Add(supplierField,0,0);meta.Controls.Add(dateField,1,0);meta.Controls.Add(dueField,2,0);meta.Controls.Add(immediateField,3,0);

        foreach(var col in new[]{("Code",L.T("MÃ NVL")),("Name",L.T("NGUYÊN LIỆU")),("Unit",L.T("ĐVT")),("Stock",L.T("TỒN")),("Quantity",L.T("SỐ LƯỢNG")),("Price",L.T("ĐƠN GIÁ (₫)")),("Amount",L.T("THÀNH TIỀN"))})
        {
            var readOnly=col.Item1 is "Code" or "Name" or "Unit" or "Stock" or "Amount";
            grid.Columns.Add(new DataGridViewTextBoxColumn{Name=col.Item1,DataPropertyName=col.Item1,HeaderText=col.Item2,ReadOnly=readOnly,AutoSizeMode=DataGridViewAutoSizeColumnMode.AllCells});
        }
        grid.Columns.Insert(0,new DataGridViewTextBoxColumn{Name="No",HeaderText="#",ReadOnly=true,AutoSizeMode=DataGridViewAutoSizeColumnMode.None,Width=44});
        grid.Columns["Name"]!.AutoSizeMode=DataGridViewAutoSizeColumnMode.Fill;
        grid.Columns.Add(new DataGridViewTextBoxColumn{Name="Delete",HeaderText="",ReadOnly=true,AutoSizeMode=DataGridViewAutoSizeColumnMode.None,Width=44});
        foreach(var name in new[]{"Stock","Quantity","Price","Amount"}){grid.Columns[name]!.DefaultCellStyle.Alignment=DataGridViewContentAlignment.MiddleRight;grid.Columns[name]!.DefaultCellStyle.Format=name is "Stock" or "Quantity"?"0.####":"N0";}
        grid.DataSource=lines;
        grid.CellFormatting+=(_,e)=>{if(e.RowIndex>=0&&e.ColumnIndex>=0&&grid.Columns[e.ColumnIndex].Name=="No"){e.Value=e.RowIndex+1;e.FormattingApplied=true;}};
        grid.CellValidating+=(_,e)=>ValidateCell(e);
        grid.CellValidated+=(_,e)=>{if(e.RowIndex>=0)grid.Rows[e.RowIndex].ErrorText="";};
        grid.DataError+=(_,e)=>{e.ThrowException=false;status.Text=L.T("Số liệu trong ô chưa hợp lệ; sửa trước khi lưu.");};
        grid.CellValueChanged+=(_,e)=>{if(e.RowIndex>=0){dirty=true;grid.InvalidateRow(e.RowIndex);}UpdateTotals();};
        var hoveredDelete=-1;
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
        grid.CellContentClick+=(_,e)=>{if(e.RowIndex<0||grid.Columns[e.ColumnIndex].Name!="Delete")return;if(grid.Rows[e.RowIndex].DataBoundItem is not Draft row)return;grid.EndEdit();lines.Remove(row);dirty=true;UpdateTotals();grid.Focus();};

        var gridHost=new Panel{Dock=DockStyle.Fill,BackColor=OrbitManagementTheme.Input,Padding=new Padding(1),Margin=new Padding(0,10,0,10)};
        gridHost.Controls.Add(grid);gridHost.Controls.Add(empty);
        var titleRow=new TableLayoutPanel{Dock=DockStyle.Fill,AutoSize=true,ColumnCount=2,RowCount=1,Margin=new Padding(0,0,0,10)};
        titleRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));titleRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        titleRow.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        var titleFlow=new FlowLayoutPanel{AutoSize=true,AutoSizeMode=AutoSizeMode.GrowAndShrink,FlowDirection=FlowDirection.LeftToRight,WrapContents=false,Anchor=AnchorStyles.Left,BackColor=OrbitManagementTheme.Surface};
        titleFlow.Controls.Add(new Label{Text=L.T("Nguyên liệu nhập"),AutoSize=true,Font=OrbitManagementTheme.CardTitle,ForeColor=OrbitManagementTheme.Text,Margin=new Padding(0,0,0,0)});
        titleFlow.Controls.Add(lineCount);titleRow.Controls.Add(titleFlow,0,0);
        titleRow.Controls.Add(new Label{Text=L.T("Đơn giá tự điền theo lần nhập gần nhất — sửa được"),AutoSize=true,Font=OrbitManagementTheme.Small,ForeColor=OrbitManagementTheme.Muted,Anchor=AnchorStyles.Right},1,0);
        var findRow=new TableLayoutPanel{Dock=DockStyle.Fill,AutoSize=true,ColumnCount=2,RowCount=1,Margin=Padding.Empty};
        findRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));findRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        findRow.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        finder.Input.PlaceholderText=L.T("Tìm mã hoặc tên nguyên liệu…");
        OrbitManagementTheme.Button(addMaterial,ManagementButtonStyle.Secondary,"plus");
        addMaterial.Anchor=AnchorStyles.None;addMaterial.Margin=new Padding(0,0,0,0);
        var finderHost=new Panel{Dock=DockStyle.Fill,Height=40,BackColor=OrbitManagementTheme.Surface,Margin=Padding.Empty};finderHost.Controls.Add(finder);
        findRow.Controls.Add(finderHost,0,0);findRow.Controls.Add(addMaterial,1,0);
        var foot=new TableLayoutPanel{Dock=DockStyle.Fill,AutoSize=true,ColumnCount=2,RowCount=1,Margin=new Padding(0,10,0,0)};
        foot.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));foot.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        foot.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        var note=new Label{Text=L.T("Phiếu nội bộ, không phải hóa đơn GTGT. Kho chỉ cộng sau khi chốt nhận."),AutoSize=true,Font=OrbitManagementTheme.Small,ForeColor=OrbitManagementTheme.Muted,Anchor=AnchorStyles.Left,MaximumSize=new Size(520,0)};
        var totals=new TableLayoutPanel{AutoSize=true,AutoSizeMode=AutoSizeMode.GrowAndShrink,ColumnCount=2,RowCount=3,Anchor=AnchorStyles.Right,BackColor=OrbitManagementTheme.Surface};
        totals.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));totals.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        void AddTotal(string caption,Label value){var row=totals.RowCount;var label=new Label{Text=caption,AutoSize=true,Font=OrbitManagementTheme.Small,ForeColor=OrbitManagementTheme.Muted,Margin=new Padding(0,0,12,4),TextAlign=ContentAlignment.MiddleRight};value.Margin=new Padding(0,0,0,4);totals.RowStyles.Add(new RowStyle(SizeType.AutoSize));totals.Controls.Add(label,0,row);totals.Controls.Add(value,1,row);}
        AddTotal(L.T("Tổng nhập"),totalValue);AddTotal(L.T("Trả ngay"),paidValue);AddTotal(L.T("Còn nợ dự kiến"),debtValue);
        foot.Controls.Add(note,0,0);foot.Controls.Add(totals,1,0);
        var linesCard=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=1,RowCount=4,BackColor=OrbitManagementTheme.Surface,Padding=new Padding(14),Margin=Padding.Empty};
        linesCard.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));
        linesCard.RowStyles.Add(new RowStyle(SizeType.AutoSize));linesCard.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        linesCard.RowStyles.Add(new RowStyle(SizeType.Percent,100));linesCard.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        linesCard.Controls.Add(titleRow,0,0);linesCard.Controls.Add(findRow,0,1);linesCard.Controls.Add(gridHost,0,2);linesCard.Controls.Add(foot,0,3);
        body.RowStyles.Add(new RowStyle(SizeType.Percent,100));
        body.Controls.Add(linesCard,0,0);

        var close=new OrbitButton{Text=L.T("Quay lại")};close.Click+=(_,_)=>Close();var actions=new FlowLayoutPanel{AutoSize=true};actions.Controls.AddRange([check,reprint,save,close]);
        var dialogLayout=new OrbitDialogLayout(this,body,status,actions,BuildHeader(meta));Controls.Add(dialogLayout);Controls.Add(results);results.BringToFront();body.BackColor=OrbitManagementTheme.Background;dialogLayout.EnableResponsiveActions(this,true);OrbitField.ApplyInputBehavior(this);OrbitManagementTheme.DialogGrid(grid);
        void SizeIndex(){if(grid.Columns["No"] is{ } column){column.MinimumWidth=OrbitManagementTheme.Px(grid,40);column.Width=OrbitManagementTheme.Px(grid,44);}}
        grid.HandleCreated+=(_,_)=>SizeIndex();grid.DataBindingComplete+=(_,_)=>SizeIndex();grid.DpiChangedAfterParent+=(_,_)=>SizeIndex();
        foreach(Control c in new Control[]{supplier,immediate}){if(c is TextBox txt)txt.TextChanged+=(_,_)=>{dirty=true;UpdateTotals();};else if(c is ComboBox cb)cb.SelectedIndexChanged+=(_,_)=>dirty=true;}
        date.ValueChanged+=(_,_)=>{dirty=true;UpdateTotals();};dueDate.ValueChanged+=(_,_)=>dirty=true;
        dueEnabled.CheckedChanged+=(_,_)=>{dirty=true;UpdateTotals();};
        immediate.Leave+=(_,_)=>{if(decimal.TryParse(immediate.Text,NumberStyles.Number,Vi,out var cash))immediate.Text=cash.ToString("N0",Vi);UpdateTotals();};
        results.ItemHeight=OrbitManagementTheme.Px(this,36);results.DrawItem+=DrawResult;results.MouseDown+=(_,e)=>{var index=results.IndexFromPoint(e.Location);if(index>=0&&results.Items[index] is MaterialPurchaseLookup row)AddMaterial(row);};
        finder.Input.TextChanged+=(_,_)=>UpdateResults();
        finder.Input.KeyDown+=(_,e)=>SearchKeyDown(e);
        finder.Input.Leave+=(_,_)=>{if(IsDisposed||!IsHandleCreated)return;BeginInvoke((Action)(()=>{if(!finder.ContainsFocus&&!results.ContainsFocus&&!addMaterial.ContainsFocus)HideResults();}));};
        addMaterial.Click+=(_,_)=>{if(finder.Input.Text.Trim().Length==0){finder.Input.Focus();return;}var pick=results.Visible?results.SelectedItem as MaterialPurchaseLookup:Matches().FirstOrDefault();if(pick is null){status.Text=L.T("Không tìm thấy nguyên liệu phù hợp. Gõ mã hoặc tên khác.");finder.Input.Focus();return;}AddMaterial(pick);};
        Resize+=(_,_)=>HideResults();
        save.Click+=async(_,_)=>await Save();check.Click+=async(_,_)=>await Reconcile();reprint.Click+=async(_,_)=>await Print();Shown+=async(_,_)=>await Initialize();AcceptButton=null;
    }
    private Control BuildHeader(Control meta)
    {
        var head=new TableLayoutPanel{Dock=DockStyle.Fill,AutoSize=true,ColumnCount=2,RowCount=1,BackColor=OrbitManagementTheme.Background,Margin=new Padding(0,0,0,12)};
        head.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));head.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        head.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        var left=new FlowLayoutPanel{AutoSize=true,AutoSizeMode=AutoSizeMode.GrowAndShrink,FlowDirection=FlowDirection.TopDown,WrapContents=false,Anchor=AnchorStyles.Left,BackColor=OrbitManagementTheme.Background};
        left.Controls.Add(new Label{Text=L.T("PHIẾU NHẬP NỘI BỘ"),AutoSize=true,Font=OrbitManagementTheme.Small,ForeColor=OrbitManagementTheme.Caramel,Margin=new Padding(0,0,0,4)});
        left.Controls.Add(new Label{Text=L.T("Lập phiếu nhập"),AutoSize=true,Font=OrbitManagementTheme.CardTitle,ForeColor=OrbitManagementTheme.Text,Margin=new Padding(0,0,0,4)});
        left.Controls.Add(new Label{Text=L.T("Ghi phiếu để nhân viên đối chiếu khi hàng về; kho chỉ cộng sau khi chốt nhận."),AutoSize=true,Font=OrbitManagementTheme.Small,ForeColor=OrbitManagementTheme.Muted,MaximumSize=new Size(620,0),Margin=Padding.Empty});
        var right=new FlowLayoutPanel{AutoSize=true,AutoSizeMode=AutoSizeMode.GrowAndShrink,FlowDirection=FlowDirection.TopDown,WrapContents=false,Anchor=AnchorStyles.Right,BackColor=OrbitManagementTheme.Background};
        right.Controls.Add(new OrbitBadge{Text=L.T("Chờ nhận · Chưa ghi kho"),AutoSize=false,Size=new Size(260,30),Font=OrbitManagementTheme.Small,ForeColor=OrbitManagementTheme.Caramel,Margin=new Padding(0,4,0,8)});
        right.Controls.Add(new Label{Text=L.T("Mã phiếu cấp khi lưu"),AutoSize=false,Width=260,TextAlign=ContentAlignment.MiddleRight,Font=OrbitManagementTheme.Small,ForeColor=OrbitManagementTheme.Muted,Margin=Padding.Empty});
        head.Controls.Add(left,0,0);head.Controls.Add(right,1,0);
        head.Margin=Padding.Empty;meta.Margin=new Padding(0,12,0,0);
        var root=new TableLayoutPanel{Dock=DockStyle.Fill,AutoSize=true,AutoSizeMode=AutoSizeMode.GrowAndShrink,ColumnCount=1,RowCount=2,BackColor=OrbitManagementTheme.Background,Margin=new Padding(0,0,0,12)};
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.Controls.Add(head,0,0);root.Controls.Add(meta,0,1);
        return root;
    }
    private static string Fold(string value)
    {
        var normalized=value.Normalize(NormalizationForm.FormD);var folded=new StringBuilder(normalized.Length);
        foreach(var ch in normalized)if(CharUnicodeInfo.GetUnicodeCategory(ch)!=UnicodeCategory.NonSpacingMark)folded.Append(ch);
        return folded.ToString().Replace('đ','d').Replace('Đ','d').ToLowerInvariant();
    }
    private List<MaterialPurchaseLookup> Matches()
    {
        var terms=Fold(finder.Input.Text).Split(' ',StringSplitOptions.RemoveEmptyEntries);
        if(terms.Length==0)return [];
        return materials.Where(m=>terms.All(t=>Fold(m.Code+" "+m.Name).Contains(t))).Take(8).ToList();
    }
    private void UpdateResults()
    {
        if(!Editable||finder.Input.Text.Trim().Length==0){HideResults();return;}
        var matches=Matches();results.BeginUpdate();results.Items.Clear();foreach(var row in matches)results.Items.Add(row);results.EndUpdate();
        if(results.Items.Count==0){HideResults();return;}
        results.SelectedIndex=0;PositionResults();results.Visible=true;results.BringToFront();
    }
    private void PositionResults()
    {
        var origin=PointToClient(finder.PointToScreen(new Point(0,finder.Height+2)));
        var width=Math.Min(finder.Width+(finder.Margin.Right>0?finder.Margin.Right:8)+addMaterial.Width,ClientSize.Width-origin.X-12);
        results.ItemHeight=OrbitManagementTheme.Px(this,36);
        var height=Math.Min(results.Items.Count,6)*results.ItemHeight+2;
        results.SetBounds(origin.X,origin.Y,Math.Max(240,width),height);
    }
    private void HideResults(){results.Visible=false;}
    private void SearchKeyDown(KeyEventArgs e)
    {
        if(e.KeyCode==Keys.Down){if(!results.Visible){UpdateResults();e.SuppressKeyPress=true;return;}results.SelectedIndex=Math.Min(results.Items.Count-1,results.SelectedIndex+1);e.SuppressKeyPress=true;}
        else if(e.KeyCode==Keys.Up&&results.Visible){results.SelectedIndex=Math.Max(0,results.SelectedIndex-1);e.SuppressKeyPress=true;}
        else if(e.KeyCode==Keys.Enter){var pick=results.Visible?results.SelectedItem as MaterialPurchaseLookup:Matches().FirstOrDefault();if(pick is not null)AddMaterial(pick);else if(finder.Input.Text.Trim().Length>0)status.Text=L.T("Không tìm thấy nguyên liệu phù hợp. Gõ mã hoặc tên khác.");e.SuppressKeyPress=true;}
        else if(e.KeyCode==Keys.Escape){HideResults();e.SuppressKeyPress=true;}
    }
    private void DrawResult(object? sender,DrawItemEventArgs e)
    {
        if(e.Index<0||results.Items[e.Index] is not MaterialPurchaseLookup row)return;
        var selected=(e.State&DrawItemState.Selected)!=0;using(var fill=new SolidBrush(selected?OrbitManagementTheme.Selected:OrbitManagementTheme.Input))e.Graphics.FillRectangle(fill,e.Bounds);
        var r=e.Bounds;int P(int n)=>OrbitManagementTheme.Px(this,n);
        var flags=TextFormatFlags.VerticalCenter|TextFormatFlags.NoPrefix|TextFormatFlags.EndEllipsis;
        TextRenderer.DrawText(e.Graphics,row.Code,OrbitManagementTheme.DrawingFont(this,OrbitManagementTheme.Code),new Rectangle(r.Left+P(10),r.Top,P(70),r.Height),OrbitManagementTheme.Muted,flags|TextFormatFlags.Left);
        var priceWidth=P(110);var metaWidth=P(130);var right=r.Right-P(10);
        var priceRect=new Rectangle(right-priceWidth,r.Top,priceWidth,r.Height);
        var metaRect=new Rectangle(priceRect.Left-metaWidth,r.Top,metaWidth,r.Height);
        var nameRect=new Rectangle(r.Left+P(86),r.Top,Math.Max(P(80),metaRect.Left-r.Left-P(86)-P(8)),r.Height);
        TextRenderer.DrawText(e.Graphics,row.Name,OrbitManagementTheme.DrawingFont(this,OrbitManagementTheme.Table),nameRect,OrbitManagementTheme.Text,flags|TextFormatFlags.Left);
        TextRenderer.DrawText(e.Graphics,L.F("Tồn {0} {1}",row.Stock.ToString("0.####",Vi),row.Unit??""),OrbitManagementTheme.DrawingFont(this,OrbitManagementTheme.Small),metaRect,OrbitManagementTheme.Muted,flags|TextFormatFlags.Right);
        TextRenderer.DrawText(e.Graphics,row.LastPrice is decimal price?price.ToString("N0",Vi)+" ₫":L.T("chưa có"),OrbitManagementTheme.DrawingFont(this,OrbitManagementTheme.Table),priceRect,selected?OrbitManagementTheme.Caramel:OrbitManagementTheme.Text,flags|TextFormatFlags.Right);
    }
    private void AddMaterial(MaterialPurchaseLookup row)
    {
        HideResults();finder.Input.Clear();
        var old=lines.FirstOrDefault(l=>l.Code==row.Code);
        if(old is not null){SelectRow(lines.IndexOf(old),"Quantity");status.Text=L.F("Đã có {0}; cập nhật số lượng.",row.Name);return;}
        lines.Add(new(){Code=row.Code,Name=row.Name,Unit=row.Unit??"",Stock=row.Stock,Quantity=1,Price=row.LastPrice??0});dirty=true;UpdateTotals();SelectRow(lines.Count-1,"Quantity");
        if(row.LastPrice is null)status.Text=L.T("Chưa có giá nhập gần nhất; nhập đơn giá cho dòng mới.");
    }
    private void SelectRow(int index,string column)
    {
        if(index<0||index>=grid.Rows.Count)return;
        grid.CurrentCell=grid.Rows[index].Cells[column];grid.Focus();grid.BeginEdit(true);
    }
    private bool Editable=>!busy&&!unknown&&ResultCode is null;
    private void State(){body.Enabled=!busy&&!unknown&&ResultCode is null;save.Enabled=Editable&&!loadFailed;save.Text=unknown?L.T("Gửi lại cùng yêu cầu"):L.T("Lưu phiếu chờ nhận");check.Visible=unknown;check.Enabled=!busy;reprint.Visible=ResultCode is not null&&savedRow is not null;reprint.Enabled=!busy;if(!Editable)HideResults();closeText();}
    private void closeText(){if(ResultCode is not null)Text=L.T("Đã lưu phiếu ")+ResultCode;}
    private void ValidateCell(DataGridViewCellValidatingEventArgs e)
    {
        if(e.RowIndex<0||e.ColumnIndex<0)return;
        var name=grid.Columns[e.ColumnIndex].DataPropertyName;
        if(name is not("Quantity" or "Price"))return;
        var text=Convert.ToString(e.FormattedValue)??"";
        if(!decimal.TryParse(text,NumberStyles.Number,Vi,out var value)||value<=0||decimal.Round(value,4)!=value)
        {
            e.Cancel=true;grid.Rows[e.RowIndex].ErrorText=name=="Quantity"?L.T("Số lượng phải lớn hơn 0, tối đa 4 số thập phân."):L.T("Đơn giá phải là số nguyên VND dương.");status.Text=grid.Rows[e.RowIndex].ErrorText;
        }
    }
    private void UpdateTotals()
    {
        var total=lines.Sum(l=>l.Quantity*l.Price);
        var cash=decimal.TryParse(immediate.Text,NumberStyles.Number,Vi,out var parsed)&&parsed>=0?decimal.Truncate(parsed):0;
        var debt=Math.Max(0,total-cash);
        totalValue.Text=total.ToString("N0",Vi)+" ₫";paidValue.Text=cash.ToString("N0",Vi)+" ₫";debtValue.Text=debt.ToString("N0",Vi)+" ₫";
        debtValue.ForeColor=debt>0?OrbitManagementTheme.Caramel:OrbitManagementTheme.Muted;
        lineCount.Text=L.F("{0} dòng",lines.Count);empty.Visible=lines.Count==0;
        var canPromise=Editable&&debt>0;dueEnabled.Enabled=canPromise;if(!canPromise)dueEnabled.Checked=false;dueDate.Enabled=canPromise&&dueEnabled.Checked;
    }
    private async Task Initialize()
    {
        busy=true;State();
        try
        {
            supplier.DisplayMember=nameof(InventoryLookup.Name);supplier.ValueMember=nameof(InventoryLookup.Code);supplier.DataSource=await service.LookupsAsync(session,"Supplier");
            materials=await service.MaterialsAsync(session);
            var stored=pending.Read(session.Identity.AccountId,"PurchaseCreate");
            if(stored is not null)
            {
                request=JsonSerializer.Deserialize<PurchaseRequest>(stored.Payload)??throw new InvalidDataException(L.T("Hồ sơ nhập hàng chưa hợp lệ."));
                if(request.RequestId!=stored.RequestId)throw new InvalidDataException(L.T("Mã yêu cầu không khớp."));
                supplier.SelectedItem=((List<InventoryLookup>)supplier.DataSource).FirstOrDefault(s=>s.Code==request.Supplier);date.Value=request.Date;dueEnabled.Checked=request.Due.HasValue;if(request.Due.HasValue)dueDate.Value=request.Due.Value;immediate.Text=request.Immediate.ToString("N0",Vi);
                foreach(var line in request.Lines){var lookup=materials.FirstOrDefault(m=>m.Code==line.Code);lines.Add(new(){Code=line.Code,Name=lookup?.Name??line.Code,Unit=lookup?.Unit??"",Stock=lookup?.Stock??0,Quantity=line.Quantity,Price=line.Price});}
                unknown=true;status.Text=L.F("Yêu cầu {0}: cần đối chiếu trước khi gửi lại.", request.RequestId);
            }
            dirty=false;UpdateTotals();
        }
        catch(Exception e)when(e is UserOperationException or IOException or JsonException or global::System.Security.Cryptography.CryptographicException or ArgumentException){loadFailed=true;status.Text=e.Message;}
        finally{busy=false;State();}
    }
    private List<PurchaseLine> ReadLines(){if(!grid.EndEdit())throw new ArgumentException(L.T("Sửa ô đang nhập trước khi lưu."));var result=lines.Select(l=>new PurchaseLine(l.Code,l.Quantity,l.Price)).ToList();PurchaseService.ValidateLines(result);return result;}
    private async Task Save()
    {
        if(busy||!Editable)return;errors.Clear();
        if(!unknown)
        {
            if(session.Identity.Role!="ADMIN"){status.Text=L.T("Chỉ quản lý được lập phiếu nhập.");return;}
            if(supplier.SelectedItem is not InventoryLookup selected){errors.SetError(supplier,L.T("Chọn nhà cung cấp."));supplier.Focus();return;}
            if(!decimal.TryParse(immediate.Text,NumberStyles.Number,Vi,out var cash)||cash<0||decimal.Truncate(cash)!=cash){errors.SetError(immediate,L.T("Nhập số tiền nguyên VND không âm."));immediate.Focus();return;}
            DateTime inputDate;
            try{inputDate=date.Value.Date;}
            catch(ArgumentException ex){errors.SetError(date,ex.Message);status.Text=ex.Message;date.FocusInput();return;}
            if(inputDate>DateTime.Today){errors.SetError(date,L.T("Ngày nhập không được ở tương lai."));date.FocusInput();return;}
            DateTime? dueInput=null;
            if(dueEnabled.Checked)
            {
                try{dueInput=dueDate.Value.Date;}
                catch(ArgumentException ex){errors.SetError(dueDate,ex.Message);status.Text=ex.Message;dueDate.FocusInput();return;}
            }
            busy=true;State();
            try{var input=ReadLines();var total=await service.PreviewAsync(session,input);if(cash>total)throw new ArgumentException(L.T("Trả ngay vượt tổng nhập."));request=new(Guid.NewGuid(),selected.Code,inputDate,dueInput,cash,total,input);}
            catch(Exception e)when(e is UserOperationException or ArgumentException){status.Text=e.Message;return;}
            finally{busy=false;State();}
        }
        if(request is null)return;
        var totalText=request.ExpectedTotal.ToString("N0",Vi);var cashText=request.Immediate.ToString("N0",Vi);var debtText=(request.ExpectedTotal-request.Immediate).ToString("N0",Vi);
        if(await OrbitConfirmationDialog.ShowAsync(this,L.F("Lưu phiếu {0} dòng, tổng {1} ₫, trả ngay {2} ₫, nợ {3} ₫? Phiếu ở trạng thái Chờ nhận, chưa cộng kho.", request.Lines.Count, totalText, cashText, debtText),L.T("Lưu phiếu chờ nhận"),MessageBoxButtons.YesNo)!=DialogResult.Yes)return;
        await SendRequest();
    }
    private async Task SendRequest()
    {
        if(request is null)return;
        busy=true;State();
        try{pending.Save(new(session.Identity.AccountId,request.RequestId,"PurchaseCreate",JsonSerializer.Serialize(request),DateTime.UtcNow));var code=await service.CreateDraftAsync(session,request);await Complete(code);}
        catch(UserOperationException e){unknown=unknown||e.Error.RequiresReconciliation;status.Text=e.Message;if(!unknown){try{pending.Clear(session.Identity.AccountId,"PurchaseCreate");request=null;}catch(IOException clearError){unknown=true;status.Text=clearError.Message;}}}
        catch(OperationCanceledException){unknown=true;status.Text=L.T("Đã dừng chờ; kiểm tra kết quả trước khi thử lại.");}
        catch(Exception e)when(e is IOException or UnauthorizedAccessException or global::System.Security.Cryptography.CryptographicException){unknown=true;status.Text=e.Message;}
        catch(Exception e){status.Text=e.Message;}
        finally{busy=false;if(!IsDisposed)State();}
    }
    private async Task Complete(string code)
    {
        pending.Clear(session.Identity.AccountId,"PurchaseCreate");ResultCode=code;dirty=false;unknown=false;
        try
        {
            savedRow=await service.GetAsync(session,code);
            status.Text=L.F("Đã lưu phiếu {0} — Chờ nhận. Có thể in phiếu để nhân viên đối chiếu khi giao hàng.", code);
            State();
            if(savedRow is not null&&await OrbitConfirmationDialog.ShowAsync(this,L.F("In phiếu {0} ngay?", code),L.T("In phiếu nhập"),MessageBoxButtons.YesNo)==DialogResult.Yes)await Print();
        }
        catch(Exception e)when(e is UserOperationException or OperationCanceledException){status.Text=e.Message;}
        catch(Exception e){status.Text=e.Message;}
        finally{if(!IsDisposed){DialogResult=DialogResult.OK;Close();}}
    }
    private async Task Print()
    {
        if(savedRow is null||busy)return;busy=true;State();
        try{await PurchasePrinting.PreviewAsync(this,savedRow);}
        catch(Exception e)when(e is not OperationCanceledException){status.Text=L.T("Chưa in được phiếu: ")+e.Message;}
        finally{busy=false;if(!IsDisposed)State();}
    }
    private async Task Reconcile(){if(busy||request is null)return;busy=true;State();try{var code=await service.ReconcileAsync(session,request.RequestId);if(code is not null)await Complete(code);else{busy=false;State();status.Text=L.T("Chưa có receipt; phiên trước có thể còn chạy. Chỉ gửi lại cùng yêu cầu đã lưu.");if(await OrbitConfirmationDialog.ShowAsync(this,L.T("Gửi lại đúng yêu cầu đang lưu, giữ nguyên mã và nội dung?"),L.T("Đối chiếu yêu cầu"),MessageBoxButtons.YesNo)==DialogResult.Yes){await SendRequest();return;}}}catch(Exception e)when(e is UserOperationException or IOException or OperationCanceledException){status.Text=e.Message;}finally{busy=false;if(!IsDisposed)State();}}
    protected override async Task<bool> CanCloseAsync()=>ResultCode is not null||(!busy&&!unknown&&(!dirty||await OrbitConfirmationDialog.ShowAsync(this,L.T("Bỏ nháp phiếu nhập chưa lưu?"),L.T("Bỏ nháp"),MessageBoxButtons.YesNo)==DialogResult.Yes));
    protected override void Dispose(bool disposing){if(disposing)errors.Dispose();base.Dispose(disposing);}
}
