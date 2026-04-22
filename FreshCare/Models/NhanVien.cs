namespace FreshCare.Models
{
    /// <summary>
    /// Bảng Nhân viên - Tác nhân tạo PhieuNhap, HoaDon
    /// </summary>
    public class NhanVien
    {
        public int MaNV { get; set; }
        public string HoTen { get; set; } = string.Empty;
        public string TenDangNhap { get; set; } = string.Empty;
        public string MatKhau { get; set; } = string.Empty; // SHA256 hash
        public string VaiTro { get; set; } = "NhanVien"; // Admin / NhanVien
        public string TrangThai { get; set; } = "HoatDong"; // HoatDong / DaKhoa
        public string? Email { get; set; }       // Dùng để xác minh quên mật khẩu
        public string? SoDienThoai { get; set; } // Dùng để xác minh quên mật khẩu
    }
}
