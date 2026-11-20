using CafeManager.Business.Services;

namespace CafeManager.WinForms.Forms;

public partial class StartupForm : Form
{
    private readonly TableStatusService? service;
    private CancellationTokenSource? loadCancellation;
    private int requestVersion;

    // Designer never opens SQL connections.
    public StartupForm()
    {
        InitializeComponent();
        Theme.AppTheme.Apply(this);
        header.BackColor = Theme.AppTheme.Espresso;
        titleLabel.BackColor = subtitleLabel.BackColor = Color.Transparent;
        titleLabel.Font = Theme.AppTheme.TitleFont; subtitleLabel.ForeColor = Theme.AppTheme.Muted;
        toolbar.BackColor = filter.BackColor = Theme.AppTheme.Surface;
        areaLabel.BackColor = Theme.AppTheme.Surface;
        Theme.AppTheme.Status(statusLabel);
        Text = "CoffeeManager · G01";
        tableGrid.AutoGenerateColumns = false;
        loadButton.EnabledChanged += (_, _) => UpdateButtonAppearance();
        cancelButton.EnabledChanged += (_, _) => UpdateButtonAppearance();
        gridPanel.SizeChanged += (_, _) => UpdateEmptyStateSize();
        UpdateButtonAppearance();
        UpdateEmptyStateSize();
    }

    public StartupForm(TableStatusService? service, string? configurationError) : this()
    {
        this.service = service;
        statusLabel.Text = configurationError ?? "Sẵn sàng tải trạng thái bàn (mẫu nền G01, chưa đăng nhập).";
        loadButton.Enabled = service is not null;
        SetContentState(service is null
            ? "Chưa kết nối dữ liệu\r\n\r\nThiết lập kết nối bằng tài khoản ứng dụng riêng để xem danh sách bàn."
            : "Chưa tải dữ liệu\r\n\r\nChọn khu vực hoặc để trống, rồi nhấn Tải trạng thái bàn.");
    }

    private void UpdateButtonAppearance()
    {
        Theme.AppTheme.PrimaryButton(loadButton);
        Theme.AppTheme.SecondaryButton(cancelButton);
    }

    private void SetContentState(string message)
    {
        emptyStateLabel.Text = message;
        var isEmpty = tableGrid.RowCount == 0;
        emptyStateLabel.Visible = isEmpty;
        tableGrid.Visible = !isEmpty;
        gridPanel.AutoScroll = isEmpty;
        UpdateEmptyStateSize();
    }

    private void UpdateEmptyStateSize()
    {
        if (gridPanel.IsDisposed || emptyStateLabel.IsDisposed) return;
        var width = Math.Max(1, gridPanel.ClientSize.Width - gridPanel.Padding.Horizontal);
        emptyStateLabel.Height = emptyStateLabel.GetPreferredSize(new Size(width, 0)).Height;
    }

    private void ShowCanceledLoad()
    {
        statusLabel.Text = "Đã hủy tải; dữ liệu cũ chưa cập nhật.";
        SetContentState("Đã hủy tải\r\n\r\nNhấn Tải trạng thái bàn khi bạn muốn thử lại.");
    }

    private async void LoadButton_Click(object? sender, EventArgs e)
    {
        if (service is null) return;
        if (areaInput.Text.Trim().Length > 5)
        {
            validation.SetError(areaInput, "Mã khu vực tối đa 5 ký tự.");
            areaInput.Focus();
            return;
        }
        validation.Clear();
        loadCancellation?.Cancel();
        var cancellation = new CancellationTokenSource();
        loadCancellation = cancellation;
        var version = ++requestVersion;
        loadButton.Enabled = false;
        cancelButton.Enabled = true;
        statusLabel.Text = "Đang tải…";
        SetContentState("Đang tải danh sách bàn…\r\n\r\nBạn có thể hủy tải; dữ liệu cũ được giữ nguyên.");
        try
        {
            var result = await service.LoadAsync(areaInput.Text, cancellation.Token);
            if (IsDisposed || Disposing || version != requestVersion) return;
            if (cancellation.IsCancellationRequested)
            {
                ShowCanceledLoad();
                return;
            }
            if (result.Error is { } error)
            {
                statusLabel.Text = $"Chưa cập nhật: {error.Message} (mã {error.Number}).";
                SetContentState("Chưa tải được dữ liệu\r\n\r\nKiểm tra thông báo kết nối và thử tải lại.");
                return;
            }
            if (result.Rows is not { } rows)
            {
                statusLabel.Text = "Chưa cập nhật: phản hồi dữ liệu không hợp lệ; thử tải lại.";
                SetContentState("Phản hồi dữ liệu không hợp lệ\r\n\r\nThử tải lại để cập nhật danh sách bàn.");
                return;
            }
            tableGrid.DataSource = rows.ToList();
            statusLabel.Text = rows.Count == 0 ? "Không có bàn trong khu vực đã chọn." : $"Đã tải {rows.Count} bàn lúc {DateTime.Now:HH:mm}.";
            contentHeading.Text = rows.Count == 0 ? "Danh sách bàn" : $"Danh sách bàn · {rows.Count} bàn";
            SetContentState("Không có bàn trong khu vực này\r\n\r\nThử mã khu vực khác hoặc để trống để xem tất cả.");
        }
        catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
        {
            if (!IsDisposed && !Disposing && version == requestVersion)
            {
                ShowCanceledLoad();
            }
        }
        catch (ArgumentException)
        {
            if (!IsDisposed && !Disposing && version == requestVersion)
            {
                validation.SetError(areaInput, "Mã khu vực cần tối đa 5 ký tự ASCII.");
                statusLabel.Text = "Kiểm tra mã khu vực; dữ liệu cũ chưa cập nhật.";
            }
        }
        finally
        {
            if (!IsDisposed && !Disposing && version == requestVersion)
            {
                loadButton.Enabled = true;
                cancelButton.Enabled = false;
            }
            if (ReferenceEquals(loadCancellation, cancellation)) loadCancellation = null;
            cancellation.Dispose();
        }
    }

    private void CancelButton_Click(object? sender, EventArgs e) => loadCancellation?.Cancel();

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        base.OnFormClosing(e);
        if (!e.Cancel) { ++requestVersion; loadCancellation?.Cancel(); }
    }
}
