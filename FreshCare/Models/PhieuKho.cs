namespace FreshCare.Models
{
    /// <summary>
    /// Phiếu nhập kho
    /// </summary>
    public class PhieuNhapKho
    {
        public int MaPhieuNhap { get; set; }
        public DateTime NgayNhap { get; set; }
        public int MaNV { get; set; }
        public string? GhiChu { get; set; }

        // Hiển thị
        public string? TenNhanVien { get; set; }
        public string? TenMatHang { get; set; }
        public List<ChiTietNhap> ChiTietNhaps { get; set; } = new();
    }

    /// <summary>
    /// Chi tiết nhập kho - liên kết PhieuNhapKho và LoHang
    /// </summary>
    public class ChiTietNhap
    {
        public int MaChiTietNhap { get; set; }
        public int MaPhieuNhap { get; set; }
        public int MaLo { get; set; }
        public decimal SoLuong { get; set; }

        // Hiển thị
        public string? TenSP { get; set; }
        public string? DonViTinh { get; set; }
        public DateTime HanSuDung { get; set; }
    }

    /// <summary>
    /// Phiếu xuất kho
    /// Luật #12: LoaiPhieu = "Bán Hàng" hoặc "Hủy Hàng"
    /// Luật #13: Doanh thu chỉ tính từ "Bán Hàng"; "Hủy Hàng" tính vào thất thoát
    /// </summary>
    public class PhieuXuat
    {
        public int MaPhieuXuat { get; set; }
        public DateTime NgayXuat { get; set; }
        public int MaNV { get; set; }
        public string LoaiPhieu { get; set; } = "Bán Hàng"; // 'Bán Hàng' hoặc 'Hủy Hàng'
        public decimal TongTien { get; set; }
        public string? GhiChu { get; set; }

        // Hiển thị
        public string? TenNhanVien { get; set; }
        public string? TenMatHang { get; set; }
        public List<ChiTietXuat> ChiTietXuats { get; set; } = new();
    }

    /// <summary>
    /// Chi tiết xuất kho - ghi nhận từng lô bị trừ theo FEFO
    /// </summary>
    public class ChiTietXuat
    {
        public int MaChiTietXuat { get; set; }
        public int MaPhieuXuat { get; set; }
        public int MaLo { get; set; }
        public decimal SoLuong { get; set; }
        public decimal DonGia { get; set; }

        // Hiển thị
        public string? TenSP { get; set; }
        public string? DonViTinh { get; set; }
        public DateTime HanSuDung { get; set; }
    }
}
