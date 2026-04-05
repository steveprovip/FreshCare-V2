namespace FreshCare.Models
{
    /// <summary>
    /// Bảng Sản phẩm
    /// Luật #9: Bắt buộc có đơn vị tính (Bó, Kg, Hộp, Khay...)
    /// </summary>
    public class SanPham
    {
        public int MaSP { get; set; }
        public string TenSP { get; set; } = string.Empty;
        public string DonViTinh { get; set; } = string.Empty; // Bó, Kg, Hộp, Khay...
        public decimal GiaBan { get; set; }
        public int MaDanhMuc { get; set; }
        public string? MoTa { get; set; }
        public string? MaVach { get; set; } // Barcode dự phòng
        public string TrangThai { get; set; } = "HoatDong";

        // Navigation (không dùng EF, chỉ để hiển thị)
        public string? TenDanhMuc { get; set; }
        public decimal PhanTramSale { get; set; }
    }
}
