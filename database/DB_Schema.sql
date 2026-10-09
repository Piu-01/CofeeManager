-- QUAN LY CAFE - SQL Server, schema moi (khong xoa DB cu).
-- Chay file nay truoc, sau do chay Insert_Data.sql trong SSMS (co GO).
-- Neu DB QuanLyCafe_ERP_V2 da ton tai, dung DB trong khac hoac migration rieng.
-- Tien: VND nguyen. So luong: cung don vi voi NGUYENVATLIEU.DonViTinh.
-- TongTien = SUM(SoLuong * GiaBan), chua giam gia, chua phi ship.
-- ThanhTien = TongTien + PhiShip - GiamGia; PhiDoiTac la chi phi cua quan.
-- Mot kho chung: khong tru kho khi chuyen nguyen lieu ra quay.
-- Tru kho theo cong thuc khi thanh toan. Huy don chua TT khong hoan kho.
-- Mon da pha nhung huy: ghi phieu xuat 'Huy do hong'.
-- Don da thanh toan khong sua/xoa; hoan tien la nghiep vu chua mo rong.
-- SoLuongTon ban dau va DiemTichLuy ban dau la so du dau ky.
-- Chi su dung thu tuc cho thanh toan, huy don, tra no, kiem ke.
-- App can thuc hien moi thao tac nhieu bang trong transaction.
-- Tao HD voi TongTien=0,GiamGia=0 -> them mon -> cap nhat GiamGia -> goi sp_ThanhToanHoaDon.
-- Tao PN voi TongTien=0 -> them chi tiet; trigger tu tinh tong va cong kho.
-- Khi xoa/bot mon, can giam GiamGia truoc neu uu dai cu vuot tong moi.
-- BAN.TrangThai da bo: doc vw_TrangThaiBan. CONGNO_NCC.TrangThai/MaNCC da bo:
-- doc vw_CongNoNCC. CHITIET_HD dung MaDong; CHITIET_THANHTOAN dung MaTT.
-- Thu/chi tra no da duoc sp_TraCongNo tao; khong ghi them phieu chi trung.
-- Bang luong la ket qua da chot; khong tu dong tao phieu chi luong.
-- Tai khoan chi luu password hash tu thu vien hash mat khau cua app.
-- Chuc vu va vai tro dang nhap la hai khai niem rieng.
SET NOCOUNT ON;
GO
CREATE DATABASE QuanLyCafe_ERP_V2;
GO
USE QuanLyCafe_ERP_V2;
GO
CREATE TABLE CHUCVU (
    MaCV         CHAR(5)       NOT NULL,
    TenCV        NVARCHAR(50)  NOT NULL,
    PhuCap DECIMAL(18,0)           NOT NULL DEFAULT 0,
    HeSoTangCa   DECIMAL(5,2)         NOT NULL DEFAULT 1.5,

    CONSTRAINT PK_CHUCVU            PRIMARY KEY (MaCV),
    CONSTRAINT CHK_CHUCVU_PhuCap    CHECK (PhuCap >= 0),
    CONSTRAINT CHK_CHUCVU_HeSo      CHECK (HeSoTangCa >= 1.0)
);

CREATE TABLE CALAMVIEC (
    MaCa         CHAR(5)       NOT NULL,
    TenCa        NVARCHAR(30)  NOT NULL,
    GioBatDau    TIME          NOT NULL,
    GioKetThuc   TIME          NOT NULL,

    CONSTRAINT PK_CALAMVIEC         PRIMARY KEY (MaCa),
    CONSTRAINT CHK_CALV_GioHop      CHECK (GioBatDau <> GioKetThuc)
);

CREATE TABLE NHANVIEN (
    MaNV         CHAR(5)       NOT NULL,
    TenNV        NVARCHAR(50)  NOT NULL,
    SDT          VARCHAR(15)   NULL,
    LoaiHopDong  NVARCHAR(20)  NOT NULL,
    LuongCung DECIMAL(18,0)           NOT NULL DEFAULT 0,
    LuongTheoGio DECIMAL(18,0)           NOT NULL DEFAULT 0,
    MaCV         CHAR(5)       NOT NULL,

    CONSTRAINT PK_NHANVIEN          PRIMARY KEY (MaNV),
    CONSTRAINT CHK_NV_LoaiHopDong   CHECK (LoaiHopDong IN ('Full-time', 'Part-time')),
    CONSTRAINT CHK_NV_LuongCung     CHECK (LuongCung >= 0),
    CONSTRAINT CHK_NV_LuongGio      CHECK (LuongTheoGio >= 0),
    CONSTRAINT FK_NV_CHUCVU         FOREIGN KEY (MaCV) REFERENCES CHUCVU(MaCV)
);

CREATE TABLE CHAMCONG (
    MaNV         CHAR(5)       NOT NULL,
    NgayLam      DATE          NOT NULL,
    MaCa         CHAR(5)       NOT NULL,
    GioVao       DATETIME2(0)      NULL,
    GioRa        DATETIME2(0)      NULL,
    TrangThai    NVARCHAR(30)  NOT NULL DEFAULT N'Di lam',

    CONSTRAINT CHK_CC_NgayVao CHECK (GioVao IS NULL OR CONVERT(DATE,GioVao)=NgayLam),
    CONSTRAINT CHK_CC_Nghi CHECK (TrangThai=N'Di lam' OR (GioVao IS NULL AND GioRa IS NULL)),
    CONSTRAINT PK_CHAMCONG          PRIMARY KEY (MaNV, NgayLam, MaCa),
    CONSTRAINT CHK_CC_TrangThai     CHECK (TrangThai IN (N'Di lam', N'Nghi phep', N'Vang')),
    CONSTRAINT CHK_CC_GioHopLe      CHECK (GioRa IS NULL OR (GioVao IS NOT NULL AND GioRa > GioVao)),
    CONSTRAINT FK_CC_NHANVIEN       FOREIGN KEY (MaNV) REFERENCES NHANVIEN(MaNV),
    CONSTRAINT FK_CC_CALAMVIEC      FOREIGN KEY (MaCa) REFERENCES CALAMVIEC(MaCa)
);

CREATE TABLE DANHMUC_SP (
    MaDM         CHAR(5)       NOT NULL,
    TenDM        NVARCHAR(50)  NOT NULL,

    CONSTRAINT PK_DANHMUC_SP        PRIMARY KEY (MaDM),
    CONSTRAINT UQ_DANHMUC_SP_Ten    UNIQUE (TenDM)
);

CREATE TABLE NGUYENVATLIEU (
    MaNVL        CHAR(5)       NOT NULL,
    TenNVL       NVARCHAR(50)  NOT NULL,
    DonViTinh    NVARCHAR(20)  NOT NULL,
    SoLuongTon   DECIMAL(18,4)         NOT NULL DEFAULT 0,

    CONSTRAINT PK_NGUYENVATLIEU     PRIMARY KEY (MaNVL),
    CONSTRAINT CHK_NVL_TonKho       CHECK (SoLuongTon >= 0)
);

CREATE TABLE NHACUNGCAP (
    MaNCC        CHAR(5)       NOT NULL,
    TenNCC       NVARCHAR(100) NOT NULL,
    SDT          VARCHAR(15)   NOT NULL,

    CONSTRAINT PK_NHACUNGCAP        PRIMARY KEY (MaNCC),
    CONSTRAINT UQ_NCC_SDT           UNIQUE (SDT)
);

CREATE TABLE SANPHAM (
    MaSP         CHAR(5)       NOT NULL,
    TenSP        NVARCHAR(50)  NOT NULL,
    MaDM         CHAR(5)       NOT NULL,
    GiaBan DECIMAL(18,0)           NOT NULL,
    TrangThai    NVARCHAR(20)  NOT NULL DEFAULT N'Dang ban',

    CONSTRAINT PK_SANPHAM           PRIMARY KEY (MaSP),
    CONSTRAINT CHK_SP_GiaBan        CHECK (GiaBan > 0),
    CONSTRAINT CHK_SP_TrangThai     CHECK (TrangThai IN (N'Dang ban', N'Ngung ban')),
    CONSTRAINT FK_SP_DANHMUC        FOREIGN KEY (MaDM) REFERENCES DANHMUC_SP(MaDM)
);

CREATE TABLE CONGTHUC (
    MaSP         CHAR(5)       NOT NULL,
    MaNVL        CHAR(5)       NOT NULL,
    DinhMuc      DECIMAL(18,4)         NOT NULL,

    CONSTRAINT PK_CONGTHUC          PRIMARY KEY (MaSP, MaNVL),
    CONSTRAINT CHK_CT_DinhMuc       CHECK (DinhMuc > 0),
    CONSTRAINT FK_CT_SANPHAM        FOREIGN KEY (MaSP)  REFERENCES SANPHAM(MaSP),
    CONSTRAINT FK_CT_NGUYENVATLIEU  FOREIGN KEY (MaNVL) REFERENCES NGUYENVATLIEU(MaNVL)
);

CREATE TABLE PHIEUNHAP (
    MaPN         CHAR(10)      NOT NULL,
    NgayNhap     DATETIME2(0)      NOT NULL DEFAULT GETDATE(),
    MaNV         CHAR(5)       NOT NULL,
    MaNCC        CHAR(5)       NOT NULL,
    TongTien DECIMAL(18,0)           NOT NULL DEFAULT 0,

    CONSTRAINT PK_PHIEUNHAP         PRIMARY KEY (MaPN),
    CONSTRAINT CHK_PN_TongTien      CHECK (TongTien >= 0),
    CONSTRAINT FK_PN_NHANVIEN       FOREIGN KEY (MaNV)  REFERENCES NHANVIEN(MaNV),
    CONSTRAINT FK_PN_NHACUNGCAP     FOREIGN KEY (MaNCC) REFERENCES NHACUNGCAP(MaNCC)
);

CREATE TABLE CHITIET_PN (
    MaPN         CHAR(10)      NOT NULL,
    MaNVL        CHAR(5)       NOT NULL,
    SoLuong      DECIMAL(18,4)         NOT NULL,
    DonGiaNhap DECIMAL(18,0)           NOT NULL,

    CONSTRAINT PK_CHITIET_PN        PRIMARY KEY (MaPN, MaNVL),
    CONSTRAINT CHK_CTPN_SoLuong     CHECK (SoLuong > 0),
    CONSTRAINT CHK_CTPN_DonGia      CHECK (DonGiaNhap > 0),
    CONSTRAINT FK_CTPN_PHIEUNHAP    FOREIGN KEY (MaPN)  REFERENCES PHIEUNHAP(MaPN),
    CONSTRAINT FK_CTPN_NVL          FOREIGN KEY (MaNVL) REFERENCES NGUYENVATLIEU(MaNVL)
);

CREATE TABLE PHIEUXUAT (
    MaPX         CHAR(10)      NOT NULL,
    NgayXuat     DATETIME2(0)      NOT NULL DEFAULT GETDATE(),
    MaNV         CHAR(5)       NOT NULL,
    LoaiXuat     NVARCHAR(30)  NOT NULL,

    CONSTRAINT PK_PHIEUXUAT         PRIMARY KEY (MaPX),
    CONSTRAINT CHK_PX_LoaiXuat      CHECK (LoaiXuat IN
        (N'Huy do hong', N'Tra hang')),
    CONSTRAINT FK_PX_NHANVIEN       FOREIGN KEY (MaNV) REFERENCES NHANVIEN(MaNV)
);

CREATE TABLE CHITIET_PX (
    MaPX         CHAR(10)      NOT NULL,
    MaNVL        CHAR(5)       NOT NULL,
    SoLuongXuat  DECIMAL(18,4)         NOT NULL,

    CONSTRAINT PK_CHITIET_PX        PRIMARY KEY (MaPX, MaNVL),
    CONSTRAINT CHK_CTPX_SoLuong     CHECK (SoLuongXuat > 0),
    CONSTRAINT FK_CTPX_PHIEUXUAT    FOREIGN KEY (MaPX)  REFERENCES PHIEUXUAT(MaPX),
    CONSTRAINT FK_CTPX_NVL          FOREIGN KEY (MaNVL) REFERENCES NGUYENVATLIEU(MaNVL)
);

CREATE TABLE PHIEUKIEMKE (
    MaKK         CHAR(10)      NOT NULL,
    NgayKK       DATE          NOT NULL,
    MaNV         CHAR(5)       NOT NULL,
    DaDieuChinh BIT NOT NULL DEFAULT 0,

    CONSTRAINT PK_PHIEUKIEMKE       PRIMARY KEY (MaKK),
    CONSTRAINT FK_KK_NHANVIEN       FOREIGN KEY (MaNV) REFERENCES NHANVIEN(MaNV)
);

CREATE TABLE CHITIET_KK (
    MaKK         CHAR(10)      NOT NULL,
    MaNVL        CHAR(5)       NOT NULL,
    TonHeThong   DECIMAL(18,4)         NOT NULL,
    TonThucTe    DECIMAL(18,4)         NOT NULL,
    LyDo         NVARCHAR(100) NULL,

    CONSTRAINT PK_CHITIET_KK        PRIMARY KEY (MaKK, MaNVL),
    CONSTRAINT CHK_CTKK_TonHT       CHECK (TonHeThong >= 0),
    CONSTRAINT CHK_CTKK_TonTT       CHECK (TonThucTe  >= 0),
    CONSTRAINT FK_CTKK_KIEMKE       FOREIGN KEY (MaKK)  REFERENCES PHIEUKIEMKE(MaKK),
    CONSTRAINT FK_CTKK_NVL          FOREIGN KEY (MaNVL) REFERENCES NGUYENVATLIEU(MaNVL)
);

CREATE TABLE HANGTHANHVIEN (
    MaHang       CHAR(5)       NOT NULL,
    DiemToiThieu INT           NOT NULL,
    PhanTramGiam INT           NOT NULL DEFAULT 0,

    CONSTRAINT PK_HANGTHANHVIEN         PRIMARY KEY (MaHang),
    CONSTRAINT CHK_HTV_Diem             CHECK (DiemToiThieu >= 0),
    CONSTRAINT CHK_HTV_PhanTramGiam     CHECK (PhanTramGiam >= 0 AND PhanTramGiam <= 100)
);

CREATE TABLE KHACHHANG (
    MaKH         CHAR(5)       NOT NULL,
    TenKH        NVARCHAR(50)  NOT NULL,
    SDT          VARCHAR(15)   NULL,
    DiemTichLuy  INT           NOT NULL DEFAULT 0,
    MaHang       CHAR(5)       NOT NULL,

    CONSTRAINT PK_KHACHHANG         PRIMARY KEY (MaKH),
    CONSTRAINT CHK_KH_Diem          CHECK (DiemTichLuy >= 0),
    CONSTRAINT FK_KH_HANG           FOREIGN KEY (MaHang) REFERENCES HANGTHANHVIEN(MaHang)
);

CREATE TABLE LICHSU_DIEM (
    MaHD CHAR(10) NULL,
    MaGD         INT           IDENTITY(1,1) NOT NULL,
    MaKH         CHAR(5)       NOT NULL,
    NgayGD       DATETIME2(0)      NOT NULL DEFAULT GETDATE(),
    Loai         NVARCHAR(10)  NOT NULL,
    SoDiem       INT           NOT NULL,
    LyDo         NVARCHAR(100) NOT NULL,

    CONSTRAINT PK_LICHSU_DIEM       PRIMARY KEY (MaGD),
    CONSTRAINT CHK_LD_Loai          CHECK (Loai IN (N'Cong', N'Tru')),
    CONSTRAINT CHK_LD_SoDiem        CHECK (SoDiem > 0),
    CONSTRAINT FK_LD_KHACHHANG      FOREIGN KEY (MaKH) REFERENCES KHACHHANG(MaKH)
);

CREATE TABLE KHUYENMAI (
    MaKM         CHAR(10)      NOT NULL,
    TenKM        NVARCHAR(100) NOT NULL,
    NgayBD       DATE          NOT NULL,
    NgayKT       DATE          NOT NULL,
    PhanTramGiam INT           NOT NULL DEFAULT 0,

    CONSTRAINT PK_KHUYENMAI         PRIMARY KEY (MaKM),
    CONSTRAINT CHK_KM_NgayHopLe     CHECK (NgayKT >= NgayBD),
    CONSTRAINT CHK_KM_PhanTram      CHECK (PhanTramGiam >= 0 AND PhanTramGiam <= 100)
);

CREATE TABLE KHUVUC (
    MaKV         CHAR(5)       NOT NULL,
    TenKV        NVARCHAR(50)  NOT NULL,

    CONSTRAINT PK_KHUVUC            PRIMARY KEY (MaKV),
    CONSTRAINT UQ_KHUVUC_Ten        UNIQUE (TenKV)
);

CREATE TABLE BAN (
    MaBan        CHAR(5)       NOT NULL,
    TenBan       NVARCHAR(20)  NOT NULL,
    MaKV         CHAR(5)       NOT NULL,

    CONSTRAINT PK_BAN               PRIMARY KEY (MaBan),
    CONSTRAINT FK_BAN_KHUVUC        FOREIGN KEY (MaKV) REFERENCES KHUVUC(MaKV)
);

CREATE TABLE DOITACGIAOHANG (
    MaDT         CHAR(5)       NOT NULL,
    TenDT        NVARCHAR(50)  NOT NULL,
    ChietKhau    DECIMAL(5,2)         NOT NULL DEFAULT 0,

    CONSTRAINT PK_DOITACGIAOHANG    PRIMARY KEY (MaDT),
    CONSTRAINT CHK_DT_ChietKhau     CHECK (ChietKhau >= 0 AND ChietKhau <= 100)
);

CREATE TABLE PHUONGTHUCTT (
    MaPT         CHAR(5)       NOT NULL,
    TenPT        NVARCHAR(50)  NOT NULL,

    CONSTRAINT PK_PHUONGTHUCTT      PRIMARY KEY (MaPT),
    CONSTRAINT UQ_PT_Ten            UNIQUE (TenPT)
);

CREATE TABLE HOADON (
    MaHD         CHAR(10)      NOT NULL,
    NgayLap      DATETIME2(0)      NOT NULL DEFAULT GETDATE(),
    MaNV         CHAR(5)       NOT NULL,
    MaKH         CHAR(5)       NULL,
    MaBan        CHAR(5)       NULL,
    MaDT         CHAR(5)       NULL,
    MaKM CHAR(10) NULL,
    LoaiDon NVARCHAR(20) NOT NULL DEFAULT N'Tai quan',
    TenNguoiNhan NVARCHAR(100) NULL,
    SDTNguoiNhan VARCHAR(15) NULL,
    DiaChiGiao NVARCHAR(250) NULL,
    TrangThaiGiao NVARCHAR(20) NULL,
    NgayThanhToan DATETIME2(0) NULL,
    PhanTramPhiDoiTac DECIMAL(5,2) NOT NULL DEFAULT 0,
    PhiDoiTac DECIMAL(18,0) NOT NULL DEFAULT 0,
    TongTien DECIMAL(18,0)           NOT NULL DEFAULT 0,
    PhiShip DECIMAL(18,0)           NOT NULL DEFAULT 0,
    GiamGia DECIMAL(18,0)           NOT NULL DEFAULT 0,
    TrangThai    NVARCHAR(30)  NOT NULL DEFAULT N'Chua thanh toan',

    ThanhTien AS (TongTien + PhiShip - GiamGia) PERSISTED,
    CONSTRAINT CHK_HD_GiamToiDa CHECK (GiamGia <= TongTien),
    CONSTRAINT CHK_HD_PhiDT CHECK (PhanTramPhiDoiTac BETWEEN 0 AND 100 AND PhiDoiTac >= 0),
    CONSTRAINT CHK_HD_LoaiDon CHECK (LoaiDon IN (N'Tai quan', N'Mang di', N'Giao hang')),
    CONSTRAINT CHK_HD_BanTheoLoai CHECK (
        (LoaiDon = N'Tai quan' AND MaBan IS NOT NULL AND MaDT IS NULL)
        OR (LoaiDon=N'Mang di' AND MaBan IS NULL AND MaDT IS NULL)
        OR (LoaiDon=N'Giao hang' AND MaBan IS NULL)),
    CONSTRAINT CHK_HD_GiaoHang CHECK (
        (LoaiDon = N'Giao hang' AND TenNguoiNhan IS NOT NULL AND SDTNguoiNhan IS NOT NULL
         AND DiaChiGiao IS NOT NULL AND TrangThaiGiao IS NOT NULL)
        OR (LoaiDon <> N'Giao hang' AND TrangThaiGiao IS NULL)),
    CONSTRAINT CHK_HD_TrangThaiGiao CHECK (TrangThaiGiao IS NULL OR TrangThaiGiao IN
        (N'Cho giao', N'Dang giao', N'Da giao', N'Huy giao')),
    CONSTRAINT CHK_HD_NgayTT CHECK (
        (TrangThai = N'Da thanh toan' AND NgayThanhToan IS NOT NULL)
        OR (TrangThai <> N'Da thanh toan' AND NgayThanhToan IS NULL)),
    CONSTRAINT FK_HD_KHUYENMAI FOREIGN KEY (MaKM) REFERENCES KHUYENMAI(MaKM),
    CONSTRAINT PK_HOADON            PRIMARY KEY (MaHD),
    CONSTRAINT CHK_HD_TongTien      CHECK (TongTien >= 0),
    CONSTRAINT CHK_HD_PhiShip       CHECK (PhiShip  >= 0),
    CONSTRAINT CHK_HD_GiamGia       CHECK (GiamGia  >= 0),
    CONSTRAINT CHK_HD_TrangThai     CHECK (TrangThai IN
        (N'Chua thanh toan', N'Da thanh toan', N'Huy')),
    CONSTRAINT FK_HD_NHANVIEN       FOREIGN KEY (MaNV)  REFERENCES NHANVIEN(MaNV),
    CONSTRAINT FK_HD_KHACHHANG      FOREIGN KEY (MaKH)  REFERENCES KHACHHANG(MaKH),
    CONSTRAINT FK_HD_BAN            FOREIGN KEY (MaBan) REFERENCES BAN(MaBan),
    CONSTRAINT FK_HD_DOITAC         FOREIGN KEY (MaDT)  REFERENCES DOITACGIAOHANG(MaDT)
);

CREATE TABLE CHITIET_HD (
    MaHD         CHAR(10)      NOT NULL,
    MaDong INT IDENTITY(1,1) NOT NULL,
    MaSP         CHAR(5)       NOT NULL,
    SoLuong      INT           NOT NULL,
    GiaBan DECIMAL(18,0)           NOT NULL,
    GhiChu       NVARCHAR(100) NULL,

    CONSTRAINT PK_CHITIET_HD        PRIMARY KEY (MaDong),
    CONSTRAINT CHK_CTHD_SoLuong     CHECK (SoLuong > 0),
    CONSTRAINT CHK_CTHD_GiaBan      CHECK (GiaBan  > 0),
    CONSTRAINT FK_CTHD_HOADON       FOREIGN KEY (MaHD) REFERENCES HOADON(MaHD),
    CONSTRAINT FK_CTHD_SANPHAM      FOREIGN KEY (MaSP) REFERENCES SANPHAM(MaSP)
);

CREATE TABLE CHITIET_THANHTOAN (
    MaHD         CHAR(10)      NOT NULL,
    MaTT INT IDENTITY(1,1) NOT NULL,
    MaPT         CHAR(5)       NOT NULL,
    NgayTT DATETIME2(0) NOT NULL DEFAULT SYSDATETIME(),
    MaThamChieu NVARCHAR(100) NULL,
    SoTien DECIMAL(18,0)           NOT NULL,

    CONSTRAINT PK_CHITIET_TT        PRIMARY KEY (MaTT),
    CONSTRAINT CHK_CTTT_SoTien      CHECK (SoTien > 0),
    CONSTRAINT FK_CTTT_HOADON       FOREIGN KEY (MaHD) REFERENCES HOADON(MaHD),
    CONSTRAINT FK_CTTT_PHUONGTHUC   FOREIGN KEY (MaPT) REFERENCES PHUONGTHUCTT(MaPT)
);

CREATE TABLE DANHMUC_THUCHI (
    MaDMTC       CHAR(5)       NOT NULL,
    TenDMTC      NVARCHAR(50)  NOT NULL,
    Loai         NVARCHAR(10)  NOT NULL,

    CONSTRAINT PK_DANHMUC_THUCHI    PRIMARY KEY (MaDMTC),
    CONSTRAINT CHK_DMTC_Loai        CHECK (Loai IN (N'Thu', N'Chi'))
);

CREATE TABLE TAISAN_CODINH (
    MaTS         CHAR(5)       NOT NULL,
    TenTS        NVARCHAR(100) NOT NULL,
    NgayMua      DATE          NOT NULL,
    GiaTri DECIMAL(18,0)           NOT NULL,
    SoThangKH    INT           NOT NULL,

    CONSTRAINT PK_TAISAN_CODINH     PRIMARY KEY (MaTS),
    CONSTRAINT CHK_TS_GiaTri        CHECK (GiaTri > 0),
    CONSTRAINT CHK_TS_SoThangKH     CHECK (SoThangKH > 0)
);

CREATE TABLE PHIEUTHUCHI (
    MaTC         CHAR(10)      NOT NULL,
    NgayLap      DATETIME2(0)      NOT NULL DEFAULT GETDATE(),
    MaDMTC       CHAR(5)       NOT NULL,
    MaNV         CHAR(5)       NOT NULL,
    MaTS         CHAR(5)       NULL,
    SoTien DECIMAL(18,0)           NOT NULL,
    GhiChu       NVARCHAR(200) NULL,

    CONSTRAINT PK_PHIEUTHUCHI       PRIMARY KEY (MaTC),
    CONSTRAINT CHK_PTC_SoTien       CHECK (SoTien > 0),
    CONSTRAINT FK_PTC_DANHMUC       FOREIGN KEY (MaDMTC) REFERENCES DANHMUC_THUCHI(MaDMTC),
    CONSTRAINT FK_PTC_NHANVIEN      FOREIGN KEY (MaNV)   REFERENCES NHANVIEN(MaNV),
    CONSTRAINT FK_PTC_TAISAN        FOREIGN KEY (MaTS)   REFERENCES TAISAN_CODINH(MaTS)
);

CREATE TABLE CONGNO_NCC (
    MaCN         CHAR(10)      NOT NULL,
    MaPN         CHAR(10)      NOT NULL,
    TienNo DECIMAL(18,0)           NOT NULL,
    HanTra       DATE          NULL,

    CONSTRAINT UQ_CN_MaPN UNIQUE (MaPN),
    CONSTRAINT PK_CONGNO_NCC        PRIMARY KEY (MaCN),
    CONSTRAINT CHK_CN_TienNo        CHECK (TienNo > 0),
    CONSTRAINT FK_CN_PHIEUNHAP      FOREIGN KEY (MaPN)  REFERENCES PHIEUNHAP(MaPN)
);

GO
CREATE UNIQUE INDEX UX_NV_SDT ON NHANVIEN(SDT) WHERE SDT IS NOT NULL;
CREATE UNIQUE INDEX UX_KH_SDT ON KHACHHANG(SDT) WHERE SDT IS NOT NULL;
ALTER TABLE LICHSU_DIEM ADD CONSTRAINT FK_LD_HD FOREIGN KEY (MaHD) REFERENCES HOADON(MaHD);
-- Mot hoa don chi cong diem mua hang mot lan.
CREATE UNIQUE INDEX UX_LD_HD ON LICHSU_DIEM(MaHD) WHERE MaHD IS NOT NULL AND Loai = N'Cong';
CREATE UNIQUE INDEX UX_HD_BanMo ON HOADON(MaBan)
    WHERE MaBan IS NOT NULL AND TrangThai = N'Chua thanh toan';
CREATE INDEX IX_HD_NgayLap ON HOADON(NgayLap, TrangThai);
CREATE INDEX IX_CTHD_MaHD ON CHITIET_HD(MaHD);
CREATE INDEX IX_CTTT_MaHD ON CHITIET_THANHTOAN(MaHD);
CREATE INDEX IX_CT_MaNVL ON CONGTHUC(MaNVL);
ALTER TABLE HANGTHANHVIEN ADD CONSTRAINT UQ_HTV_Nguong UNIQUE (DiemToiThieu);
ALTER TABLE NHANVIEN ADD DangLamViec BIT NOT NULL CONSTRAINT DF_NV_DangLam DEFAULT 1;
GO
CREATE TABLE VAITRO (
    MaVT CHAR(5) NOT NULL CONSTRAINT PK_VAITRO PRIMARY KEY,
    TenVT NVARCHAR(50) NOT NULL CONSTRAINT UQ_VT_Ten UNIQUE
);
CREATE TABLE TAIKHOAN (
    MaTK INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_TAIKHOAN PRIMARY KEY,
    MaNV CHAR(5) NOT NULL CONSTRAINT UQ_TK_NV UNIQUE,
    TenDangNhap VARCHAR(50) NOT NULL CONSTRAINT UQ_TK_Login UNIQUE,
    MatKhauHash VARCHAR(255) NOT NULL,
    MaVT CHAR(5) NOT NULL,
    HoatDong BIT NOT NULL DEFAULT 0,
    CONSTRAINT FK_TK_NV FOREIGN KEY (MaNV) REFERENCES NHANVIEN(MaNV),
    CONSTRAINT FK_TK_VT FOREIGN KEY (MaVT) REFERENCES VAITRO(MaVT)
);
CREATE TABLE BANGLUONG (
    MaBL INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_BANGLUONG PRIMARY KEY,
    MaNV CHAR(5) NOT NULL,
    Thang TINYINT NOT NULL CHECK (Thang BETWEEN 1 AND 12),
    Nam SMALLINT NOT NULL CHECK (Nam BETWEEN 2000 AND 9999),
    NgayChot DATETIME2(0) NOT NULL DEFAULT SYSDATETIME(),
    LuongCoBan DECIMAL(18,0) NOT NULL CHECK (LuongCoBan >= 0),
    SoGioLam DECIMAL(10,2) NOT NULL DEFAULT 0 CHECK (SoGioLam >= 0),
    DonGiaGio DECIMAL(18,0) NOT NULL DEFAULT 0 CHECK (DonGiaGio >= 0),
    TienTangCa DECIMAL(18,0) NOT NULL DEFAULT 0 CHECK (TienTangCa >= 0),
    PhuCap DECIMAL(18,0) NOT NULL DEFAULT 0 CHECK (PhuCap >= 0),
    Thuong DECIMAL(18,0) NOT NULL DEFAULT 0 CHECK (Thuong >= 0),
    KhauTru DECIMAL(18,0) NOT NULL DEFAULT 0 CHECK (KhauTru >= 0),
    ThucLinh AS (LuongCoBan + ROUND(SoGioLam * DonGiaGio, 0)
                    + TienTangCa + PhuCap + Thuong - KhauTru) PERSISTED,
    CONSTRAINT CHK_BL_ThucLinh CHECK (LuongCoBan + ROUND(SoGioLam * DonGiaGio,0)
                    + TienTangCa + PhuCap + Thuong >= KhauTru),
    CONSTRAINT UQ_BL_Ky UNIQUE (MaNV, Thang, Nam),
    CONSTRAINT FK_BL_NV FOREIGN KEY (MaNV) REFERENCES NHANVIEN(MaNV)
);
CREATE TABLE THANHTOAN_CONGNO (
    MaTT INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_TTCN PRIMARY KEY,
    MaCN CHAR(10) NOT NULL,
    MaTC CHAR(10) NOT NULL CONSTRAINT UQ_TTCN_PhieuChi UNIQUE,
    MaPT CHAR(5) NOT NULL,
    CONSTRAINT FK_TTCN_CN FOREIGN KEY (MaCN) REFERENCES CONGNO_NCC(MaCN),
    CONSTRAINT FK_TTCN_TC FOREIGN KEY (MaTC) REFERENCES PHIEUTHUCHI(MaTC),
    CONSTRAINT FK_TTCN_PT FOREIGN KEY (MaPT) REFERENCES PHUONGTHUCTT(MaPT)
);
-- Snapshot nguyen lieu da tieu hao: sua cong thuc khong doi lich su ban hang.
CREATE TABLE TIEUHAO_HOADON (
    MaHD CHAR(10) NOT NULL,
    MaNVL CHAR(5) NOT NULL,
    SoLuong DECIMAL(18,4) NOT NULL CHECK (SoLuong > 0),
    CONSTRAINT PK_TIEUHAO PRIMARY KEY (MaHD, MaNVL),
    CONSTRAINT FK_TH_HD FOREIGN KEY (MaHD) REFERENCES HOADON(MaHD),
    CONSTRAINT FK_TH_NVL FOREIGN KEY (MaNVL) REFERENCES NGUYENVATLIEU(MaNVL)
);
GO
CREATE VIEW vw_CongNoNCC AS
SELECT c.MaCN, p.MaNCC, c.MaPN, c.TienNo, c.HanTra,
       COALESCE(t.DaTra,0) AS DaTra, c.TienNo - COALESCE(t.DaTra,0) AS ConNo,
       CASE WHEN c.TienNo = COALESCE(t.DaTra,0) THEN N'Da tra'
            WHEN c.HanTra < CONVERT(DATE,SYSDATETIME()) THEN N'Qua han'
            WHEN COALESCE(t.DaTra,0) > 0 THEN N'Tra mot phan'
            ELSE N'Chua tra' END AS TrangThai
FROM CONGNO_NCC c JOIN PHIEUNHAP p ON p.MaPN=c.MaPN
OUTER APPLY (SELECT SUM(tc.SoTien) AS DaTra FROM THANHTOAN_CONGNO tt
             JOIN PHIEUTHUCHI tc ON tc.MaTC=tt.MaTC WHERE tt.MaCN=c.MaCN) t;
GO
CREATE VIEW vw_TrangThaiBan AS
SELECT b.MaBan,b.TenBan,b.MaKV,
       CASE WHEN h.MaHD IS NULL THEN N'Trong' ELSE N'Dang phuc vu' END AS TrangThai,
       h.MaHD
FROM BAN b LEFT JOIN HOADON h ON h.MaBan=b.MaBan AND h.TrangThai=N'Chua thanh toan';
GO

-- Tu dong tinh tong tien; khoa chi tiet cua hoa don da thanh toan/huy.
CREATE TRIGGER TR_CTHD_TongTien ON CHITIET_HD AFTER INSERT, UPDATE, DELETE AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (SELECT 1 FROM HOADON h WITH (UPDLOCK,HOLDLOCK)
               WHERE h.MaHD IN (SELECT MaHD FROM inserted UNION SELECT MaHD FROM deleted)
                 AND h.TrangThai <> N'Chua thanh toan')
        THROW 51001,N'Khong duoc sua mon cua hoa don da thanh toan hoac huy.',1;
    UPDATE h SET TongTien=COALESCE(t.Tong,0)
    FROM HOADON h OUTER APPLY
      (SELECT SUM(CONVERT(DECIMAL(18,0),d.SoLuong)*d.GiaBan) AS Tong
       FROM CHITIET_HD d WHERE d.MaHD=h.MaHD) t
    WHERE h.MaHD IN (SELECT MaHD FROM inserted UNION SELECT MaHD FROM deleted);
END;
GO
CREATE TRIGGER TR_CTTT_KiemTra ON CHITIET_THANHTOAN AFTER INSERT,UPDATE,DELETE AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (SELECT 1 FROM HOADON h WITH (UPDLOCK,HOLDLOCK)
        WHERE h.MaHD IN (SELECT MaHD FROM inserted UNION SELECT MaHD FROM deleted)
          AND h.TrangThai<>N'Chua thanh toan')
        THROW 51002,N'Khong duoc sua thanh toan cua hoa don da chot/huy.',1;
    IF EXISTS (SELECT 1 FROM HOADON h
        CROSS APPLY (SELECT SUM(SoTien) AS Tong FROM CHITIET_THANHTOAN WHERE MaHD=h.MaHD) t
        WHERE h.MaHD IN (SELECT MaHD FROM inserted UNION SELECT MaHD FROM deleted)
          AND t.Tong>h.ThanhTien)
        THROW 51003,N'Tien phan bo thanh toan vuot so phai tra (khong luu tien khach dua).',1;
END;
GO
CREATE TRIGGER TR_HD_Chot ON HOADON AFTER INSERT,UPDATE,DELETE AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (SELECT 1 FROM deleted WHERE TrangThai=N'Huy')
        THROW 51004,N'Khong sua/xoa hoa don da huy.',1;
    -- Hoa don da TT chi duoc doi TrangThaiGiao; moi thong tin tai chinh giu nguyen.
    IF EXISTS (
        SELECT MaHD,NgayLap,MaNV,MaKH,MaBan,MaDT,MaKM,LoaiDon,TenNguoiNhan,
               SDTNguoiNhan,DiaChiGiao,NgayThanhToan,PhanTramPhiDoiTac,PhiDoiTac,
               TongTien,PhiShip,GiamGia,TrangThai
        FROM deleted WHERE TrangThai=N'Da thanh toan'
        EXCEPT
        SELECT MaHD,NgayLap,MaNV,MaKH,MaBan,MaDT,MaKM,LoaiDon,TenNguoiNhan,
               SDTNguoiNhan,DiaChiGiao,NgayThanhToan,PhanTramPhiDoiTac,PhiDoiTac,
               TongTien,PhiShip,GiamGia,TrangThai FROM inserted)
        THROW 51022,N'Hoa don da TT chi duoc cap nhat trang thai giao hang.',1;
    IF EXISTS (SELECT 1 FROM inserted i WHERE i.TrangThai=N'Da thanh toan'
        AND (i.TongTien<=0 OR NOT EXISTS (SELECT 1 FROM CHITIET_HD WHERE MaHD=i.MaHD)
             OR i.ThanhTien<>COALESCE((SELECT SUM(SoTien) FROM CHITIET_THANHTOAN WHERE MaHD=i.MaHD),0)
             OR NOT EXISTS (SELECT 1 FROM TIEUHAO_HOADON WHERE MaHD=i.MaHD)))
        THROW 51005,N'Phai co mon, thanh toan du va ghi nhan tieu hao truoc khi chot.',1;
    IF EXISTS (SELECT 1 FROM inserted i WHERE i.TrangThai=N'Huy'
        AND (EXISTS (SELECT 1 FROM CHITIET_THANHTOAN WHERE MaHD=i.MaHD)
             OR EXISTS (SELECT 1 FROM TIEUHAO_HOADON WHERE MaHD=i.MaHD)))
        THROW 51006,N'Chi huy don chua thu tien va chua tru nguyen lieu.',1;
    IF EXISTS (SELECT 1 FROM inserted i WHERE i.TongTien <>
        COALESCE((SELECT SUM(CONVERT(DECIMAL(18,0),SoLuong)*GiaBan) FROM CHITIET_HD WHERE MaHD=i.MaHD),0))
        THROW 51007,N'TongTien phai khop chi tiet mon.',1;
    IF EXISTS (SELECT 1 FROM inserted i WHERE COALESCE(
        (SELECT SUM(SoTien) FROM CHITIET_THANHTOAN WHERE MaHD=i.MaHD),0)>i.ThanhTien)
        THROW 51008,N'Thay doi hoa don lam so tien da thu vuot tien phai tra.',1;
END;
GO
-- Nhap/xuat cap nhat kho theo chenhlech inserted-deleted; ho tro nhieu dong.
CREATE TRIGGER TR_CTPN_Kho ON CHITIET_PN AFTER INSERT,UPDATE,DELETE AS
BEGIN
    SET NOCOUNT ON;
    ;WITH delta AS (
      SELECT MaNVL,CONVERT(DECIMAL(18,4),SUM(SL)) SL FROM
        (SELECT MaNVL,SoLuong SL FROM inserted UNION ALL SELECT MaNVL,-SoLuong FROM deleted) x
      GROUP BY MaNVL)
    UPDATE n WITH (UPDLOCK) SET SoLuongTon=n.SoLuongTon+d.SL
    FROM NGUYENVATLIEU n JOIN delta d ON d.MaNVL=n.MaNVL;
    UPDATE p SET TongTien=COALESCE(t.Tong,0)
    FROM PHIEUNHAP p OUTER APPLY
      (SELECT SUM(ROUND(SoLuong*DonGiaNhap,0)) Tong FROM CHITIET_PN WHERE MaPN=p.MaPN) t
    WHERE p.MaPN IN (SELECT MaPN FROM inserted UNION SELECT MaPN FROM deleted);
END;
GO
CREATE TRIGGER TR_CTPX_Kho ON CHITIET_PX AFTER INSERT,UPDATE,DELETE AS
BEGIN
    SET NOCOUNT ON;
    ;WITH delta AS (
      SELECT MaNVL,CONVERT(DECIMAL(18,4),SUM(SL)) SL FROM
        (SELECT MaNVL,SoLuongXuat SL FROM inserted UNION ALL SELECT MaNVL,-SoLuongXuat FROM deleted) x
      GROUP BY MaNVL)
    UPDATE n WITH (UPDLOCK) SET SoLuongTon=n.SoLuongTon-d.SL
    FROM NGUYENVATLIEU n JOIN delta d ON d.MaNVL=n.MaNVL;
END;
GO
CREATE TRIGGER TR_TieuHao_Kho ON TIEUHAO_HOADON AFTER INSERT,UPDATE,DELETE AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (SELECT 1 FROM deleted)
        THROW 51009,N'Tieu hao hoa don la lich su, khong sua/xoa.',1;
    IF EXISTS (SELECT 1 FROM inserted i JOIN HOADON h WITH (UPDLOCK,HOLDLOCK) ON h.MaHD=i.MaHD
               WHERE h.TrangThai<>N'Chua thanh toan')
        THROW 51010,N'Chi ghi tieu hao trong giao dich thanh toan don dang mo.',1;
    ;WITH delta AS (SELECT MaNVL,CONVERT(DECIMAL(18,4),SUM(SoLuong)) SL FROM inserted GROUP BY MaNVL)
    UPDATE n WITH (UPDLOCK) SET SoLuongTon=n.SoLuongTon-d.SL
    FROM NGUYENVATLIEU n JOIN delta d ON d.MaNVL=n.MaNVL;
    -- CHECK SoLuongTon >= 0 se huy giao dich neu khong du nguyen lieu.
END;
GO
CREATE TRIGGER TR_LichSuDiem ON LICHSU_DIEM AFTER INSERT,UPDATE,DELETE AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (SELECT 1 FROM deleted)
        THROW 51011,N'Lich su diem khong sua/xoa; tao giao dich dieu chinh.',1;
    IF EXISTS (SELECT 1 FROM inserted i JOIN HOADON h ON h.MaHD=i.MaHD
        WHERE h.MaKH IS NULL OR h.MaKH<>i.MaKH OR h.TrangThai<>N'Da thanh toan')
        THROW 51012,N'Hoa don tich diem phai da thanh toan va dung khach hang.',1;
    ;WITH delta AS (SELECT MaKH,SUM(CASE WHEN Loai=N'Cong' THEN SoDiem ELSE -SoDiem END) Diem
                  FROM inserted GROUP BY MaKH)
    UPDATE k WITH (UPDLOCK) SET DiemTichLuy=k.DiemTichLuy+d.Diem
    FROM KHACHHANG k JOIN delta d ON d.MaKH=k.MaKH;
    UPDATE k SET MaHang=h.MaHang
    FROM KHACHHANG k CROSS APPLY
      (SELECT TOP (1) MaHang FROM HANGTHANHVIEN WHERE DiemToiThieu<=k.DiemTichLuy
       ORDER BY DiemToiThieu DESC) h
    WHERE k.MaKH IN (SELECT MaKH FROM inserted);
END;
GO
-- Cong no goc khong vuot tong phieu nhap; khong sua sau khi da tra.
CREATE TRIGGER TR_CongNo ON CONGNO_NCC AFTER INSERT,UPDATE,DELETE AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (SELECT 1 FROM deleted d JOIN THANHTOAN_CONGNO t ON t.MaCN=d.MaCN)
        THROW 51013,N'Khong sua cong no da co thanh toan.',1;
    IF EXISTS (SELECT 1 FROM inserted i JOIN PHIEUNHAP p ON p.MaPN=i.MaPN WHERE i.TienNo>p.TongTien)
        THROW 51014,N'Cong no vuot gia tri phieu nhap.',1;
END;
GO
CREATE TRIGGER TR_PhieuNhap_Tong ON PHIEUNHAP AFTER INSERT,UPDATE AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (SELECT 1 FROM inserted i WHERE i.TongTien<>
       COALESCE((SELECT SUM(ROUND(SoLuong*DonGiaNhap,0)) FROM CHITIET_PN WHERE MaPN=i.MaPN),0))
        THROW 51015,N'Tong phieu nhap khong khop chi tiet.',1;
    IF EXISTS (SELECT 1 FROM inserted i JOIN CONGNO_NCC c ON c.MaPN=i.MaPN WHERE c.TienNo>i.TongTien)
        THROW 51016,N'Khong duoc giam gia tri phieu nhap duoi cong no.',1;
END;
GO
CREATE TRIGGER TR_TraNo ON THANHTOAN_CONGNO AFTER INSERT,UPDATE,DELETE AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (SELECT 1 FROM deleted) THROW 51017,N'Lich su tra no khong sua/xoa.',1;
    IF EXISTS (SELECT 1 FROM inserted i JOIN PHIEUTHUCHI p ON p.MaTC=i.MaTC
        JOIN DANHMUC_THUCHI d ON d.MaDMTC=p.MaDMTC WHERE d.Loai<>N'Chi')
        THROW 51018,N'Tra no phai lien ket phieu chi.',1;
    IF EXISTS (SELECT 1 FROM CONGNO_NCC c WITH (UPDLOCK,HOLDLOCK)
       CROSS APPLY (SELECT SUM(p.SoTien) DaTra FROM THANHTOAN_CONGNO t
                    JOIN PHIEUTHUCHI p ON p.MaTC=t.MaTC WHERE t.MaCN=c.MaCN) x
       WHERE c.MaCN IN (SELECT MaCN FROM inserted) AND x.DaTra>c.TienNo)
        THROW 51019,N'Tra no vuot so no goc.',1;
END;
GO
CREATE TRIGGER TR_PhieuChi_LichSu ON PHIEUTHUCHI AFTER UPDATE,DELETE AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (SELECT 1 FROM deleted d JOIN THANHTOAN_CONGNO t ON t.MaTC=d.MaTC)
        THROW 51020,N'Khong sua/xoa phieu chi da lien ket tra no.',1;
END;
GO
CREATE TRIGGER TR_DMTC_LichSu ON DANHMUC_THUCHI AFTER UPDATE AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (SELECT 1 FROM inserted i JOIN deleted d ON d.MaDMTC=i.MaDMTC
        JOIN PHIEUTHUCHI p ON p.MaDMTC=i.MaDMTC
        JOIN THANHTOAN_CONGNO t ON t.MaTC=p.MaTC WHERE i.Loai<>d.Loai)
        THROW 51021,N'Khong doi loai danh muc cua phieu tra no.',1;
END;
GO
CREATE TYPE dbo.DanhSachThanhToan AS TABLE (
    MaPT CHAR(5) NOT NULL,
    SoTien DECIMAL(18,0) NOT NULL CHECK (SoTien>0),
    MaThamChieu NVARCHAR(100) NULL
);
GO
-- GiamGia: gia tri VND da tinh theo chinh sach uu dai tai thoi diem ban.
-- Chinh sach mau: neu co MaKM thi dung KM, neu khong thi dung hang thanh vien;
-- khong cong don hai uu dai. App tinh va luu GiamGia truoc khi goi thu tuc.
-- 1 diem / 1.000 VND tien mon sau giam (khong tinh phi ship).
CREATE PROCEDURE dbo.sp_ThanhToanHoaDon
    @MaHD CHAR(10), @ThanhToan dbo.DanhSachThanhToan READONLY,
    @NgayTT DATETIME2(0)=NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    BEGIN TRY
        BEGIN TRAN;
        DECLARE @TrangThai NVARCHAR(30),@Tong DECIMAL(18,0),@Giam DECIMAL(18,0),
                @PhaiTra DECIMAL(18,0),@MaKH CHAR(5),@MaDT CHAR(5),@MaKM CHAR(10),@Ngay DATE;
        SELECT @TrangThai=TrangThai,@Tong=TongTien,@Giam=GiamGia,@PhaiTra=ThanhTien,
               @MaKH=MaKH,@MaDT=MaDT,@MaKM=MaKM,@Ngay=CONVERT(DATE,NgayLap)
        FROM HOADON WITH (UPDLOCK,HOLDLOCK) WHERE MaHD=@MaHD;
        IF @TrangThai IS NULL OR @TrangThai<>N'Chua thanh toan'
            THROW 51101,N'Hoa don khong ton tai hoac da chot/huy.',1;
        IF @Tong<=0 THROW 51102,N'Hoa don phai co mon.',1;
        IF EXISTS (SELECT 1 FROM CHITIET_THANHTOAN WHERE MaHD=@MaHD)
            THROW 51103,N'Thu tuc nhan toan bo phan bo TT; don nay da co dong TT.',1;
        IF @PhaiTra<>COALESCE((SELECT SUM(SoTien) FROM @ThanhToan),0)
            THROW 51104,N'Tong phan bo thanh toan khong bang ThanhTien.',1;
        IF @MaKM IS NOT NULL AND NOT EXISTS
           (SELECT 1 FROM KHUYENMAI WHERE MaKM=@MaKM AND @Ngay BETWEEN NgayBD AND NgayKT)
            THROW 51105,N'Khuyen mai khong con hieu luc tai ngay lap don.',1;
        IF EXISTS (SELECT 1 FROM CHITIET_HD d JOIN SANPHAM s ON s.MaSP=d.MaSP
                   WHERE d.MaHD=@MaHD AND s.TrangThai<>N'Dang ban')
            THROW 51106,N'Hoa don co mon da ngung ban.',1;
        IF EXISTS (SELECT 1 FROM CHITIET_HD d WHERE d.MaHD=@MaHD
                   AND NOT EXISTS (SELECT 1 FROM CONGTHUC c WHERE c.MaSP=d.MaSP))
            THROW 51107,N'Mon chua co cong thuc. Hang ban san can nguyen lieu thanh pham.',1;
        SET @NgayTT=COALESCE(@NgayTT,SYSDATETIME());
        IF @NgayTT < (SELECT NgayLap FROM HOADON WHERE MaHD=@MaHD)
            THROW 51108,N'Ngay thanh toan khong duoc truoc ngay lap.',1;
        INSERT INTO TIEUHAO_HOADON(MaHD,MaNVL,SoLuong)
        SELECT @MaHD,c.MaNVL,SUM(d.SoLuong*c.DinhMuc)
        FROM CHITIET_HD d JOIN CONGTHUC c ON c.MaSP=d.MaSP
        WHERE d.MaHD=@MaHD GROUP BY c.MaNVL;
        INSERT INTO CHITIET_THANHTOAN(MaHD,MaPT,SoTien,NgayTT,MaThamChieu)
        SELECT @MaHD,MaPT,SoTien,@NgayTT,MaThamChieu FROM @ThanhToan;
        DECLARE @Phi DECIMAL(5,2)=0;
        IF @MaDT IS NOT NULL SELECT @Phi=ChietKhau FROM DOITACGIAOHANG WHERE MaDT=@MaDT;
        UPDATE HOADON SET TrangThai=N'Da thanh toan',NgayThanhToan=@NgayTT,
               PhanTramPhiDoiTac=@Phi,PhiDoiTac=ROUND((@Tong-@Giam)*@Phi/100,0)
        WHERE MaHD=@MaHD;
        DECLARE @Diem INT=CONVERT(INT,FLOOR((@Tong-@Giam)/1000));
        IF @MaKH IS NOT NULL AND @Diem>0
            INSERT INTO LICHSU_DIEM(MaKH,MaHD,NgayGD,Loai,SoDiem,LyDo)
            VALUES(@MaKH,@MaHD,@NgayTT,N'Cong',@Diem,N'Tich diem mua hang');
        COMMIT;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT>0 ROLLBACK;
        THROW;
    END CATCH
END;
GO
CREATE PROCEDURE dbo.sp_HuyHoaDon @MaHD CHAR(10) AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    BEGIN TRY
        BEGIN TRAN;
        IF NOT EXISTS (SELECT 1 FROM HOADON WITH (UPDLOCK,HOLDLOCK)
                       WHERE MaHD=@MaHD AND TrangThai=N'Chua thanh toan')
            THROW 51109,N'Chi huy duoc hoa don dang mo.',1;
        UPDATE HOADON SET TrangThai=N'Huy',
               TrangThaiGiao=CASE WHEN LoaiDon=N'Giao hang' THEN N'Huy giao' ELSE NULL END
        WHERE MaHD=@MaHD;
        COMMIT;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT>0 ROLLBACK;
        THROW;
    END CATCH
END;
GO
CREATE PROCEDURE dbo.sp_TraCongNo
    @MaCN CHAR(10),@MaTC CHAR(10),@MaNV CHAR(5),@MaPT CHAR(5),
    @SoTien DECIMAL(18,0),@NgayTra DATETIME2(0)=NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    BEGIN TRY
        BEGIN TRAN;
        IF NOT EXISTS (SELECT 1 FROM CONGNO_NCC WITH (UPDLOCK,HOLDLOCK) WHERE MaCN=@MaCN)
            THROW 51110,N'Cong no khong ton tai.',1;
        IF @SoTien<=0 OR @SoTien>(SELECT ConNo FROM vw_CongNoNCC WHERE MaCN=@MaCN)
            THROW 51111,N'So tien tra no khong hop le.',1;
        INSERT INTO PHIEUTHUCHI(MaTC,NgayLap,MaDMTC,MaNV,SoTien,GhiChu)
        VALUES(@MaTC,COALESCE(@NgayTra,SYSDATETIME()),'TC011',@MaNV,@SoTien,N'Tra no nha cung cap');
        INSERT INTO THANHTOAN_CONGNO(MaCN,MaTC,MaPT) VALUES(@MaCN,@MaTC,@MaPT);
        COMMIT;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT>0 ROLLBACK;
        THROW;
    END CATCH
END;
GO
-- Chot kiem ke ngay sau khi dem, khi quầy tam ngung nhap/xuat/ban hang.
-- TonHeThong phai bang ton hien tai; phieu lich su chi de xem, khong duoc ap dung lai.
CREATE PROCEDURE dbo.sp_ChotKiemKe @MaKK CHAR(10) AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    BEGIN TRY
        BEGIN TRAN;
        IF NOT EXISTS (SELECT 1 FROM PHIEUKIEMKE WITH (UPDLOCK,HOLDLOCK)
                       WHERE MaKK=@MaKK AND DaDieuChinh=0 AND NgayKK=CONVERT(DATE,SYSDATETIME()))
            THROW 51112,N'Chi chot phieu kiem ke hom nay chua dieu chinh.',1;
        IF NOT EXISTS (SELECT 1 FROM CHITIET_KK WHERE MaKK=@MaKK)
            THROW 51113,N'Phieu kiem ke chua co chi tiet.',1;
        IF EXISTS (SELECT 1 FROM CHITIET_KK k JOIN NGUYENVATLIEU n WITH (UPDLOCK,HOLDLOCK)
                   ON n.MaNVL=k.MaNVL WHERE k.MaKK=@MaKK AND k.TonHeThong<>n.SoLuongTon)
            THROW 51114,N'Ton da thay doi sau khi dem, can kiem ke lai.',1;
        UPDATE n SET SoLuongTon=k.TonThucTe FROM NGUYENVATLIEU n
        JOIN CHITIET_KK k ON k.MaNVL=n.MaNVL WHERE k.MaKK=@MaKK;
        UPDATE PHIEUKIEMKE SET DaDieuChinh=1 WHERE MaKK=@MaKK;
        COMMIT;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT>0 ROLLBACK;
        THROW;
    END CATCH
END;
GO
CREATE TRIGGER TR_KK_LichSu ON CHITIET_KK AFTER INSERT,UPDATE,DELETE AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (SELECT 1 FROM PHIEUKIEMKE p WHERE p.DaDieuChinh=1
       AND p.MaKK IN (SELECT MaKK FROM inserted UNION SELECT MaKK FROM deleted))
        THROW 51115,N'Khong sua chi tiet kiem ke da chot.',1;
END;
GO
