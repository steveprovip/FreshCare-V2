using System.ComponentModel.DataAnnotations;

namespace FreshCare.Models.ViewModels
{
    /// <summary>
    /// ViewModel cho trang Đăng nhập
    /// </summary>
    public class DangNhapViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập tên đăng nhập")]
        public string TenDangNhap { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập mật khẩu")]
        public string MatKhau { get; set; } = string.Empty;
    }

    /// <summary>
    /// ViewModel cho trang Đăng ký nhân viên
    /// </summary>
    public class DangKyViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập họ tên")]
        public string HoTen { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập tên đăng nhập")]
        [MinLength(4, ErrorMessage = "Tên đăng nhập tối thiểu 4 ký tự")]
        public string TenDangNhap { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập mật khẩu")]
        [MinLength(6, ErrorMessage = "Mật khẩu tối thiểu 6 ký tự")]
        public string MatKhau { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng xác nhận mật khẩu")]
        [Compare("MatKhau", ErrorMessage = "Mật khẩu xác nhận không khớp")]
        public string XacNhanMatKhau { get; set; } = string.Empty;

        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        public string? Email { get; set; }

        [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
        public string? SoDienThoai { get; set; }
    }

    /// <summary>
    /// ViewModel Bước 1: Xác minh danh tính để quên mật khẩu
    /// </summary>
    public class QuenMatKhauViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập tên đăng nhập")]
        public string TenDangNhap { get; set; } = string.Empty;

        // Nhập một trong hai để xác minh
        public string? Email { get; set; }
        public string? SoDienThoai { get; set; }
    }

    /// <summary>
    /// ViewModel Bước 2: Đặt mật khẩu mới
    /// </summary>
    public class DatLaiMatKhauViewModel
    {
        public int MaNV { get; set; }
        public string TenDangNhap { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập mật khẩu mới")]
        [MinLength(6, ErrorMessage = "Mật khẩu tối thiểu 6 ký tự")]
        public string MatKhauMoi { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng xác nhận mật khẩu mới")]
        [Compare("MatKhauMoi", ErrorMessage = "Mật khẩu xác nhận không khớp")]
        public string XacNhanMatKhauMoi { get; set; } = string.Empty;
    }

    /// <summary>
    /// ViewModel Dashboard tổng hợp
    /// </summary>
    public class DashboardViewModel
    {
        public List<LoHang> LoHangQuaHan { get; set; } = new();   // Đỏ
        public List<LoHang> LoHangCanDate { get; set; } = new();  // Cam
        public List<LoHang> LoHangAnToan { get; set; } = new();   // Xanh
        public int TongSanPham { get; set; }
        public int TongLoHang { get; set; }
        public int SoLoQuaHan { get; set; }
        public int SoLoCanDate { get; set; }
    }

    /// <summary>
    /// ViewModel cho form Nhập kho
    /// </summary>
    public class NhapKhoViewModel
    {
        [Required(ErrorMessage = "Vui lòng chọn sản phẩm")]
        public int MaSP { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập số lượng")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Số lượng phải lớn hơn 0")]
        public decimal SoLuong { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập ngày sản xuất")]
        public DateTime NgaySanXuat { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập hạn sử dụng")]
        public DateTime HanSuDung { get; set; }

        public string? GhiChu { get; set; }

        // Hiển thị dropdown
        public List<SanPham> DanhSachSanPham { get; set; } = new();
    }

    /// <summary>
    /// ViewModel cho form Xuất kho
    /// </summary>
    public class XuatKhoViewModel
    {
        [Required(ErrorMessage = "Vui lòng chọn sản phẩm")]
        public int MaSP { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập số lượng")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Số lượng phải lớn hơn 0")]
        public decimal SoLuong { get; set; }

        public string? GhiChu { get; set; }

        // Hiển thị
        public List<SanPham> DanhSachSanPham { get; set; } = new();
        public List<LoHang> DanhSachLoHang { get; set; } = new();
    }

    /// <summary>
    /// ViewModel cho Báo cáo
    /// </summary>
    public class BaoCaoViewModel
    {
        public DateTime? TuNgay { get; set; }
        public DateTime? DenNgay { get; set; }

        // Tồn kho
        public List<BaoCaoTonKho> TonKhoList { get; set; } = new();

        // Doanh thu (chỉ phiếu Bán Hàng - Luật #13)
        public decimal TongDoanhThu { get; set; }

        // Hao hụt (phiếu Hủy Hàng - Luật #13)
        public decimal TongThatThoat { get; set; }
        public List<PhieuXuat> PhieuHuyList { get; set; } = new();
        public List<PhieuXuat> PhieuBanList { get; set; } = new();
    }

    /// <summary>
    /// Chi tiết tồn kho theo sản phẩm
    /// </summary>
    public class BaoCaoTonKho
    {
        public int MaSP { get; set; }
        public string TenSP { get; set; } = string.Empty;
        public string DonViTinh { get; set; } = string.Empty;
        public string TenDanhMuc { get; set; } = string.Empty;
        public decimal TongTon { get; set; }
        public int SoLo { get; set; }
        public List<LoHang> ChiTietLo { get; set; } = new();
    }
    //hien thi hoa don
    public class HoaDonViewModel
    {
        public int HoaDonId { get; set; }
        public DateTime NgayLap { get; set; }
        public string? TenKhachHang { get; set; }

        public List<ChiTietHoaDon>? ChiTiet { get; set; }

        public decimal TongTien { get; set; }
    }
    public class ChiTietPhieuXuatViewModel
    {
        public int MaPhieuXuat { get; set; }
        public DateTime NgayXuat { get; set; }
        public string LoaiPhieu { get; set; }
        public decimal TongTien { get; set; }
        public string TenNhanVien { get; set; }

        public List<ChiTietItem> DanhSachChiTiet { get; set; } = new List<ChiTietItem>();
    }

    public class ChiTietItem
    {
        public int MaLo { get; set; }
        public string TenSanPham { get; set; }
        public decimal SoLuong { get; set; }
        public decimal DonGia { get; set; }
    }
}
