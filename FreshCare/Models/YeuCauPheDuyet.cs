using System;

namespace FreshCare.Models
{
    public class YeuCauPheDuyet
    {
        public int MaYeuCau { get; set; }
        public int MaNV { get; set; }
        public string PhanHe { get; set; } = null!;
        public string LoaiChinhSua { get; set; } = null!;
        public int MaBanGhi { get; set; }
        public string? DuLieuCu { get; set; }
        public string DuLieuMoi { get; set; } = null!;
        public DateTime NgayGui { get; set; }
        public string TrangThai { get; set; } = null!; // Chờ Duyệt, Đã Duyệt, Từ Chối
        public int? NguoiDuyet { get; set; }
        public DateTime? NgayDuyet { get; set; }
        public string? GhiChu { get; set; }

        public string? TenNhanVienGhi { get; set; } // Dùng cho View
        public string? TenNguoiDuyet { get; set; } // Dùng cho View
    }
}
