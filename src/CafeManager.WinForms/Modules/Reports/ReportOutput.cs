using System.Collections.Concurrent;
using System.Data;
using System.Globalization;
using System.Reflection;
using System.Text;
using CafeManager.Business.Security;
using CafeManager.Business.Services;
using CafeManager.Models.Dtos.Reports;
using CafeManager.Models.Requests.Reports;
using CafeManager.Models.States;
using CafeManager.WinForms.Printing;

namespace CafeManager.WinForms.Modules.Reports;

// Dùng chung cho in PDF và xuất CSV: lấy giá trị theo cột, format hiển thị hoặc giá trị thô.
internal static class ReportOutput
{
    private static readonly ConcurrentDictionary<(Type Type, string Field), PropertyInfo?> Properties = new();

    public static object? Value(IReportRow row, string field) =>
        Properties.GetOrAdd((row.GetType(), field), key => key.Type.GetProperty(key.Field))?.GetValue(row);

    public static string Display(IReportRow row, ReportColumn column, ReportKind kind)
    {
        var value = Value(row, column.Field);
        if (value is null) return "—";
        if (column.Field == "Coverage" && value is string coverage) return ReportLabels.Text(coverage);
        if (column.Field == "Name" && kind == ReportKind.SalesChannel && value is string channel) return SalesStates.Label(channel);
        if (column.Field == "Name" && kind == ReportKind.CashFlow && row.Code == "Sales") return L.T("Thu bán hàng");
        return column.Kind switch
        {
            ReportValueKind.Money => PrintFormat.Money((decimal)value),
            ReportValueKind.Quantity or ReportValueKind.Hours => PrintFormat.Quantity((decimal)value),
            ReportValueKind.Integer => PrintFormat.Integer((int)value),
            ReportValueKind.Date => PrintFormat.DateTime(value is DateTime date ? date : null),
            _ => Convert.ToString(value, PrintFormat.Vi) ?? string.Empty,
        };
    }

    // CSV giữ số thô (không ký hiệu tiền) để mở bằng Excel/tính toán lại được.
    public static string Raw(IReportRow row, ReportColumn column)
    {
        var value = Value(row, column.Field);
        if (value is null) return string.Empty;
        return column.Kind switch
        {
            ReportValueKind.Money or ReportValueKind.Quantity or ReportValueKind.Hours =>
                ((decimal)value).ToString(CultureInfo.InvariantCulture),
            ReportValueKind.Integer => ((int)value).ToString(CultureInfo.InvariantCulture),
            ReportValueKind.Date => ((DateTime)value).ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture),
            _ => Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty,
        };
    }

    public static DataTable Table(ReportKind kind, IReadOnlyList<IReportRow> rows)
    {
        var columns = ReportColumns.For(kind);
        var table = new DataTable("Rows");
        foreach (var column in columns) table.Columns.Add(column.Field, typeof(string));
        foreach (var row in rows)
        {
            var values = columns.Select(column => (object)Display(row, column, kind)).ToArray();
            table.Rows.Add(values);
        }
        return table;
    }

    public static async Task<(IReadOnlyList<IReportRow> Rows, bool Complete)> LoadAsync(
        ReportService service, SessionContext session, ReportFilter applied, ReportPage first, int cap, CancellationToken token)
    {
        if (applied.Skip == 0 && first.Rows.Count >= first.Metadata.TotalCount) return (first.Rows, true);
        var rows = new List<IReportRow>(cap);
        var skip = 0;
        while (skip < first.Metadata.TotalCount && rows.Count < cap)
        {
            token.ThrowIfCancellationRequested();
            var page = skip == 0 && applied.Skip == 0
                ? first
                : await service.ReadAsync(session, applied with { Skip = skip }, token);
            rows.AddRange(page.Rows.Take(cap - rows.Count));
            if (page.Rows.Count == 0) break;
            skip += page.Rows.Count;
        }
        return (rows, rows.Count >= first.Metadata.TotalCount);
    }

    public static void WriteCsv(string path, ReportPage page, IReadOnlyList<IReportRow> rows, bool complete)
    {
        var columns = ReportColumns.For(page.Metadata.Kind);
        var builder = new StringBuilder();
        void Line(params string[] fields) =>
            builder.AppendLine(string.Join(";", fields.Select(Escape)));

        Line(L.T("Báo cáo"), ReportsControl.Label(page.Metadata.Kind));
        Line(L.T("Kỳ"), $"{page.Metadata.From:dd/MM/yyyy} - {page.Metadata.To:dd/MM/yyyy}");
        Line(L.T("Cơ sở ngày"), ReportLabels.Text(page.Metadata.DateBasis));
        Line(L.T("Đọc lúc"), PrintFormat.DateTime(page.Metadata.ReadAt));
        Line(L.T("Số dòng"), complete ? page.Metadata.TotalCount.ToString(CultureInfo.InvariantCulture)
            : L.F("{0}/{1} (giới hạn xuất)", rows.Count, page.Metadata.TotalCount));
        Line(L.T("Ghi chú"), ReportLabels.Text(page.Metadata.Note));
        builder.AppendLine();
        Line(columns.Select(column => column.Header).ToArray());
        foreach (var row in rows) Line(columns.Select(column => Raw(row, column)).ToArray());
        builder.AppendLine();
        Line(L.T("Tổng"), ReportsControl.Totals(page));

        File.WriteAllText(path, builder.ToString(), new UTF8Encoding(true));
    }

    private static string Escape(string value) =>
        value.Contains(';') || value.Contains('"') || value.Contains('\n') || value.Contains('\r')
            ? "\"" + value.Replace("\"", "\"\"") + "\""
            : value;
}
