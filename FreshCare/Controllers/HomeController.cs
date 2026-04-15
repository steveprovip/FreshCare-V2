using FreshCare.Helpers;
using FreshCare.Models;
using FreshCare.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace FreshCare.Controllers
{
    /// <summary> alo cường à cường không chối được đâu
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

                    // Đọc cấu hình Hàng bán chậm
                    int rSoNgay = 30;
                    int rSoLuong = 10;
                    string sqlConfig = "SELECT TenThamSo, GiaTri FROM CauHinh WHERE TenThamSo IN ('BAN_CHAM_SO_NGAY', 'BAN_CHAM_SO_LUONG')";
                    using (var cmd = new SqlCommand(sqlConfig, conn))
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string ten = reader["TenThamSo"].ToString()!;
                            int val = int.TryParse(reader["GiaTri"].ToString(), out var parsed) ? parsed : 0;
                            if (ten == "BAN_CHAM_SO_NGAY" && val > 0) rSoNgay = val;
                            if (ten == "BAN_CHAM_SO_LUONG" && val >= 0) rSoLuong = val;
                        }
                    }
                    model.SoNgayBanCham = rSoNgay;
                    model.SoLuongBanChamThreshold = rSoLuong;

                    // Query Hàng bán chậm: tổng đã bán trong X ngày qua <= Y, và hiện đang có tồn kho
                    string sqlBanCham = @"
                        SELECT sp.MaSP, sp.TenSP, sp.DonViTinh, ISNULL(SUM(cx.SoLuong), 0) AS TongDaBan
                        FROM SanPham sp
                        INNER JOIN LoHang lh ON sp.MaSP = lh.MaSP AND lh.SoLuongTon > 0 AND lh.TrangThai != N'Đã Hủy'
                        LEFT JOIN ChiTietXuat cx ON lh.MaLo = cx.MaLo
                        LEFT JOIN PhieuXuat px ON cx.MaPhieuXuat = px.MaPhieuXuat AND px.LoaiPhieu = N'Bán Hàng' 
                                              AND px.NgayXuat >= DATEADD(DAY, -@SoNgay, GETDATE())
                        WHERE sp.TrangThai = N'HoatDong'
                        GROUP BY sp.MaSP, sp.TenSP, sp.DonViTinh
                        HAVING ISNULL(SUM(cx.SoLuong), 0) <= @SoLuong
                        ORDER BY TongDaBan ASC, sp.TenSP ASC";

                    using (var cmd = new SqlCommand(sqlBanCham, conn))
                    {
                        cmd.Parameters.AddWithValue("@SoNgay", model.SoNgayBanCham);
                        cmd.Parameters.AddWithValue("@SoLuong", model.SoLuongBanChamThreshold);
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                model.SanPhamBanCham.Add(new ThongKeSanPham
                                {
                                    MaSP = Convert.ToInt32(reader["MaSP"]),
                                    TenSP = reader["TenSP"].ToString()!,
                                    DonViTinh = reader["DonViTinh"].ToString()!,
                                    TongSoLuong = Convert.ToDecimal(reader["TongDaBan"])
                                });
                            }
                        }
                    }
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
        /// Luật #7: Quá hạn (HSD < Today), Cận Date (HSD < Today + 14), An Toàn
        /// </summary>
        private void CapNhatTrangThaiLoHang(SqlConnection conn)
        {
            string updateSql = @"
                UPDATE LoHang SET TrangThai = 
                    CASE 
                        WHEN HanSuDung < CAST(GETDATE() AS DATE) THEN N'Quá Hạn'
                        WHEN DATEDIFF(DAY, CAST(GETDATE() AS DATE), HanSuDung) < 14 THEN N'Cận Date'
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

        // POST: /Home/CapNhatCauHinhBanCham
        [HttpPost]
        public IActionResult CapNhatCauHinhBanCham(int soNgay, int soLuong)
        {
            if (HttpContext.Session.GetString("VaiTro") != "Admin")
            {
                TempData["Error"] = "Chỉ Admin mới có quyền đổi cấu hình!";
                return RedirectToAction("Index");
            }
            if (soNgay <= 0 || soLuong < 0)
            {
                TempData["Error"] = "Số ngày phải > 0 và Số lượng tối thiểu >= 0.";
                return RedirectToAction("Index");
            }

            try
            {
                using (var conn = DatabaseHelper.GetConnection(_connectionString))
                {
                    conn.Open();

                    void UpsertConfig(string key, string val)
                    {
                        string check = "SELECT COUNT(*) FROM CauHinh WHERE TenThamSo = @key";
                        int exist = 0;
                        using (var cCmd = new SqlCommand(check, conn))
                        {
                            cCmd.Parameters.AddWithValue("@key", key);
                            exist = Convert.ToInt32(cCmd.ExecuteScalar());
                        }

                        if (exist > 0)
                        {
                            string up = "UPDATE CauHinh SET GiaTri = @val WHERE TenThamSo = @key";
                            using (var cCmd = new SqlCommand(up, conn))
                            {
                                cCmd.Parameters.AddWithValue("@key", key);
                                cCmd.Parameters.AddWithValue("@val", val);
                                cCmd.ExecuteNonQuery();
                            }
                        }
                        else
                        {
                            string ins = "INSERT INTO CauHinh (MaConfig, TenThamSo, GiaTri) VALUES (@key, @key, @val)";
                            using (var cCmd = new SqlCommand(ins, conn))
                            {
                                cCmd.Parameters.AddWithValue("@key", key);
                                cCmd.Parameters.AddWithValue("@val", val);
                                cCmd.ExecuteNonQuery();
                            }
                        }
                    }

                    UpsertConfig("BAN_CHAM_SO_NGAY", soNgay.ToString());
                    UpsertConfig("BAN_CHAM_SO_LUONG", soLuong.ToString());

                    int maNV = HttpContext.Session.GetInt32("MaNV") ?? 0;
                    LichSuController.GhiLog(_connectionString, maNV, "Cập nhật Cấu hình", $"Sửa Thuật toán Hàng bản chậm: {soNgay} ngày, <= {soLuong} sản phẩm");

                    TempData["Success"] = "Cập nhật cấu hình thuật toán Hàng Bán Chậm thành công!";
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Lỗi cập nhật cấu hình: " + ex.Message;
            }

            return RedirectToAction("Index");
        }
    }
}
