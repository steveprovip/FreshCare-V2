using System;

namespace FreshCare.Models
{
    public class LichSuHeThong
    {
        public int MaLog { get; set; }
        public int MaNV { get; set; }
        public string ThaoTac { get; set; } = null!;
        public string? ChiTiet { get; set; }
        public DateTime NgayTao { get; set; }

        public string? TenNhanVien { get; set; } // Dùng cho View
    }
}
