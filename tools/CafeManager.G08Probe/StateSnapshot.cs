using Microsoft.Data.SqlClient;

internal static class StateSnapshot
{
    internal static async Task<object?> Scalar(SqlConnection c, string sql)
    {
        await using var command = c.CreateCommand(); command.CommandText = sql;
        return await command.ExecuteScalarAsync();
    }
    internal static async Task<string> FinancialHash(SqlConnection c) => (string)(await Scalar(c, """
        SELECT CONVERT(varchar(64),HASHBYTES('SHA2_256',CONVERT(varbinary(max),(
        SELECT (SELECT MaHD,TongTien,GiamGia,PhiShip,ThanhTien,TrangThai,NgayThanhToan FROM dbo.HOADON ORDER BY MaHD FOR JSON PATH,INCLUDE_NULL_VALUES) Bills,
        (SELECT * FROM dbo.CHITIET_THANHTOAN ORDER BY MaTT FOR JSON PATH,INCLUDE_NULL_VALUES) Payments,
        (SELECT MaNVL,SoLuongTon FROM dbo.NGUYENVATLIEU ORDER BY MaNVL FOR JSON PATH) Inventory,
        (SELECT MaKH,DiemTichLuy FROM dbo.KHACHHANG ORDER BY MaKH FOR JSON PATH) Points
        FOR JSON PATH,WITHOUT_ARRAY_WRAPPER))),2)
        """))!;
}
