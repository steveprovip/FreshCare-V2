using FreshCare.Helpers;
using FreshCare.Models;
using FreshCare.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace FreshCare.Controllers
{
    /// <summary>
    /// HomeController - Dashboard cảnh báo hạn sử dụng
    /// Luật #7: Tự động phân loại Quá hạn (Đỏ), Cận date (Cam), An toàn (Xanh)
    /// Luật #6: Số ngày còn lại >= 0
    /// Luật #8: Tự động tính giá sale cho hàng cận date
    /// </summary>
    public class HomeController : Controller
    {
        private readonly string _connectionString;

        public HomeController(string connectionString)
        {
            _connectionString = connectionString;
        }

        // GET: /Home/Index - Dashboard cảnh báo HSD
        public IActionResult Index()
        {
            if (HttpContext.Session.GetInt32("MaNV") == null)
                return RedirectToAction("DangNhap", "TaiKhoan");

            var model = new DashboardViewModel();

            try
            {
                using (var conn = DatabaseHelper.GetConnection(_connectionString))
                {
                    conn.Open();

                    // Cập nhật trạng thái lô hàng tự động (Luật #7)
                    CapNhatTrangThaiLoHang(conn);

                    // Lấy danh sách lô hàng còn tồn kho
                    string sql = @"SELECT lh.MaLo, lh.MaSP, lh.SoLuongNhap, lh.SoLuongTon,
                                          lh.NgaySanXuat, lh.HanSuDung, lh.NgayNhapKho, lh.TrangThai,
                                          sp.TenSP, sp.DonViTinh, sp.GiaBan,
                                          dm.TenDanhMuc, dm.PhanTramSale
                                   FROM LoHang lh
                                   INNER JOIN SanPham sp ON lh.MaSP = sp.MaSP
                                   INNER JOIN DanhMuc dm ON sp.MaDanhMuc = dm.MaDanhMuc
                                   WHERE lh.SoLuongTon > 0 AND lh.TrangThai != N'Đã Hủy'
                                   ORDER BY lh.HanSuDung ASC";

                    using (var cmd = new SqlCommand(sql, conn))
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var lo = new LoHang
                            {
                                MaLo = Convert.ToInt32(reader["MaLo"]),
                                MaSP = Convert.ToInt32(reader["MaSP"]),
                                SoLuongNhap = Convert.ToDecimal(reader["SoLuongNhap"]),
                                SoLuongTon = Convert.ToDecimal(reader["SoLuongTon"]),
                                NgaySanXuat = Convert.ToDateTime(reader["NgaySanXuat"]),
                                HanSuDung = Convert.ToDateTime(reader["HanSuDung"]),
                                NgayNhapKho = Convert.ToDateTime(reader["NgayNhapKho"]),
                                TrangThai = reader["TrangThai"].ToString()!,
                                TenSP = reader["TenSP"].ToString(),
                                DonViTinh = reader["DonViTinh"].ToString(),
                                GiaBanGoc = Convert.ToDecimal(reader["GiaBan"]),
                                TenDanhMuc = reader["TenDanhMuc"].ToString(),
                                PhanTramSale = Convert.ToDecimal(reader["PhanTramSale"])
                            };

                            // Phân loại vào 3 bảng theo màu
                            switch (lo.TrangThai)
                            {
                                case "Quá Hạn":
                                    model.LoHangQuaHan.Add(lo);
                                    break;
                                case "Cận Date":
                                    model.LoHangCanDate.Add(lo);
                                    break;
                                default:
                                    model.LoHangAnToan.Add(lo);
                                    break;
                            }
                        }
                    }

                    // Thống kê tổng
                    model.SoLoQuaHan = model.LoHangQuaHan.Count;
                    model.SoLoCanDate = model.LoHangCanDate.Count;

                    using (var countCmd = new SqlCommand("SELECT COUNT(*) FROM SanPham WHERE TrangThai = N'HoatDong'", conn))
                        model.TongSanPham = Convert.ToInt32(countCmd.ExecuteScalar());

                    using (var countCmd = new SqlCommand("SELECT COUNT(*) FROM LoHang WHERE SoLuongTon > 0 AND TrangThai != N'Đã Hủy'", conn))
                        model.TongLoHang = Convert.ToInt32(countCmd.ExecuteScalar());
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Lỗi tải Dashboard: " + ex.Message;
            }

            return View(model);
        }

        /// <summary>
        /// Cập nhật tự động trạng thái tất cả lô hàng dựa trên ngày hiện tại
        /// Luật #7: Quá hạn (HSD < Today), Cận Date (HSD < Today + 30), An Toàn
        /// </summary>
        private void CapNhatTrangThaiLoHang(SqlConnection conn)
        {
            string updateSql = @"
                UPDATE LoHang SET TrangThai = 
                    CASE 
                        WHEN HanSuDung < CAST(GETDATE() AS DATE) THEN N'Quá Hạn'
                        WHEN DATEDIFF(DAY, CAST(GETDATE() AS DATE), HanSuDung) < 30 THEN N'Cận Date'
                        ELSE N'An Toàn'
                    END
                WHERE TrangThai != N'Đã Hủy' AND SoLuongTon > 0";

            using (var cmd = new SqlCommand(updateSql, conn))
            {
                cmd.ExecuteNonQuery();
            }
        }

        public IActionResult Error()
        {
            return View();
        }
    }
}
