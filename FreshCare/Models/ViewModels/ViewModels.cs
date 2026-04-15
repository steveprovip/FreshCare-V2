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

        // Bổ sung cho Hàng Bán Chậm
        public List<ThongKeSanPham> SanPhamBanCham { get; set; } = new();
        public int SoNgayBanCham { get; set; } = 30; // Từ CauHinh
        public int SoLuongBanChamThreshold { get; set; } = 10; // Từ CauHinh
    }

    /// <summary>
    /// ViewModel cho form Nhập kho (hỗ trợ nhập nhiều mặt hàng)
    /// </summary>
    public class NhapKhoViewModel
    {
        // === Nhập đơn (backward compatible) ===
        public int MaSP { get; set; }
        public decimal SoLuong { get; set; }
        public DateTime NgaySanXuat { get; set; }
        public DateTime HanSuDung { get; set; }

        public string? GhiChu { get; set; }

        // === Nhập nhiều mặt hàng cùng lúc ===
        public List<NhapKhoItem> DanhSachNhap { get; set; } = new();

        // Hiển thị dropdown
        public List<SanPham> DanhSachSanPham { get; set; } = new();
    }

    /// <summary>
    /// Một dòng mặt hàng trong phiếu nhập kho
    /// </summary>
    public class NhapKhoItem
    {
        public int MaSP { get; set; }
        public decimal SoLuong { get; set; }
        public DateTime NgaySanXuat { get; set; }
        public DateTime HanSuDung { get; set; }
        public decimal HeSoQuyDoi { get; set; } = 1;
        public string? DonViQuyDoi { get; set; }
    }

    /// <summary>
    /// ViewModel cho form Xuất kho (hỗ trợ bán nhiều mặt hàng)
    /// </summary>
    public class XuatKhoViewModel
    {
        // === Xuất đơn (backward compatible) ===
        public int MaSP { get; set; }
        public decimal SoLuong { get; set; }

        public string? GhiChu { get; set; }

        // === Xuất nhiều mặt hàng cùng lúc ===
        public List<XuatKhoItem> DanhSachXuat { get; set; } = new();

        // Hiển thị
        public List<SanPham> DanhSachSanPham { get; set; } = new();
        public List<LoHang> DanhSachLoHang { get; set; } = new();
    }

    /// <summary>
    /// Một dòng mặt hàng trong phiếu xuất kho (bán hàng)
    /// </summary>
    public class XuatKhoItem
    {
        public int MaSP { get; set; }
        public decimal SoLuong { get; set; }
        public decimal HeSoQuyDoi { get; set; } = 1;
        public string? DonViQuyDoi { get; set; }
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

        // Thống kê mặt hàng bán chạy / chậm (Yêu cầu #5)
        public List<ThongKeSanPham> TopBanChay { get; set; } = new();
        public List<ThongKeSanPham> TopBanCham { get; set; } = new();
    }

    /// <summary>
    /// Thống kê mặt hàng bán chạy / chậm
    /// </summary>
    public class ThongKeSanPham
    {
        public int MaSP { get; set; }
        public string TenSP { get; set; } = string.Empty;
        public string DonViTinh { get; set; } = string.Empty;
        public decimal TongSoLuong { get; set; }
        public decimal TongDoanhThu { get; set; }
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

    /// <summary>
    /// ViewModel chi tiết phiếu nhập kho (để Xem / In)
    /// </summary>
    public class ChiTietPhieuNhapViewModel
    {
        public int MaPhieuNhap { get; set; }
        public DateTime NgayNhap { get; set; }
        public string TenNhanVien { get; set; } = string.Empty;
        public string? GhiChu { get; set; }
        public decimal TongTien { get; set; }
        public List<ChiTietNhapItem> DanhSachChiTiet { get; set; } = new();
    }

    public class ChiTietNhapItem
    {
        public int MaLo { get; set; }
        public string TenSanPham { get; set; } = string.Empty;
        public string DonViTinh { get; set; } = string.Empty;
        public decimal SoLuong { get; set; }
        public decimal GiaNhap { get; set; }
        public DateTime NgaySanXuat { get; set; }
        public DateTime HanSuDung { get; set; }
    }

    /// <summary>
    /// ViewModel In tổng hợp phiếu nhập trong ngày
    /// </summary>
    public class TongHopNhapNgayViewModel
    {
        public DateTime Ngay { get; set; }
        public string TenNhanVien { get; set; } = string.Empty;
        public List<ChiTietPhieuNhapViewModel> DanhSachPhieu { get; set; } = new();
        public decimal TongTien { get; set; }
    }

    /// <summary>
    /// ViewModel In tổng hợp phiếu xuất trong ngày
    /// </summary>
    public class TongHopXuatNgayViewModel
    {
        public DateTime Ngay { get; set; }
        public string TenNhanVien { get; set; } = string.Empty;
        public List<ChiTietPhieuXuatViewModel> DanhSachPhieu { get; set; } = new();
        public decimal TongTien { get; set; }
    }

    /// <summary>
    /// ViewModel cho trang Danh sách Sản phẩm (phân trang, tìm kiếm, sắp xếp)
    /// </summary>
    public class DanhSachSanPhamViewModel
    {
        public List<SanPhamChiTiet> DanhSach { get; set; } = new();
        public string? TimKiem { get; set; }
        public string SapXep { get; set; } = "az"; // az, za
        public int Trang { get; set; } = 1;
        public int TongTrang { get; set; }
        public int TongSanPham { get; set; }
        public int SoLuongMoiTrang { get; set; } = 10;
    }

    /// <summary>
    /// Sản phẩm + thông tin lô gần nhất (NSX, HSD)
    /// </summary>
    public class SanPhamChiTiet
    {
        public int MaSP { get; set; }
        public string TenSP { get; set; } = null!;
        public string DonViTinh { get; set; } = null!;
        public decimal GiaNhap { get; set; }
        public decimal GiaBan { get; set; }
        public int MaDanhMuc { get; set; }
        public string? TenDanhMuc { get; set; }
        public string? MoTa { get; set; }
        public string? MaVach { get; set; }
        public string TrangThai { get; set; } = null!;
        // Thông tin lô gần nhất
        public DateTime? NgaySanXuatGanNhat { get; set; }
        public DateTime? HanSuDungGanNhat { get; set; }
        public decimal TongTonKho { get; set; }
    }

    /// <summary>
    /// ViewModel cho trang Lịch sử hệ thống
    /// </summary>
    public class LichSuHeThongViewModel
    {
        public List<LichSuHeThong> DanhSach { get; set; } = new();
        public string? TimKiem { get; set; }
        public int Trang { get; set; } = 1;
        public int TongTrang { get; set; }
    }
}
