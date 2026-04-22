using System.ComponentModel.DataAnnotations;

namespace FreshCare.Models
{
    public class ChiTietHoaDon
    {
        [Key]
        public int ChiTietHoaDonId { get; set; }

        public int HoaDonId { get; set; }
        public HoaDon? HoaDon { get; set; }

        public int SanPhamId { get; set; }
        public SanPham? SanPham { get; set; }

        public int SoLuong { get; set; }
        public decimal DonGia { get; set; }

        public decimal ThanhTien => SoLuong * DonGia;
    }
}
