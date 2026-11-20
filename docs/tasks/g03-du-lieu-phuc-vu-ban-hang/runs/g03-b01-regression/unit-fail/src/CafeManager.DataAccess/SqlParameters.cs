using System.Data;
using Microsoft.Data.SqlClient;

namespace CafeManager.DataAccess;

internal static class SqlParameters
{
    public static string? OptionalText(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    public static SqlParameter Text(string name, SqlDbType type, int size, string? value, bool required = false)
    {
        value = OptionalText(value);
        if ((required && value is null) || value?.Length > size)
            throw new ArgumentException($"Giá trị {name} bắt buộc hoặc vượt độ dài {size}.");
        if (type is SqlDbType.Char or SqlDbType.VarChar && value is not null && value.Any(c => c > 127))
            throw new ArgumentException($"Giá trị {name} cần ký tự ASCII theo contract.");
        return new SqlParameter(name, type, size) { Value = (object?)value ?? DBNull.Value };
    }

    public static SqlParameter Decimal(string name, decimal value, byte scale = 0)
    {
        if (scale is not (0 or 4)) throw new ArgumentOutOfRangeException(nameof(scale));
        decimal limit = scale == 0 ? 999999999999999999m : 99999999999999.9999m;
        if (value < -limit || value > limit || decimal.Round(value, scale) != value)
            throw new ArgumentOutOfRangeException(name, "Giá trị vượt decimal(18,scale), không tự làm tròn.");
        return new SqlParameter(name, SqlDbType.Decimal) { Precision = 18, Scale = scale, Value = value };
    }

    public static SqlParameter DateTime(string name, System.DateTime? value)
    {
        if (value is { } date && date.Ticks % TimeSpan.TicksPerSecond != 0)
            throw new ArgumentException("datetime2(0) không nhận phần dưới giây.", name);
        return new SqlParameter(name, SqlDbType.DateTime2) { Scale = 0, Value = (object?)value ?? DBNull.Value };
    }
}
