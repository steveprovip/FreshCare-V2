using FreshCare.Helpers;
using FreshCare.Models;
using FreshCare.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace FreshCare.Controllers
{
    /// <summary>
    /// Controller Tài khoản - Đăng nhập & Đăng ký
    /// Luật #16: Xây dựng chức năng đăng nhập và đăng ký cho nhân viên kho
    /// Luật #20: Controller sử dụng using và try-catch
    /// </summary>
    public class TaiKhoanController : Controller
    {
        private readonly string _connectionString;

        public TaiKhoanController(string connectionString)
        {
            _connectionString = connectionString;
        }

        // GET: /TaiKhoan/DangNhap
        public IActionResult DangNhap()
        {
            // Nếu đã đăng nhập, chuyển đến trang chủ
            if (HttpContext.Session.GetInt32("MaNV") != null)
                return RedirectToAction("Index", "Home");
            return View();
        }

        // POST: /TaiKhoan/DangNhap
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DangNhap(DangNhapViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            try
            {
                // Luật #3: ADO.NET + Luật #4: SqlParameter
                using (var conn = DatabaseHelper.GetConnection(_connectionString))
                {
                    conn.Open();
                    string sql = @"SELECT MaNV, HoTen, TenDangNhap, VaiTro, TrangThai 
                                   FROM NhanVien 
                                   WHERE TenDangNhap = @TenDangNhap AND MatKhau = @MatKhau";

                    using (var cmd = new SqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@TenDangNhap", model.TenDangNhap);
                        cmd.Parameters.AddWithValue("@MatKhau", DatabaseHelper.HashPassword(model.MatKhau));

                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                string trangThai = reader["TrangThai"].ToString()!;
                                if (trangThai == "DaKhoa")
                                {
                                    ModelState.AddModelError("", "Tài khoản đã bị khóa. Liên hệ Quản lý.");
                                    return View(model);
                                }

                                // Lưu thông tin vào Session
                                HttpContext.Session.SetInt32("MaNV", Convert.ToInt32(reader["MaNV"]));
                                HttpContext.Session.SetString("HoTen", reader["HoTen"].ToString()!);
                                HttpContext.Session.SetString("VaiTro", reader["VaiTro"].ToString()!);
                                HttpContext.Session.SetString("TenDangNhap", reader["TenDangNhap"].ToString()!);

                                return RedirectToAction("Index", "Home");
                            }
                        }
                    }
                }

                ModelState.AddModelError("", "Tên đăng nhập hoặc mật khẩu không đúng.");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Lỗi hệ thống: " + ex.Message);
            }

            return View(model);
        }

        // GET: /TaiKhoan/DangKy
        public IActionResult DangKy()
        {
            return View();
        }

        // POST: /TaiKhoan/DangKy
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DangKy(DangKyViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            try
            {
                using (var conn = DatabaseHelper.GetConnection(_connectionString))
                {
                    conn.Open();

                    // Kiểm tra tên đăng nhập đã tồn tại chưa
                    string checkSql = "SELECT COUNT(*) FROM NhanVien WHERE TenDangNhap = @TenDangNhap";
                    using (var checkCmd = new SqlCommand(checkSql, conn))
                    {
                        checkCmd.Parameters.AddWithValue("@TenDangNhap", model.TenDangNhap);
                        int count = Convert.ToInt32(checkCmd.ExecuteScalar());
                        if (count > 0)
                        {
                            ModelState.AddModelError("TenDangNhap", "Tên đăng nhập đã tồn tại.");
                            return View(model);
                        }
                    }

                    // Tạo tài khoản mới (mặc định vai trò Nhân viên)
                    string insertSql = @"INSERT INTO NhanVien (HoTen, TenDangNhap, MatKhau, VaiTro, TrangThai) 
                                         VALUES (@HoTen, @TenDangNhap, @MatKhau, N'NhanVien', N'HoatDong')";
                    using (var cmd = new SqlCommand(insertSql, conn))
                    {
                        cmd.Parameters.AddWithValue("@HoTen", model.HoTen);
                        cmd.Parameters.AddWithValue("@TenDangNhap", model.TenDangNhap);
                        cmd.Parameters.AddWithValue("@MatKhau", DatabaseHelper.HashPassword(model.MatKhau));
                        cmd.ExecuteNonQuery();
                    }
                }

                // Luật #19: TempData alert
                TempData["Success"] = "Đăng ký thành công! Vui lòng đăng nhập.";
                return RedirectToAction("DangNhap");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Lỗi hệ thống: " + ex.Message);
            }

            return View(model);
        }

        // POST: /TaiKhoan/DangXuat
        public IActionResult DangXuat()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("DangNhap");
        }

        // GET: /TaiKhoan/QuanLy (Admin only)
        public IActionResult QuanLy()
        {
            if (HttpContext.Session.GetString("VaiTro") != "Admin")
                return RedirectToAction("Index", "Home");

            var list = new List<NhanVien>();

            try
            {
                using (var conn = DatabaseHelper.GetConnection(_connectionString))
                {
                    conn.Open();
                    string sql = "SELECT MaNV, HoTen, TenDangNhap, VaiTro, TrangThai FROM NhanVien ORDER BY MaNV";
                    using (var cmd = new SqlCommand(sql, conn))
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            list.Add(new NhanVien
                            {
                                MaNV = Convert.ToInt32(reader["MaNV"]),
                                HoTen = reader["HoTen"].ToString()!,
                                TenDangNhap = reader["TenDangNhap"].ToString()!,
                                VaiTro = reader["VaiTro"].ToString()!,
                                TrangThai = reader["TrangThai"].ToString()!
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
    }
}
