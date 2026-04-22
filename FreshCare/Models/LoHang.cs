namespace FreshCare.Models
{
    /// <summary>
    /// Bảng Lô hàng (Batch) - BẢNG LÕI CỦA HỆ THỐNG
    /// Mỗi lần nhập hàng tạo 1 lô mới gắn NSX + HSD
    /// Luật #7: Tự động phân loại trạng thái: Quá hạn (Đỏ), Cận date <14 ngày (Cam), An toàn (Xanh)
    /// Luật #6: Số ngày còn lại hiển thị >= 0
    /// </summary>
    public class LoHang
    {
        public int MaLo { get; set; }
        public int MaSP { get; set; }
        public decimal SoLuongNhap { get; set; }
        public decimal SoLuongTon { get; set; }
        public DateTime NgaySanXuat { get; set; }
        public DateTime HanSuDung { get; set; }
        public DateTime NgayNhapKho { get; set; }
        public string TrangThai { get; set; } = "An Toàn"; // 'An Toàn','Cận Date','Quá Hạn','Đã Hủy'

        // === Thuộc tính tính toán (không lưu DB) ===
        public string? TenSP { get; set; }
        public string? DonViTinh { get; set; }
        public string? TenDanhMuc { get; set; }
        public decimal GiaBanGoc { get; set; }
        public decimal PhanTramSale { get; set; }

        /// <summary>
        /// Luật #6: Số ngày còn lại, không cho phép giá trị âm
        /// </summary>
        public int SoNgayConLai
        {
            get
            {
                int days = (HanSuDung.Date - DateTime.Now.Date).Days;
                return days < 0 ? 0 : days;
            }
        }

        /// <summary>
        /// Tỷ lệ giảm giá thực tế lũy tiến theo số ngày còn lại (Luật mới)
        /// Càng gần ngày hết hạn càng giảm sâu.
        /// </summary>
        public decimal PhanTramSaleHienTai
        {
            get
            {
                if (TrangThai == "Cận Date" && PhanTramSale > 0)
                {
                    // Công thức lũy tiến: Base + (Base * (14 - soNgay) / 14)
                    decimal factor = (decimal)(14 - SoNgayConLai) / 14m;
                    decimal actualSale = PhanTramSale * (1m + factor);
                    
                    // Giới hạn giảm tối đa 80% để tránh mất trắng
                    return actualSale > 80m ? 80m : Math.Round(actualSale, 2);
                }
                return 0;
            }
        }

        /// <summary>
        /// Luật #8: Giá thực tế (đã Sale lũy tiến nếu cận date)
        /// </summary>
        public decimal GiaThucTe
        {
            get
            {
                if (TrangThai == "Cận Date" && PhanTramSaleHienTai > 0)
                    return GiaBanGoc * (100 - PhanTramSaleHienTai) / 100;
                return GiaBanGoc;
            }
        }

        /// <summary>
        /// Màu sắc CSS cho Dashboard (Đỏ/Cam/Xanh)
        /// </summary>
        public string MauCanhBao
        {
            get
            {
                return TrangThai switch
                {
                    "Quá Hạn" => "danger",    // Đỏ
                    "Cận Date" => "warning",   // Cam
                    "Đã Hủy" => "secondary",  // Xám
                    _ => "success"             // Xanh
                };
            }
        }
    }
}
