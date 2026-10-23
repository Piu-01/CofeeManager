using CafeManager.WinForms.Theme;

namespace CafeManager.WinForms.UI;

// Khung trình bày danh sách; không sở hữu query hoặc thao tác nghiệp vụ.
public sealed class OrbitManagementLayout : TableLayoutPanel
{
    public TableLayoutPanel Heading { get; } = new() { Dock = DockStyle.Fill, AutoSize = true, ColumnCount = 2, Margin = new Padding(0, 0, 0, 16) };
    public Label Title { get; } = new() { AutoSize = true, Margin = new Padding(0, 0, 0, 6) };
    public Label Description { get; } = new() { AutoSize = true, Margin = Padding.Empty };
    public FlowLayoutPanel PrimaryActions { get; } = Flow();
    public FlowLayoutPanel Filters { get; } = Flow();
    public FlowLayoutPanel RowActions { get; } = Flow();
    public FlowLayoutPanel Pager { get; } = Flow();
    public Label Count { get; } = new() { AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(8), Visible = false };
    public Label Page { get; } = new() { AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(8, 10, 8, 10) };
    public Panel Body { get; } = new() { Dock = DockStyle.Fill, Margin = Padding.Empty };
    private readonly TableLayoutPanel card = new() { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 4, Margin = Padding.Empty, Padding = new Padding(1) };
    private readonly TableLayoutPanel searchRow = Row();
    private bool arranging;
    private readonly List<(TableLayoutPanel Row, FlowLayoutPanel Flow, Control Left)> bars = new();
    private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<Button, ButtonSize> buttonSizes = new();
    private sealed record ButtonSize(int Width, int Height);
    private static FlowLayoutPanel Flow() => new() { AutoSize = false, Dock = DockStyle.Fill, WrapContents = true, Margin = Padding.Empty, Padding = Padding.Empty };
    private static TableLayoutPanel Row()
    {
        var row = new TableLayoutPanel { AutoSize = false, Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, Margin = Padding.Empty, Padding = new Padding(8) };
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100)); row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 0));
        row.RowStyles.Add(new RowStyle(SizeType.Absolute,0));
        return row;
    }
    public OrbitManagementLayout(Control search, Control state)
    {
        Dock = DockStyle.Fill; ColumnCount = 1; RowCount = 3; Padding = new Padding(24); Margin = Padding.Empty;
        ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        RowStyles.Add(new RowStyle(SizeType.AutoSize)); RowStyles.Add(new RowStyle(SizeType.Percent, 100)); RowStyles.Add(new RowStyle(SizeType.AutoSize));
        Heading.AutoSize=false; Heading.Height=80; Heading.RowCount=1;
        Heading.RowStyles.Add(new RowStyle(SizeType.Absolute,0));
        Heading.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100)); Heading.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 200));
        var captions = new TableLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, ColumnCount = 1, RowCount=2, Margin = Padding.Empty };
        captions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));
        captions.RowStyles.Add(new RowStyle(SizeType.AutoSize));captions.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        captions.Controls.Add(Title); captions.Controls.Add(Description); Heading.Controls.Add(captions, 0, 0); Heading.Controls.Add(PrimaryActions, 1, 0);
        search.Dock = DockStyle.Fill; search.Anchor = AnchorStyles.Left | AnchorStyles.Right; search.Margin = new Padding(4, 0, 8, 0);
        if(search is TextBox input){input.AutoSize=false;input.MinimumSize=new Size(120,40);}
        searchRow.Controls.Add(search, 0, 0); searchRow.Controls.Add(Filters, 1, 0);
        search.VisibleChanged += (_,_) =>
        {
            searchRow.SuspendLayout();
            searchRow.SetColumn(Filters,1); searchRow.SetColumnSpan(Filters,1);
            searchRow.ResumeLayout(true);
        };
        var tools = Row(); tools.Controls.Add(Count, 0, 0); tools.Controls.Add(RowActions, 1, 0);
        var footer = Row(); footer.Controls.Add(Page, 0, 0); footer.Controls.Add(Pager, 1, 0);
        Pager.WrapContents=false; PrimaryActions.WrapContents=false;
        bars.Add((Heading,PrimaryActions,captions));bars.Add((searchRow,Filters,search));bars.Add((tools,RowActions,Count));bars.Add((footer,Pager,Page));
        card.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        card.RowStyles.Add(new RowStyle(SizeType.AutoSize)); card.RowStyles.Add(new RowStyle(SizeType.AutoSize)); card.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); card.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        card.Controls.Add(searchRow, 0, 0); card.Controls.Add(tools, 0, 1); card.Controls.Add(Body, 0, 2); card.Controls.Add(footer, 0, 3);
        Controls.Add(Heading, 0, 0); Controls.Add(card, 0, 1); Controls.Add(state, 0, 2);
        state.Margin = Padding.Empty; state.Padding = new Padding(0, 8, 0, 0);
        foreach(var (_,flow,_) in bars)flow.Layout+=(_,_)=>{if(!arranging)PerformLayout();};
        HandleCreated += (_, _) => { Title.Font = AppTheme.TitleFont; Description.ForeColor = AppTheme.Muted; PaintSurface(card); foreach(Button button in PrimaryActions.Controls) AppTheme.PrimaryButton(button); };
        card.Paint += (_, e) => { using var pen = new Pen(AppTheme.Border); e.Graphics.DrawRectangle(pen, 0, 0, card.Width - 1, card.Height - 1); };
    }
    private static void PaintSurface(Control root)
    {
        root.BackColor = AppTheme.Surface;
        foreach (Control child in root.Controls)
        {
            if (child is Panel or TableLayoutPanel or FlowLayoutPanel or Label) PaintSurface(child);
            if (child is DataGridView grid) grid.AlternatingRowsDefaultCellStyle.BackColor=AppTheme.Surface;
        }
    }
    public static void Button(Button button, string icon = "", bool primary = false)
    {
        button.AutoSize = false; button.Padding = Padding.Empty; button.Margin = new Padding(4, 2, 4, 2);
        var wide = button.Text.Length > 5 || button.Text is "Mở sổ" or "Sơ đồ";
        buttonSizes.Remove(button); buttonSizes.Add(button,new ButtonSize(primary ? 200 : wide ? 160 : 88,primary ? 40 : 36));
        NormalizeButton(button);
        button.HandleCreated+=(_,_)=>NormalizeButton(button);
        button.DpiChangedAfterParent+=(_,_)=>NormalizeButton(button);
        if (icon.Length > 0) Icon(button, icon, primary);
        if (primary) AppTheme.PrimaryButton(button); else AppTheme.SecondaryButton(button);
    }
    public static void Icon(Button button,string name,bool primary=false)
    {
        Bitmap? owned=null;
        void RefreshIcon()
        {
            var pixels=20*button.DeviceDpi/96F;var size=pixels<=20?20:pixels<=24?24:32;
            using var stream=typeof(AppTheme).Assembly.GetManifestResourceStream($"CafeManager.WinForms.Resources.Orbit.{name}-{size}.png");
            if(stream is null)return;
            using var source=new Bitmap(stream);var image=new Bitmap(source);
            if(primary)for(var x=0;x<image.Width;x++)for(var y=0;y<image.Height;y++){var color=image.GetPixel(x,y);image.SetPixel(x,y,Color.FromArgb(color.A,AppTheme.OnPrimary));}
            button.Image=image;owned?.Dispose();owned=image;
        }
        RefreshIcon();button.HandleCreated+=(_,_)=>RefreshIcon();button.DpiChangedAfterParent+=(_,_)=>RefreshIcon();button.Disposed+=(_,_)=>owned?.Dispose();
    }
    private static void NormalizeButton(Button button)
    {
        if(!buttonSizes.TryGetValue(button,out var size))return;
        var scale=button.DeviceDpi/96F;
        var target=new Size((int)Math.Round(size.Width*scale),(int)Math.Round(size.Height*scale));
        if(button.Size!=target)button.Size=target;
        button.Margin=new Padding((int)(4*scale),(int)(2*scale),(int)(4*scale),(int)(2*scale));
    }
    protected override void OnLayout(LayoutEventArgs e)
    {
        if(arranging||!Visible){base.OnLayout(e);return;}
        arranging=true;
        try
        {
            var scale=DeviceDpi/96F;
            foreach(var (row,flow,left) in bars)
            {
                foreach(var button in flow.Controls.OfType<Button>())NormalizeButton(button);
                flow.MaximumSize=Size.Empty;
                var children=flow.Controls.Cast<Control>().Where(c=>c.Visible).ToArray();
                var natural=children.Sum(c=>c.Width+c.Margin.Horizontal)+flow.Padding.Horizontal;
                var available=Math.Max(1,ClientSize.Width-Padding.Horizontal-row.Padding.Horizontal);
                var leftWidth=left.Visible?(int)((left is TextBox?240:180)*scale):0;
                var right=Math.Min(natural,Math.Max((int)(100*scale),available-leftWidth));
                if(!flow.WrapContents)right=natural;
                var used=flow.Padding.Left;var lineHeight=0;var height=flow.Padding.Vertical;
                foreach(var child in children)
                {
                    var width=child.Width+child.Margin.Horizontal;
                    if(flow.WrapContents&&used>flow.Padding.Left&&used+width>right-flow.Padding.Right){height+=lineHeight;used=flow.Padding.Left;lineHeight=0;}
                    used+=width;lineHeight=Math.Max(lineHeight,child.Height+child.Margin.Vertical);
                }
                height+=lineHeight;
                row.ColumnStyles[1].Width=right;
                if(left.Visible)height=Math.Max(height,left.GetPreferredSize(new Size(Math.Max(1,available-right),0)).Height+left.Margin.Vertical);
                var rowHeight=height+row.Padding.Vertical;
                row.RowStyles[0].Height=height;
                if(row.Height!=rowHeight)row.Height=rowHeight;
                row.MinimumSize=new Size(0,rowHeight);
            }
            Title.Font=AppTheme.TitleFont;Description.ForeColor=AppTheme.Muted;
            PaintSurface(card);
            foreach(Button button in PrimaryActions.Controls)AppTheme.PrimaryButton(button);
            base.OnLayout(e);
        }
        finally{arranging=false;}
    }
    public static void Columns(DataGridView grid)
    {
        grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None; grid.RowHeadersVisible = false;
        var stretch=grid.Columns.Cast<DataGridViewColumn>().FirstOrDefault(c=>c.DataPropertyName is "Name" or "Reason")??grid.Columns.Cast<DataGridViewColumn>().FirstOrDefault();
        foreach (DataGridViewColumn col in grid.Columns)
        {
            var fill = col==stretch;
            col.AutoSizeMode = fill ? DataGridViewAutoSizeColumnMode.Fill : DataGridViewAutoSizeColumnMode.None;
            col.MinimumWidth = fill ? 180 : 80; col.Width = (int)((col.DataPropertyName is "StartDate" or "EndDate" or "CreatedAt" or "OccurredAt" or "Phone" ? 150 : col.DataPropertyName=="Code"?130:110)*grid.DeviceDpi/96F);
            if (col.DataPropertyName is "Price" or "Quantity" or "Points" or "MinimumPoints" or "Discount") col.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            if(col.DataPropertyName is "Points" or "MinimumPoints") {col.DefaultCellStyle.Format="N0";col.DefaultCellStyle.FormatProvider=System.Globalization.CultureInfo.GetCultureInfo("vi-VN");}
            if(col.DataPropertyName=="Discount")col.DefaultCellStyle.Format="0'%'";
        }
    }
}



