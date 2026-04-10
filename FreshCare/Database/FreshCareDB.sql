-- =============================================
-- HỆ THỐNG QUẢN LÝ KHO & HẠN SỬ DỤNG FRESHCARE
-- SQL Server 2022 - Tạo CSDL (CLEAN INSTALL)
-- =============================================

USE master;
GO

IF EXISTS (SELECT name FROM sys.databases WHERE name = N'FreshCareDB')
BEGIN
    ALTER DATABASE FreshCareDB SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE FreshCareDB;
END
GO

CREATE DATABASE FreshCareDB;
GO

USE FreshCareDB;
GO

-- =============================================
-- 1. BẢNG NHÂN VIÊN
-- =============================================
CREATE TABLE NhanVien (
    MaNV        INT IDENTITY(1,1) PRIMARY KEY,
    HoTen       NVARCHAR(100) NOT NULL,
    TenDangNhap NVARCHAR(50) NOT NULL UNIQUE,
    MatKhau     NVARCHAR(255) NOT NULL,
    VaiTro      NVARCHAR(20) NOT NULL DEFAULT N'NhanVien',
    TrangThai   NVARCHAR(20) NOT NULL DEFAULT N'HoatDong'
);
GO

-- =============================================
-- 2. BẢNG DANH MỤC
-- =============================================
CREATE TABLE DanhMuc (
    MaDanhMuc    INT IDENTITY(1,1) PRIMARY KEY,
    TenDanhMuc   NVARCHAR(100) NOT NULL UNIQUE,
    PhanTramSale DECIMAL(5,2) NOT NULL DEFAULT 0
);
GO

-- =============================================
-- 3. BẢNG SẢN PHẨM
-- =============================================
CREATE TABLE SanPham (
    MaSP        INT IDENTITY(1,1) PRIMARY KEY,
    TenSP       NVARCHAR(200) NOT NULL,
    DonViTinh   NVARCHAR(20) NOT NULL,
    GiaBan      DECIMAL(18,2) NOT NULL,
    MaDanhMuc   INT NOT NULL,
    MoTa        NVARCHAR(500) NULL,
    MaVach      NVARCHAR(50) NULL,
    TrangThai   NVARCHAR(20) NOT NULL DEFAULT N'HoatDong',
    CONSTRAINT FK_SanPham_DanhMuc FOREIGN KEY (MaDanhMuc) REFERENCES DanhMuc(MaDanhMuc)
);
GO

-- =============================================
-- 4. BẢNG LÔ HÀNG
-- =============================================
CREATE TABLE LoHang (
    MaLo        INT IDENTITY(1,1) PRIMARY KEY,
    MaSP        INT NOT NULL,
    SoLuongNhap DECIMAL(18,2) NOT NULL,
    SoLuongTon  DECIMAL(18,2) NOT NULL,
    NgaySanXuat DATE NOT NULL,
    HanSuDung   DATE NOT NULL,
    NgayNhapKho DATETIME NOT NULL DEFAULT GETDATE(),
    TrangThai   NVARCHAR(20) NOT NULL DEFAULT N'An Toàn',
    CONSTRAINT FK_LoHang_SanPham FOREIGN KEY (MaSP) REFERENCES SanPham(MaSP),
    CONSTRAINT CK_LoHang_HSD CHECK (HanSuDung >= NgaySanXuat),
    CONSTRAINT CK_LoHang_SoLuong CHECK (SoLuongTon >= 0)
);
GO

-- =============================================
-- 5. BẢNG PHIẾU NHẬP KHO
-- =============================================
CREATE TABLE PhieuNhapKho (
    MaPhieuNhap INT IDENTITY(1,1) PRIMARY KEY,
    NgayNhap    DATETIME NOT NULL DEFAULT GETDATE(),
    MaNV        INT NOT NULL,
    GhiChu      NVARCHAR(500) NULL,
    CONSTRAINT FK_PhieuNhap_NhanVien FOREIGN KEY (MaNV) REFERENCES NhanVien(MaNV)
);
GO

-- =============================================
-- 6. BẢNG CHI TIẾT NHẬP
-- =============================================
CREATE TABLE ChiTietNhap (
    MaChiTietNhap INT IDENTITY(1,1) PRIMARY KEY,
    MaPhieuNhap   INT NOT NULL,
    MaLo          INT NOT NULL,
    SoLuong       DECIMAL(18,2) NOT NULL,
    CONSTRAINT FK_CTNhap_PhieuNhap FOREIGN KEY (MaPhieuNhap) REFERENCES PhieuNhapKho(MaPhieuNhap),
    CONSTRAINT FK_CTNhap_LoHang FOREIGN KEY (MaLo) REFERENCES LoHang(MaLo)
);
GO

-- =============================================
-- 7. BẢNG PHIẾU XUẤT
-- =============================================
CREATE TABLE PhieuXuat (
    MaPhieuXuat INT IDENTITY(1,1) PRIMARY KEY,
    NgayXuat    DATETIME NOT NULL DEFAULT GETDATE(),
    MaNV        INT NOT NULL,
    LoaiPhieu   NVARCHAR(20) NOT NULL,
    TongTien    DECIMAL(18,2) NOT NULL DEFAULT 0,
    GhiChu      NVARCHAR(500) NULL,
    CONSTRAINT FK_PhieuXuat_NhanVien FOREIGN KEY (MaNV) REFERENCES NhanVien(MaNV),
    CONSTRAINT CK_PhieuXuat_Loai CHECK (LoaiPhieu IN (N'Bán Hàng', N'Hủy Hàng'))
);
GO

-- =============================================
-- 8. BẢNG CHI TIẾT XUẤT
-- =============================================
CREATE TABLE ChiTietXuat (
    MaChiTietXuat INT IDENTITY(1,1) PRIMARY KEY,
    MaPhieuXuat   INT NOT NULL,
    MaLo          INT NOT NULL,
    SoLuong       DECIMAL(18,2) NOT NULL,
    DonGia        DECIMAL(18,2) NOT NULL,
    CONSTRAINT FK_CTXuat_PhieuXuat FOREIGN KEY (MaPhieuXuat) REFERENCES PhieuXuat(MaPhieuXuat),
    CONSTRAINT FK_CTXuat_LoHang FOREIGN KEY (MaLo) REFERENCES LoHang(MaLo)
);
GO

-- =============================================
-- DỮ LIỆU MẪU (THỰC TẾ HƠN)
-- =============================================

-- Tài khoản Admin (admin / admin123)
INSERT INTO NhanVien (HoTen, TenDangNhap, MatKhau, VaiTro, TrangThai)
VALUES (N'Quản Lý FreshCare', 'admin',
        '240be518fabd2724ddb6f04eeb1da5967448d7e831c08c8fa822809f74c720a9',
        N'Admin', N'HoatDong');

-- Tài khoản nhân viên mẫu (nhanvien1 / nhanvien123)
INSERT INTO NhanVien (HoTen, TenDangNhap, MatKhau, VaiTro, TrangThai)
VALUES (N'Nguyễn Duy Linh', 'nhanvien1',
        'a4a63b2f73dde89c4ad73e94ebbd48659be1e8916b3c0bbee3f459c6f22e13c6',
        N'NhanVien', N'HoatDong');
GO

-- Danh mục mẫu
INSERT INTO DanhMuc (TenDanhMuc, PhanTramSale) VALUES
(N'Rau Củ Quả', 50),
(N'Thịt Cá Tươi Sống', 30),
(N'Trái Cây Nhập Khẩu', 40),
(N'Đồ Khô - Gia Vị - Nước Chấm', 15),
(N'Sữa & Sản Phẩm Từ Sữa', 35),
(N'Trứng & Đậu Phụ', 25);
GO

-- Sản phẩm mẫu (thực tế hơn với đơn vị tính chính xác)
INSERT INTO SanPham (TenSP, DonViTinh, GiaBan, MaDanhMuc, MoTa, MaVach) VALUES
-- Rau Củ Quả
(N'Rau muống hữu cơ',              N'Bó',   12000,  1, N'Rau muống sạch VietGAP 300g',     '8938505050101'),
(N'Cà chua cherry Đà Lạt',          N'Kg',   45000,  1, N'Cà chua bi Đà Lạt loại 1',       '8938505050102'),
(N'Bắp cải xanh',                   N'Kg',   18000,  1, N'Bắp cải VietGAP',                 NULL),
(N'Khoai tây Đà Lạt',               N'Kg',   25000,  1, N'Khoai tây sạch Đà Lạt',           NULL),

-- Thịt Cá Tươi Sống
(N'Thịt heo ba chỉ',                N'Kg',   135000, 2, N'Thịt heo sạch MEATDeli',          '8938505050201'),
(N'Cá hồi phi lê Na Uy',            N'Khay', 189000, 2, N'Cá hồi nhập khẩu khay 300g',     '8938505050202'),
(N'Tôm sú tươi',                    N'Kg',   220000, 2, N'Tôm sú nuôi size 20-25 con/kg',  NULL),

-- Trái Cây Nhập Khẩu
(N'Táo Envy New Zealand',            N'Kg',   95000,  3, N'Táo Envy xuất xứ NZ',             '8938505050301'),
(N'Nho xanh Mỹ',                    N'Kg',   120000, 3, N'Nho xanh không hạt nhập Mỹ',      NULL),
(N'Chuối Cavendish hữu cơ',         N'Nải',  28000,  3, N'Chuối organic Dole',               NULL),

-- Đồ Khô - Gia Vị - Nước Chấm
(N'Nước mắm Phú Quốc 500ml',        N'Chai', 42000,  4, N'Nước mắm truyền thống Phú Quốc',  '8938505050401'),
(N'Dầu ăn Neptune 1L',              N'Chai', 48000,  4, N'Dầu ăn Neptune Gold',              '8938505050402'),
(N'Bột nêm Knorr 900g',             N'Gói',  46000,  4, N'Bột nêm từ thịt và xương',        NULL),

-- Sữa & Sản Phẩm Từ Sữa
(N'Sữa tươi TH True Milk 1L',       N'Hộp',  32000,  5, N'Sữa tươi tiệt trùng nguyên chất 1 lít', '8938505050501'),
(N'Sữa tươi TH True Milk 180ml',    N'Vỉ',   38000,  5, N'Vỉ 4 hộp x 180ml (bán nguyên vỉ)',       '8938505050502'),
(N'Sữa chua Vinamilk có đường',     N'Vỉ',   25000,  5, N'Vỉ 4 hộp x 100g',                        '8938505050503'),

-- Trứng & Đậu Phụ
(N'Trứng gà ta',                     N'Vỉ',   38000,  6, N'Vỉ 10 quả trứng gà thả vườn',    '8938505050601'),
(N'Đậu phụ non Sojami',             N'Hộp',  15000,  6, N'Đậu phụ tươi đóng hộp 300g',      NULL);
GO

-- Lô hàng mẫu (nhiều trạng thái để test Dashboard)
INSERT INTO LoHang (MaSP, SoLuongNhap, SoLuongTon, NgaySanXuat, HanSuDung, TrangThai) VALUES
-- Rau muống: 2 lô, 1 cận date
(1, 40, 32,  '2026-04-01', '2026-04-08', N'Cận Date'),
(1, 60, 60,  '2026-04-04', '2026-04-18', N'An Toàn'),
-- Cà chua: 1 lô quá hạn
(2, 25, 18,  '2026-03-18', '2026-04-02', N'Quá Hạn'),
(2, 30, 30,  '2026-04-02', '2026-04-22', N'An Toàn'),
-- Thịt heo: cận date
(5, 15, 12,  '2026-04-02', '2026-04-09', N'Cận Date'),
(5, 20, 20,  '2026-04-04', '2026-04-15', N'An Toàn'),
-- Cá hồi: an toàn
(6, 20, 18,  '2026-04-03', '2026-04-28', N'An Toàn'),
-- Táo Envy: an toàn
(8, 50, 45,  '2026-04-01', '2026-05-15', N'An Toàn'),
-- Nước mắm: an toàn (hạn dài)
(11, 100, 95, '2026-01-15', '2027-01-15', N'An Toàn'),
-- Sữa TH 1L: cận date
(14, 30, 22, '2026-03-20', '2026-04-10', N'Cận Date'),
(14, 40, 40, '2026-04-01', '2026-04-25', N'An Toàn'),
-- Sữa TH 180ml vỉ
(15, 25, 25, '2026-04-02', '2026-05-02', N'An Toàn'),
-- Trứng gà
(17, 50, 42, '2026-04-01', '2026-04-20', N'An Toàn');
GO


--sua thong tin san pham va tao hoa don va xem hoa don
INSERT INTO SanPham (TenSP, DonViTinh, GiaBan, TrangThai)
VALUES (N'Sữa Vinamilk', N'Hộp', 10000, N'HoatDong');

PRINT N'=== Tạo CSDL FreshCareDB thành công! ===';
PRINT N'=== Tài khoản: admin / admin123 ===';
PRINT N'=== Tổng: 6 danh mục, 18 sản phẩm, 13 lô hàng mẫu ===';
GO
