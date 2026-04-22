namespace FreshCare.Models
{
    /// <summary>
    /// Bảng Danh mục sản phẩm - chứa PhanTramSale cho giá cận date
    /// Luật #8: Tự động tính giá sale theo % giảm giá của từng danh mục
    /// </summary>
    public class DanhMuc
    {
        public int MaDanhMuc { get; set; }
        public string TenDanhMuc { get; set; } = string.Empty;
        public decimal PhanTramSale { get; set; } // % giảm giá khi cận date
    }
}
