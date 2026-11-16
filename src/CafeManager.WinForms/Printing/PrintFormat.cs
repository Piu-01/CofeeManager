using System.Globalization;

namespace CafeManager.WinForms.Printing;

// Một nguồn định dạng cho hóa đơn, báo cáo PDF và CSV để không lệch số giữa các đầu ra.
internal static class PrintFormat
{
    public static readonly CultureInfo Vi = CultureInfo.GetCultureInfo("vi-VN");

    public static string Money(decimal value) => value.ToString("#,##0", Vi) + " ₫";
    public static string PlainMoney(decimal value) => value.ToString("#,##0", Vi);
    public static string Quantity(decimal value) => value.ToString("#,##0.####", Vi);
    public static string Integer(int value) => value.ToString("#,##0", Vi);
    public static string DateTime(DateTime? value) => value?.ToString("dd/MM/yyyy HH:mm", Vi) ?? "—";
    public static string Date(DateTime? value) => value?.ToString("dd/MM/yyyy", Vi) ?? "—";
    public static string DateTime(DateTime value) => value.ToString("dd/MM/yyyy HH:mm", Vi);
}
