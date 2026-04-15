USE FreshCareDB;
GO

-- 1. BẢNG CẤU HÌNH
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'CauHinh')
BEGIN
    CREATE TABLE CauHinh (
        MaConfig VARCHAR(50) PRIMARY KEY,
        TenThamSo NVARCHAR(100) NOT NULL,
        GiaTri NVARCHAR(255) NOT NULL
    );
    INSERT INTO CauHinh VALUES ('BAN_CHAM_SO_LUONG', N'Số lượng bán tối thiểu', '5');
    INSERT INTO CauHinh VALUES ('BAN_CHAM_SO_NGAY', N'Số ngày xét bán chậm (ngày)', '30');
END
GO

-- 2. BẢNG LỊCH SỬ HỆ THỐNG
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'LichSuHeThong')
BEGIN
    CREATE TABLE LichSuHeThong (
        MaLog INT IDENTITY(1,1) PRIMARY KEY,
        MaNV INT NOT NULL,
        ThaoTac NVARCHAR(50) NOT NULL,
        ChiTiet NVARCHAR(MAX) NULL,
        NgayTao DATETIME NOT NULL DEFAULT GETDATE(),
        CONSTRAINT FK_LichSu_NhanVien FOREIGN KEY (MaNV) REFERENCES NhanVien(MaNV)
    );
END
GO

-- 3. BẢNG YÊU CẦU PHÊ DUYỆT
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'YeuCauPheDuyet')
BEGIN
    CREATE TABLE YeuCauPheDuyet (
        MaYeuCau INT IDENTITY(1,1) PRIMARY KEY,
        MaNV INT NOT NULL,
        PhanHe NVARCHAR(50) NOT NULL,
        LoaiChinhSua NVARCHAR(50) NOT NULL,
        MaBanGhi INT NOT NULL,
        DuLieuCu NVARCHAR(MAX) NULL,
        DuLieuMoi NVARCHAR(MAX) NOT NULL,
        NgayGui DATETIME NOT NULL DEFAULT GETDATE(),
        TrangThai NVARCHAR(20) NOT NULL DEFAULT N'Chờ Duyệt',
        NguoiDuyet INT NULL,
        NgayDuyet DATETIME NULL,
        GhiChu NVARCHAR(500) NULL,
        CONSTRAINT FK_YeuCau_NV FOREIGN KEY (MaNV) REFERENCES NhanVien(MaNV),
        CONSTRAINT FK_YeuCau_Duyet FOREIGN KEY (NguoiDuyet) REFERENCES NhanVien(MaNV)
    );
END
GO
