using CafeManager.WinForms.Theme;

namespace CafeManager.WinForms.UI;

// Mỗi page giữ heading/primary riêng; tab chung nằm dưới heading của page đang chọn.
public sealed class OrbitManagementTabs : UserControl
{
    private readonly OrbitSectionTabs tabs;
    private readonly List<OrbitRecordListControl> pages = [];
    public OrbitManagementTabs(bool underline = false)
    {
        Dock = DockStyle.Fill; BackColor = OrbitManagementTheme.Background;
        tabs = new OrbitSectionTabs { Underline = underline };
        tabs.SelectedIndexChanged += (_, _) => ShowPage();
    }
    public void AddPage(string caption, OrbitRecordListControl page)
    {
        page.Dock = DockStyle.Fill; page.Visible = false; pages.Add(page); Controls.Add(page);
        tabs.Add(caption, tabs.Underline ? pages.Count == 1 ? "armchair" : "pin" : ""); ShowPage();
    }
    private void ShowPage()
    {
        if (tabs.SelectedIndex < 0 || tabs.SelectedIndex >= pages.Count) return;
        for (var i = 0; i < pages.Count; i++) if (i != tabs.SelectedIndex) pages[i].Visible = false;
        var page = pages[tabs.SelectedIndex]; page.PageWorkspace.Tabs.Controls.Add(tabs);
        page.Visible = true; page.BringToFront(); page.PageWorkspace.PerformLayout();
        if (FindForm() is Forms.MainForm main) main.SetWorkspaceTitle(page.PageWorkspace.Title.Text);
    }
    protected override void Dispose(bool disposing) { if (disposing && !tabs.IsDisposed) tabs.Dispose(); base.Dispose(disposing); }
}
