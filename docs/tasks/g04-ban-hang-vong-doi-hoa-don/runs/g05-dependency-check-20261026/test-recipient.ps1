$ErrorActionPreference='Stop'
$root=(Resolve-Path (Join-Path $PSScriptRoot '../../../../..')).Path
. "$root/tools/database/DatabaseTestTools.ps1"
$c=Open-TestConnection 'CoffeeManager_Test_G04_20261025_181739_98e5df59_checkout'
$created=[Collections.Generic.List[string]]::new()
$results=[Collections.Generic.List[object]]::new()
try {
    if((Read-TestScalar $c 'SELECT RunId FROM dbo.G04TestMarker') -cne '20261025_181739_98e5df59'){throw 'Marker mismatch'}
    $financialSql="SELECT (SELECT (SELECT * FROM dbo.CHITIET_THANHTOAN ORDER BY MaTT FOR JSON PATH) Payments,(SELECT * FROM dbo.TIEUHAO_HOADON ORDER BY MaHD,MaNVL FOR JSON PATH) Consumption,(SELECT MaNVL,SoLuongTon FROM dbo.NGUYENVATLIEU ORDER BY MaNVL FOR JSON PATH) Stock,(SELECT MaKH,DiemTichLuy FROM dbo.KHACHHANG ORDER BY MaKH FOR JSON PATH) Points FOR JSON PATH)"
    $before=Read-TestScalar $c $financialSql
    $actor="DECLARE @a int,@s binary(8); SELECT @a=MaTK,@s=SecurityStamp FROM dbo.TAIKHOAN WHERE TenDangNhap='admin' AND HoatDong=1; IF @a IS NULL THROW 50000,'Test admin missing',1; "
    $cases=@(
        @{Code='HD86000001';Name='Recipient spaces';Recipient='   ';Phone='0900000026';Address='Test address'},
        @{Code='HD86000002';Name='Phone empty';Recipient='Test';Phone='';Address='Test address'},
        @{Code='HD86000003';Name='Phone letters';Recipient='Test';Phone='abc';Address='Test address'},
        @{Code='HD86000004';Name='Address spaces';Recipient='Test';Phone='0900000026';Address='   '}
    )
    foreach($case in $cases){
        $code=$case.Code
        if((Read-TestScalar $c "SELECT COUNT(*) FROM dbo.HOADON WHERE MaHD='$code'") -ne 0){throw 'Fixture collision'}
        $cmd=$c.CreateCommand()
        try {
            $cmd.CommandText="INSERT dbo.HOADON(MaHD,MaNV,LoaiDon,TenNguoiNhan,SDTNguoiNhan,DiaChiGiao,TrangThaiGiao) SELECT @code,MaNV,N'Giao hang',@recipient,@phone,@address,N'Cho giao' FROM dbo.TAIKHOAN WHERE TenDangNhap='admin';"
            foreach($pair in @{code=$code;recipient=$case.Recipient;phone=$case.Phone;address=$case.Address}.GetEnumerator()){[void]$cmd.Parameters.AddWithValue('@'+$pair.Key,$pair.Value)}
            [void]$cmd.ExecuteNonQuery();$created.Add($code)
        } finally {$cmd.Dispose()}
        $request=[guid]::NewGuid().ToString()
        Expect-TestSqlError $c ($actor+"DECLARE @r binary(8);SELECT @r=Revision FROM dbo.HOADON WHERE MaHD='$code'; EXEC dbo.sp_ChuyenTrangThaiGiaoHang @a,@s,'$code',@r,N'Cho giao',N'Dang giao','$request';") 52503
        if((Read-TestScalar $c "SELECT COUNT(*) FROM dbo.HOADON WHERE MaHD='$code' AND TrangThaiGiao=N'Cho giao' AND TrangThai=N'Chua thanh toan'") -ne 1){throw 'Invalid recipient changed order'}
        if((Read-TestScalar $c "SELECT COUNT(*) FROM dbo.G03OperationReceipt WHERE RequestId='$request'") -ne 0){throw 'Rejected request left receipt'}
        $results.Add([pscustomobject]@{Case=$case.Name;Outcome='PASS';Error=52503;Transaction='0:0';ReceiptCount=0})
    }
    foreach($channel in @('Tai quan','Mang di')){
        $code=Read-TestScalar $c "SELECT TOP(1) MaHD FROM dbo.HOADON WHERE LoaiDon=N'$channel' ORDER BY MaHD"
        if(-not $code){throw 'Missing channel fixture'}
        Expect-TestSqlError $c ($actor+"EXEC dbo.sp_DeliveryGet @a,@s,'$code';") 52501
        $results.Add([pscustomobject]@{Case='Reject non-delivery '+$channel;Code=$code;Outcome='PASS';Error=52501;Transaction='0:0'})
    }
    if($before -cne (Read-TestScalar $c $financialSql)){throw 'Financial state changed during recipient negative tests'}
    $results | ConvertTo-Json -Depth 8 | Set-Content "$PSScriptRoot/recipient-results.json" -Encoding utf8
    Write-Output '6/6 negative SQL checks PASS; payments/consumption/stock/points unchanged.'
} finally {
    foreach($code in $created){Run-TestSql $c "DELETE FROM dbo.HOADON WHERE MaHD='$code' AND MaHD IN ('HD86000001','HD86000002','HD86000003','HD86000004') AND LoaiDon=N'Giao hang' AND TongTien=0 AND TrangThai=N'Chua thanh toan';"}
    if($created.Count -gt 0){Read-TestScalar $c "SELECT COUNT(*) FROM dbo.HOADON WHERE MaHD IN ('HD86000001','HD86000002','HD86000003','HD86000004')" | Set-Content "$PSScriptRoot/recipient-fixtures-remaining.txt"}
    $c.Dispose()
}
