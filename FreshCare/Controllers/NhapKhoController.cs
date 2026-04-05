using FreshCare.Helpers;
using FreshCare.Models;
using FreshCare.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace FreshCare.Controllers
{
    /// <summary>
    /// NhapKhoController - Nhập kho & Tạo lô hàng mới
    /// Luật #17: HSD >= NSX
    /// Luật #20: using + try-catch
    /// </summary>
    public class NhapKhoController : Controller
    {
        private readonly string _connectionString;

        public NhapKhoController(string connectionString)
        {
            _connectionString = connectionString;
        }

        // GET: /NhapKho/Index
        public IActionResult Index()
        {
            if (HttpContext.Session.GetInt32("MaNV") == null)
                return RedirectToAction("DangNhap", "TaiKhoan");

            var model = new NhapKhoViewModel
            {
                NgaySanXuat = DateTime.Today,
                HanSuDung = DateTime.Today.AddDays(30),
                DanhSachSanPham = LayDanhSachSanPham()
            };

            return View(model);
        }

        // POST: /NhapKho/NhapMoi
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult NhapMoi(NhapKhoViewModel model)
        {
            model.DanhSachSanPham = LayDanhSachSanPham();

            // Luật #17: Kiểm tra HSD >= NSX
            if (model.HanSuDung < model.NgaySanXuat)
            {
                TempData["Error"] = "Lỗi: Hạn sử dụng không được nhỏ hơn Ngày sản xuất!";
                return View("Index", model);
            }

            if (!ModelState.IsValid) return View("Index", model);

            int maNV = HttpContext.Session.GetInt32("MaNV") ?? 0;

            try
            {
                using (var conn = DatabaseHelper.GetConnection(_connectionString))
                {
                    conn.Open();

                    // Sử dụng Transaction để đảm bảo toàn vẹn dữ liệu
                    using (var transaction = conn.BeginTransaction())
                    {
                        try
                        {
                            // Luật #7: Tự động phân loại trạng thái
                            string trangThai = DatabaseHelper.PhanLoaiTrangThai(model.HanSuDung);

                            // 1. Tạo lô hàng mới (Luật #4: SqlParameter)
                            string sqlLoHang = @"INSERT INTO LoHang (MaSP, SoLuongNhap, SoLuongTon, NgaySanXuat, HanSuDung, NgayNhapKho, TrangThai)
                                                 OUTPUT INSERTED.MaLo
                                                 VALUES (@MaSP, @SoLuong, @SoLuong, @NgaySanXuat, @HanSuDung, GETDATE(), @TrangThai)";
                            int maLo;
                            using (var cmd = new SqlCommand(sqlLoHang, conn, transaction))
                            {
                                cmd.Parameters.AddWithValue("@MaSP", model.MaSP);
                                cmd.Parameters.AddWithValue("@SoLuong", model.SoLuong);
                                cmd.Parameters.AddWithValue("@NgaySanXuat", model.NgaySanXuat);
                                cmd.Parameters.AddWithValue("@HanSuDung", model.HanSuDung);
                                cmd.Parameters.AddWithValue("@TrangThai", trangThai);
                                maLo = Convert.ToInt32(cmd.ExecuteScalar());
                            }

                            // 2. Tạo phiếu nhập kho
                            string sqlPhieu = @"INSERT INTO PhieuNhapKho (NgayNhap, MaNV, GhiChu)
                                                OUTPUT INSERTED.MaPhieuNhap
                                                VALUES (GETDATE(), @MaNV, @GhiChu)";
                            int maPhieu;
                            using (var cmd = new SqlCommand(sqlPhieu, conn, transaction))
                            {
                                cmd.Parameters.AddWithValue("@MaNV", maNV);
                                cmd.Parameters.AddWithValue("@GhiChu", (object?)model.GhiChu ?? DBNull.Value);
                                maPhieu = Convert.ToInt32(cmd.ExecuteScalar());
                            }

                            // 3. Tạo chi tiết nhập
                            string sqlChiTiet = @"INSERT INTO ChiTietNhap (MaPhieuNhap, MaLo, SoLuong)
                                                  VALUES (@MaPhieuNhap, @MaLo, @SoLuong)";
                            using (var cmd = new SqlCommand(sqlChiTiet, conn, transaction))
                            {
                                cmd.Parameters.AddWithValue("@MaPhieuNhap", maPhieu);
                                cmd.Parameters.AddWithValue("@MaLo", maLo);
                                cmd.Parameters.AddWithValue("@SoLuong", model.SoLuong);
                                cmd.ExecuteNonQuery();
                            }

                            transaction.Commit();

                            // Luật #19: TempData alert
                            TempData["Success"] = $"Nhập kho thành công! Mã lô: LO-{maLo:D4}, Phiếu nhập: PN-{maPhieu:D4}";
                        }
                        catch
                        {
                            transaction.Rollback();
                            throw;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Lỗi nhập kho: " + ex.Message;
            }

            return RedirectToAction("Index");
        }

        // GET: /NhapKho/LichSu - Xem lịch sử phiếu nhập
        public IActionResult LichSu()
        {
            if (HttpContext.Session.GetInt32("MaNV") == null)
                return RedirectToAction("DangNhap", "TaiKhoan");

            var list = new List<PhieuNhapKho>();
            try
            {
                using (var conn = DatabaseHelper.GetConnection(_connectionString))
                {
                    conn.Open();
                    string sql = @"SELECT pn.MaPhieuNhap, pn.NgayNhap, pn.GhiChu, nv.HoTen AS TenNhanVien
                                   FROM PhieuNhapKho pn
                                   INNER JOIN NhanVien nv ON pn.MaNV = nv.MaNV
                                   ORDER BY pn.NgayNhap DESC";
                    using (var cmd = new SqlCommand(sql, conn))
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            list.Add(new PhieuNhapKho
                            {
                                MaPhieuNhap = Convert.ToInt32(reader["MaPhieuNhap"]),
                                NgayNhap = Convert.ToDateTime(reader["NgayNhap"]),
                                GhiChu = reader["GhiChu"]?.ToString(),
                                TenNhanVien = reader["TenNhanVien"].ToString()
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Lỗi: " + ex.Message;
            }

            return View(list);
        }

        // AJAX: Lấy thông tin sản phẩm theo ID (Luật #14)
        [HttpGet]
        public IActionResult LaySanPham(int maSP)
        {
            try
            {
                using (var conn = DatabaseHelper.GetConnection(_connectionString))
                {
                    conn.Open();
                    string sql = "SELECT MaSP, TenSP, DonViTinh, GiaBan FROM SanPham WHERE MaSP = @MaSP";
                    using (var cmd = new SqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@MaSP", maSP);
                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                                return Json(new
                                {
                                    success = true,
                                    donViTinh = reader["DonViTinh"].ToString(),
                                    giaBan = Convert.ToDecimal(reader["GiaBan"])
                                });
                        }
                    }
                }
            }
            catch { }
            return Json(new { success = false });
        }

        private List<SanPham> LayDanhSachSanPham()
        {
            var list = new List<SanPham>();
            try
            {
                using (var conn = DatabaseHelper.GetConnection(_connectionString))
                {
                    conn.Open();
                    string sql = @"SELECT sp.MaSP, sp.TenSP, sp.DonViTinh, sp.GiaBan, dm.TenDanhMuc
                                   FROM SanPham sp 
                                   INNER JOIN DanhMuc dm ON sp.MaDanhMuc = dm.MaDanhMuc
                                   WHERE sp.TrangThai = N'HoatDong' ORDER BY sp.TenSP";
                    using (var cmd = new SqlCommand(sql, conn))
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            list.Add(new SanPham
                            {
                                MaSP = Convert.ToInt32(reader["MaSP"]),
                                TenSP = reader["TenSP"].ToString()!,
                                DonViTinh = reader["DonViTinh"].ToString()!,
                                GiaBan = Convert.ToDecimal(reader["GiaBan"]),
                                TenDanhMuc = reader["TenDanhMuc"].ToString()
                            });
                        }
                    }
                }
            }
            catch { }
            return list;
        }
    }
}
