using System.ComponentModel.DataAnnotations;

namespace FreshCare.Models
{
    public class HoaDon
    {
        [Key]
        public int HoaDonId { get; set; }

        public DateTime NgayLap { get; set; }

        public string? TenKhachHang { get; set; }

        public decimal TongTien { get; set; }

        // Quan hệ
        public List<ChiTietHoaDon>? ChiTietHoaDons { get; set; }
    }
}
