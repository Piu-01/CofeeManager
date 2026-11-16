namespace CafeManager.Business.Services;

// Ước tính nguyên VND, tháng cuối bù phần dư; không tạo bút toán hoặc phiếu chi.
public static class AssetDepreciationCalculator
{
    public static (decimal Monthly, decimal Accumulated) Calculate(decimal value, int months, DateOnly purchasedOn, DateOnly asOf)
    {
        if (value <= 0 || value > 999999999999999999m || decimal.Truncate(value) != value || months <= 0)
            throw new ArgumentException(L.T("Nguyên giá/thời gian khấu hao không hợp lệ."));
        var elapsed = Math.Max(0L, (long)(asOf.Year - purchasedOn.Year) * 12 + asOf.Month - purchasedOn.Month + 1);
        var basis = decimal.Floor(value / months);
        var accumulated = elapsed >= months ? value : basis * elapsed;
        var monthly = elapsed == 0 || elapsed > months ? 0 : elapsed == months ? value - basis * (months - 1L) : basis;
        return (monthly, accumulated);
    }
}
