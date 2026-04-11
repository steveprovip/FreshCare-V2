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

                    // Tạo tài khoản mới (mặc định vai trò Nhân viên) - bao gồm Email và SĐT
                    string insertSql = @"INSERT INTO NhanVien (HoTen, TenDangNhap, MatKhau, VaiTro, TrangThai, Email, SoDienThoai) 
                                         VALUES (@HoTen, @TenDangNhap, @MatKhau, N'NhanVien', N'HoatDong', @Email, @SoDienThoai)";
                    using (var cmd = new SqlCommand(insertSql, conn))
                    {
                        cmd.Parameters.AddWithValue("@HoTen", model.HoTen);
                        cmd.Parameters.AddWithValue("@TenDangNhap", model.TenDangNhap);
                        cmd.Parameters.AddWithValue("@MatKhau", DatabaseHelper.HashPassword(model.MatKhau));
                        cmd.Parameters.AddWithValue("@Email", (object?)model.Email ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@SoDienThoai", (object?)model.SoDienThoai ?? DBNull.Value);
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

        // ============================================================
        // CHỨC NĂNG QUÊN MẬT KHẨU
        // ============================================================

        // GET: /TaiKhoan/QuenMatKhau - Bước 1: nhập tên đăng nhập + email/SĐT
        public IActionResult QuenMatKhau()
        {
            return View(new QuenMatKhauViewModel());
        }

        // POST: /TaiKhoan/QuenMatKhau - Xác minh thông tin
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult QuenMatKhau(QuenMatKhauViewModel model)
        {
            // Bắt buộc nhập ít nhất Email hoặc SĐT
            if (string.IsNullOrWhiteSpace(model.Email) && string.IsNullOrWhiteSpace(model.SoDienThoai))
            {
                ModelState.AddModelError("", "Vui lòng nhập Email hoặc Số điện thoại để xác minh.");
            }

            if (!ModelState.IsValid) return View(model);

            try
            {
                using (var conn = DatabaseHelper.GetConnection(_connectionString))
                {
                    conn.Open();

                    // Tìm tài khoản khớp tên đăng nhập + (Email HOẶC SĐT)
                    string sql = @"SELECT MaNV, HoTen, TenDangNhap, TrangThai 
                                   FROM NhanVien 
                                   WHERE TenDangNhap = @TenDangNhap
                                     AND (
                                           (Email IS NOT NULL AND Email = @Email)
                                        OR (SoDienThoai IS NOT NULL AND SoDienThoai = @SoDienThoai)
                                     )";
                    using (var cmd = new SqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@TenDangNhap", model.TenDangNhap);
                        cmd.Parameters.AddWithValue("@Email", (object?)model.Email ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@SoDienThoai", (object?)model.SoDienThoai ?? DBNull.Value);

                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                string trangThai = reader["TrangThai"].ToString()!;
                                if (trangThai == "DaKhoa")
                                {
                                    ModelState.AddModelError("", "Tài khoản đã bị khóa. Vui lòng liên hệ quản lý.");
                                    return View(model);
                                }

                                int maNV = Convert.ToInt32(reader["MaNV"]);
                                string tenDangNhap = reader["TenDangNhap"].ToString()!;

                                // Chuyển sang bước 2: đặt lại mật khẩu
                                return RedirectToAction("DatLaiMatKhau", new { maNV = maNV, tenDN = tenDangNhap });
                            }
                        }
                    }
                }

                ModelState.AddModelError("", "Thông tin không khớp. Vui lòng kiểm tra lại tên đăng nhập và Email/Số điện thoại.");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Lỗi hệ thống: " + ex.Message);
            }

            return View(model);
        }

        // GET: /TaiKhoan/DatLaiMatKhau - Bước 2: đặt mật khẩu mới
        public IActionResult DatLaiMatKhau(int maNV, string tenDN)
        {
            if (maNV <= 0) return RedirectToAction("QuenMatKhau");

            return View(new DatLaiMatKhauViewModel
            {
                MaNV = maNV,
                TenDangNhap = tenDN
            });
        }

        // POST: /TaiKhoan/DatLaiMatKhau - Lưu mật khẩu mới
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DatLaiMatKhau(DatLaiMatKhauViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            try
            {
                using (var conn = DatabaseHelper.GetConnection(_connectionString))
                {
                    conn.Open();

                    string sql = "UPDATE NhanVien SET MatKhau = @MatKhau WHERE MaNV = @MaNV";
                    using (var cmd = new SqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@MatKhau", DatabaseHelper.HashPassword(model.MatKhauMoi));
                        cmd.Parameters.AddWithValue("@MaNV", model.MaNV);
                        cmd.ExecuteNonQuery();
                    }
                }

                TempData["Success"] = "Đặt lại mật khẩu thành công! Vui lòng đăng nhập.";
                return RedirectToAction("DangNhap");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Lỗi hệ thống: " + ex.Message);
            }

            return View(model);
        }

        // them nhan vien moi
        [HttpPost]
        public IActionResult AddStaff(string HoTen, string Username, string Password, string ConfirmPassword)
        {
            if (string.IsNullOrEmpty(HoTen) || string.IsNullOrEmpty(Username) || string.IsNullOrEmpty(Password))
            {
                TempData["Error"] = "Vui lòng nhập đầy đủ thông tin";
                return RedirectToAction("QuanLy");
            }

            if (Password != ConfirmPassword)
            {
                TempData["Error"] = "Mật khẩu không khớp";
                return RedirectToAction("QuanLy");
            }

            try
            {
                using (var conn = DatabaseHelper.GetConnection(_connectionString))
                {
                    conn.Open();

                    // check trùng
                    string checkSql = "SELECT COUNT(*) FROM NhanVien WHERE TenDangNhap = @Username";
                    using (var checkCmd = new SqlCommand(checkSql, conn))
                    {
                        checkCmd.Parameters.AddWithValue("@Username", Username);
                        int count = Convert.ToInt32(checkCmd.ExecuteScalar());

                        if (count > 0)
                        {
                            TempData["Error"] = "Tài khoản đã tồn tại";
                            return RedirectToAction("QuanLy");
                        }
                    }

                    // insert
                    string insertSql = @"INSERT INTO NhanVien (HoTen, TenDangNhap, MatKhau, VaiTro, TrangThai)
                                 VALUES (@HoTen, @Username, @Password, N'NhanVien', N'HoatDong')";

                    using (var cmd = new SqlCommand(insertSql, conn))
                    {
                        cmd.Parameters.AddWithValue("@HoTen", HoTen);

                        cmd.Parameters.AddWithValue("@Username", Username);
                        cmd.Parameters.AddWithValue("@Password", DatabaseHelper.HashPassword(Password));

                        cmd.ExecuteNonQuery();
                    }
                }

                TempData["Success"] = "Thêm nhân viên thành công";
                return RedirectToAction("QuanLy");
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Lỗi: " + ex.Message;
                return RedirectToAction("QuanLy");
            }
        }
        [HttpPost]
        public IActionResult DeleteStaff(int id)
        {
            try
            {
                using (var conn = DatabaseHelper.GetConnection(_connectionString))
                {
                    conn.Open();
                    // Luật #5: Không dùng DELETE, cập nhật trạng thái
                    string sql = "UPDATE NhanVien SET TrangThai = N'DaKhoa' WHERE MaNV = @Id";

                    using (var cmd = new SqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@Id", id);
                        cmd.ExecuteNonQuery();
                    }
                }

                TempData["Success"] = "Đã khóa tài khoản nhân viên thành công";
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Lỗi: " + ex.Message;
            }

            // ✅ quay về đúng trang danh sách
            return RedirectToAction("QuanLy");
        }

        // mở khóa nhân viên
        [HttpPost]
        public IActionResult UnlockStaff(int id)
        {
            try
            {
                using (var conn = DatabaseHelper.GetConnection(_connectionString))
                {
                    conn.Open();
                    string sql = "UPDATE NhanVien SET TrangThai = N'HoatDong' WHERE MaNV = @Id";

                    using (var cmd = new SqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@Id", id);
                        cmd.ExecuteNonQuery();
                    }
                }

                TempData["Success"] = "Đã mở khóa tài khoản nhân viên thành công";
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Lỗi: " + ex.Message;
            }

            return RedirectToAction("QuanLy");
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
        // thêm nhân viên mới

        
    }
}
