using FreshCare.Helpers;
using FreshCare.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace FreshCare.Controllers
{
    /// <summary>
    /// SanPhamController - Quản lý danh mục & sản phẩm
    /// Luật #5: Không dùng DELETE, cập nhật trạng thái
    /// Luật #9: Mỗi sản phẩm bắt buộc có đơn vị tính
    /// Luật #14: AJAX cập nhật không reload
    /// </summary>
    public class SanPhamController : Controller
    {
        private readonly string _connectionString;

        public SanPhamController(string connectionString)
        {
            _connectionString = connectionString;
        }

        #region Danh Mục

        // GET: /SanPham/DanhMuc
        public IActionResult DanhMuc()
        {
            if (HttpContext.Session.GetInt32("MaNV") == null)
                return RedirectToAction("DangNhap", "TaiKhoan");

            var list = new List<DanhMuc>();
            try
            {
                using (var conn = DatabaseHelper.GetConnection(_connectionString))
                {
                    conn.Open();
                    using (var cmd = new SqlCommand("SELECT MaDanhMuc, TenDanhMuc, PhanTramSale FROM DanhMuc ORDER BY MaDanhMuc", conn))
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            list.Add(new DanhMuc
                            {
                                MaDanhMuc = Convert.ToInt32(reader["MaDanhMuc"]),
                                TenDanhMuc = reader["TenDanhMuc"].ToString()!,
                                PhanTramSale = Convert.ToDecimal(reader["PhanTramSale"])
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

        // POST: /SanPham/ThemDanhMuc
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ThemDanhMuc(string tenDanhMuc, decimal phanTramSale)
        {
            try
            {
                using (var conn = DatabaseHelper.GetConnection(_connectionString))
                {
                    conn.Open();
                    string sql = "INSERT INTO DanhMuc (TenDanhMuc, PhanTramSale) VALUES (@TenDanhMuc, @PhanTramSale)";
                    using (var cmd = new SqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@TenDanhMuc", tenDanhMuc);
                        cmd.Parameters.AddWithValue("@PhanTramSale", phanTramSale);
                        cmd.ExecuteNonQuery();
                    }
                }
                TempData["Success"] = "Thêm danh mục thành công!";
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Lỗi: " + ex.Message;
            }

            return RedirectToAction("DanhMuc");
        }

        // POST: /SanPham/SuaDanhMuc
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SuaDanhMuc(int maDanhMuc, string tenDanhMuc, decimal phanTramSale)
        {
            try
            {
                using (var conn = DatabaseHelper.GetConnection(_connectionString))
                {
                    conn.Open();
                    string sql = "UPDATE DanhMuc SET TenDanhMuc = @TenDanhMuc, PhanTramSale = @PhanTramSale WHERE MaDanhMuc = @MaDanhMuc";
                    using (var cmd = new SqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@MaDanhMuc", maDanhMuc);
                        cmd.Parameters.AddWithValue("@TenDanhMuc", tenDanhMuc);
                        cmd.Parameters.AddWithValue("@PhanTramSale", phanTramSale);
                        cmd.ExecuteNonQuery();
                    }
                }
                TempData["Success"] = "Cập nhật danh mục thành công!";
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Lỗi: " + ex.Message;
            }

            return RedirectToAction("DanhMuc");
        }

        #endregion

        #region Sản Phẩm

        // GET: /SanPham/DanhSach
        public IActionResult DanhSach()
        {
            if (HttpContext.Session.GetInt32("MaNV") == null)
                return RedirectToAction("DangNhap", "TaiKhoan");

            var list = new List<SanPham>();
            try
            {
                using (var conn = DatabaseHelper.GetConnection(_connectionString))
                {
                    conn.Open();
                    string sql = @"SELECT sp.MaSP, sp.TenSP, sp.DonViTinh, sp.GiaBan, sp.MaDanhMuc,
                                          sp.MoTa, sp.MaVach, sp.TrangThai, dm.TenDanhMuc
                                   FROM SanPham sp
                                   INNER JOIN DanhMuc dm ON sp.MaDanhMuc = dm.MaDanhMuc
                                   WHERE sp.TrangThai = N'HoatDong'
                                   ORDER BY sp.MaSP";
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
                                MaDanhMuc = Convert.ToInt32(reader["MaDanhMuc"]),
                                MoTa = reader["MoTa"]?.ToString(),
                                MaVach = reader["MaVach"]?.ToString(),
                                TrangThai = reader["TrangThai"].ToString()!,
                                TenDanhMuc = reader["TenDanhMuc"].ToString()
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Lỗi: " + ex.Message;
            }

            // Load danh mục cho dropdown
            ViewBag.DanhMucs = LayDanhMuc();
            return View(list);
        }

        // POST: /SanPham/ThemSanPham
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ThemSanPham(string tenSP, string donViTinh, decimal giaBan, int maDanhMuc, string? moTa, string? maVach)
        {
            try
            {
                using (var conn = DatabaseHelper.GetConnection(_connectionString))
                {
                    conn.Open();
                    string sql = @"INSERT INTO SanPham (TenSP, DonViTinh, GiaBan, MaDanhMuc, MoTa, MaVach, TrangThai) 
                                   VALUES (@TenSP, @DonViTinh, @GiaBan, @MaDanhMuc, @MoTa, @MaVach, N'HoatDong')";
                    using (var cmd = new SqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@TenSP", tenSP);
                        cmd.Parameters.AddWithValue("@DonViTinh", donViTinh);
                        cmd.Parameters.AddWithValue("@GiaBan", giaBan);
                        cmd.Parameters.AddWithValue("@MaDanhMuc", maDanhMuc);
                        cmd.Parameters.AddWithValue("@MoTa", (object?)moTa ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@MaVach", (object?)maVach ?? DBNull.Value);
                        cmd.ExecuteNonQuery();
                    }
                }
                TempData["Success"] = "Thêm sản phẩm thành công!";
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Lỗi: " + ex.Message;
            }

            return RedirectToAction("DanhSach");
        }

        // POST: /SanPham/SuaSanPham
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SuaSanPham(int maSP, string tenSP, string donViTinh, decimal giaBan, int maDanhMuc, string? moTa, string? maVach)
        {
            try
            {
                using (var conn = DatabaseHelper.GetConnection(_connectionString))
                {
                    conn.Open();
                    string sql = @"UPDATE SanPham 
                                   SET TenSP = @TenSP, DonViTinh = @DonViTinh, GiaBan = @GiaBan, 
                                       MaDanhMuc = @MaDanhMuc, MoTa = @MoTa, MaVach = @MaVach
                                   WHERE MaSP = @MaSP";
                    using (var cmd = new SqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@MaSP", maSP);
                        cmd.Parameters.AddWithValue("@TenSP", tenSP);
                        cmd.Parameters.AddWithValue("@DonViTinh", donViTinh);
                        cmd.Parameters.AddWithValue("@GiaBan", giaBan);
                        cmd.Parameters.AddWithValue("@MaDanhMuc", maDanhMuc);
                        cmd.Parameters.AddWithValue("@MoTa", (object?)moTa ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@MaVach", (object?)maVach ?? DBNull.Value);
                        cmd.ExecuteNonQuery();
                    }
                }
                TempData["Success"] = "Cập nhật sản phẩm thành công!";
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Lỗi: " + ex.Message;
            }

            return RedirectToAction("DanhSach");
        }

        // POST: /SanPham/XoaSanPham (Luật #5: Không DELETE, cập nhật trạng thái)
        [HttpPost]
        public IActionResult XoaSanPham(int maSP)
        {
            try
            {
                using (var conn = DatabaseHelper.GetConnection(_connectionString))
                {
                    conn.Open();
                    // Luật #5: Không sử dụng DELETE, cập nhật trạng thái
                    string sql = "UPDATE SanPham SET TrangThai = N'DaXoa' WHERE MaSP = @MaSP";
                    using (var cmd = new SqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@MaSP", maSP);
                        cmd.ExecuteNonQuery();
                    }
                }
                TempData["Success"] = "Đã xoá sản phẩm (cập nhật trạng thái).";
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Lỗi: " + ex.Message;
            }

            return RedirectToAction("DanhSach");
        }

        // AJAX: Tìm sản phẩm theo mã vạch (Luật #14)
        [HttpGet]
        public IActionResult TimTheoMaVach(string maVach)
        {
            try
            {
                using (var conn = DatabaseHelper.GetConnection(_connectionString))
                {
                    conn.Open();
                    string sql = @"SELECT sp.MaSP, sp.TenSP, sp.DonViTinh, sp.GiaBan, dm.TenDanhMuc
                                   FROM SanPham sp 
                                   INNER JOIN DanhMuc dm ON sp.MaDanhMuc = dm.MaDanhMuc
                                   WHERE sp.MaVach = @MaVach AND sp.TrangThai = N'HoatDong'";
                    using (var cmd = new SqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@MaVach", maVach);
                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                return Json(new
                                {
                                    success = true,
                                    maSP = reader["MaSP"],
                                    tenSP = reader["TenSP"],
                                    donViTinh = reader["DonViTinh"],
                                    giaBan = reader["GiaBan"],
                                    tenDanhMuc = reader["TenDanhMuc"]
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }

            return Json(new { success = false, message = "Không tìm thấy sản phẩm" });
        }

        private List<DanhMuc> LayDanhMuc()
        {
            var list = new List<DanhMuc>();
            try
            {
                using (var conn = DatabaseHelper.GetConnection(_connectionString))
                {
                    conn.Open();
                    using (var cmd = new SqlCommand("SELECT MaDanhMuc, TenDanhMuc, PhanTramSale FROM DanhMuc", conn))
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            list.Add(new DanhMuc
                            {
                                MaDanhMuc = Convert.ToInt32(reader["MaDanhMuc"]),
                                TenDanhMuc = reader["TenDanhMuc"].ToString()!,
                                PhanTramSale = Convert.ToDecimal(reader["PhanTramSale"])
                            });
                        }
                    }
                }
            }
            catch { }
            return list;
        }

        #endregion
    }
}
