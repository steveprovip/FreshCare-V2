using FreshCare.Helpers;
using FreshCare.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Text.Json;

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
            if (HttpContext.Session.GetInt32("MaNV") == null)
                return RedirectToAction("DangNhap", "TaiKhoan");
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
            if (HttpContext.Session.GetInt32("MaNV") == null)
                return RedirectToAction("DangNhap", "TaiKhoan");
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
        public IActionResult DanhSach(string? timKiem, string sapXep = "az", int trang = 1)
        {
            if (HttpContext.Session.GetInt32("MaNV") == null)
                return RedirectToAction("DangNhap", "TaiKhoan");

            int soLuongMoiTrang = 10;
            var model = new FreshCare.Models.ViewModels.DanhSachSanPhamViewModel
            {
                TimKiem = timKiem,
                SapXep = sapXep,
                Trang = trang,
                SoLuongMoiTrang = soLuongMoiTrang
            };

            try
            {
                using (var conn = DatabaseHelper.GetConnection(_connectionString))
                {
                    conn.Open();

                    // === Kiểm tra cột GiaNhap có tồn tại không (tương thích DB cũ) ===
                    bool hasGiaNhap = true;
                    try
                    {
                        using (var checkCmd = new SqlCommand(
                            "SELECT TOP 0 GiaNhap FROM SanPham", conn))
                        {
                            checkCmd.ExecuteNonQuery();
                        }
                    }
                    catch
                    {
                        hasGiaNhap = false;
                        // Tự động thêm cột GiaNhap nếu thiếu
                        try
                        {
                            using (var alterCmd = new SqlCommand(
                                "ALTER TABLE SanPham ADD GiaNhap DECIMAL(18,2) NOT NULL DEFAULT 0", conn))
                            {
                                alterCmd.ExecuteNonQuery();
                                hasGiaNhap = true;
                            }
                        }
                        catch { /* Đã tồn tại hoặc không có quyền */ }
                    }

                    string giaNhapCol = hasGiaNhap ? "sp.GiaNhap," : "0 AS GiaNhap,";

                    // === Đếm tổng sản phẩm (có lọc tìm kiếm) ===
                    string sqlCount = @"SELECT COUNT(*) FROM SanPham sp
                                        INNER JOIN DanhMuc dm ON sp.MaDanhMuc = dm.MaDanhMuc
                                        WHERE sp.TrangThai = N'HoatDong'";
                    if (!string.IsNullOrWhiteSpace(timKiem))
                        sqlCount += " AND sp.TenSP LIKE @TimKiem";

                    using (var cmd = new SqlCommand(sqlCount, conn))
                    {
                        if (!string.IsNullOrWhiteSpace(timKiem))
                            cmd.Parameters.AddWithValue("@TimKiem", "%" + timKiem + "%");
                        model.TongSanPham = Convert.ToInt32(cmd.ExecuteScalar());
                    }

                    model.TongTrang = (int)Math.Ceiling((double)model.TongSanPham / soLuongMoiTrang);
                    if (model.Trang < 1) model.Trang = 1;
                    if (model.Trang > model.TongTrang && model.TongTrang > 0) model.Trang = model.TongTrang;
                    int offset = (model.Trang - 1) * soLuongMoiTrang;

                    // === Sắp xếp ===
                    string orderClause = sapXep == "za" ? "sp.TenSP DESC" : "sp.TenSP ASC";

                    // === Truy vấn chính: Sản phẩm + thông tin lô gần nhất ===
                    string sql = $@"SELECT sp.MaSP, sp.TenSP, sp.DonViTinh, {giaNhapCol} sp.GiaBan, sp.MaDanhMuc,
                                          sp.MoTa, sp.MaVach, sp.TrangThai, dm.TenDanhMuc,
                                          loInfo.NgaySanXuatGanNhat, loInfo.HanSuDungGanNhat, 
                                          ISNULL(loInfo.TongTonKho, 0) AS TongTonKho,
                                          loInfo.NgayNhapKhoGanNhat
                                   FROM SanPham sp
                                   INNER JOIN DanhMuc dm ON sp.MaDanhMuc = dm.MaDanhMuc
                                   OUTER APPLY (
                                       SELECT MIN(lh.NgaySanXuat) AS NgaySanXuatGanNhat,
                                              MIN(lh.HanSuDung) AS HanSuDungGanNhat,
                                              SUM(lh.SoLuongTon) AS TongTonKho,
                                              MAX(lh.NgayNhapKho) AS NgayNhapKhoGanNhat
                                       FROM LoHang lh 
                                       WHERE lh.MaSP = sp.MaSP AND lh.SoLuongTon > 0 AND lh.TrangThai != N'Đã Hủy'
                                   ) loInfo
                                   WHERE sp.TrangThai = N'HoatDong'";

                    if (!string.IsNullOrWhiteSpace(timKiem))
                        sql += " AND sp.TenSP LIKE @TimKiem";

                    sql += $" ORDER BY {orderClause} OFFSET @Offset ROWS FETCH NEXT @Fetch ROWS ONLY";

                    using (var cmd = new SqlCommand(sql, conn))
                    {
                        if (!string.IsNullOrWhiteSpace(timKiem))
                            cmd.Parameters.AddWithValue("@TimKiem", "%" + timKiem + "%");
                        cmd.Parameters.AddWithValue("@Offset", offset);
                        cmd.Parameters.AddWithValue("@Fetch", soLuongMoiTrang);

                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                model.DanhSach.Add(new FreshCare.Models.ViewModels.SanPhamChiTiet
                                {
                                    MaSP = Convert.ToInt32(reader["MaSP"]),
                                    TenSP = reader["TenSP"].ToString()!,
                                    DonViTinh = reader["DonViTinh"].ToString()!,
                                    GiaNhap = Convert.ToDecimal(reader["GiaNhap"]),
                                    GiaBan = Convert.ToDecimal(reader["GiaBan"]),
                                    MaDanhMuc = Convert.ToInt32(reader["MaDanhMuc"]),
                                    MoTa = reader["MoTa"]?.ToString(),
                                    MaVach = reader["MaVach"]?.ToString(),
                                    TrangThai = reader["TrangThai"].ToString()!,
                                    TenDanhMuc = reader["TenDanhMuc"].ToString(),
                                    NgaySanXuatGanNhat = reader["NgaySanXuatGanNhat"] != DBNull.Value ? Convert.ToDateTime(reader["NgaySanXuatGanNhat"]) : null,
                                    HanSuDungGanNhat = reader["HanSuDungGanNhat"] != DBNull.Value ? Convert.ToDateTime(reader["HanSuDungGanNhat"]) : null,
                                    TongTonKho = Convert.ToDecimal(reader["TongTonKho"]),
                                    NgayNhapKhoGanNhat = reader["NgayNhapKhoGanNhat"] != DBNull.Value ? Convert.ToDateTime(reader["NgayNhapKhoGanNhat"]) : null
                                });
                            }
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
            return View(model);
        }

        // POST: /SanPham/ThemSanPham
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ThemSanPham(string tenSP, string donViTinh, decimal giaNhap, decimal giaBan, int maDanhMuc, string? moTa, string? maVach)
        {
            if (giaBan <= giaNhap)
            {
                TempData["Error"] = "Lỗi: Giá bán phải lớn hơn giá nhập!";
                return RedirectToAction("DanhSach");
            }
            try
            {
                using (var conn = DatabaseHelper.GetConnection(_connectionString))
                {
                    conn.Open();
                    string sql = @"INSERT INTO SanPham (TenSP, DonViTinh, GiaNhap, GiaBan, MaDanhMuc, MoTa, MaVach, TrangThai) 
                                   VALUES (@TenSP, @DonViTinh, @GiaNhap, @GiaBan, @MaDanhMuc, @MoTa, @MaVach, N'HoatDong')";
                    using (var cmd = new SqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@TenSP", tenSP);
                        cmd.Parameters.AddWithValue("@DonViTinh", donViTinh);
                        cmd.Parameters.AddWithValue("@GiaNhap", giaNhap);
                        cmd.Parameters.AddWithValue("@GiaBan", giaBan);
                        cmd.Parameters.AddWithValue("@MaDanhMuc", maDanhMuc);
                        cmd.Parameters.AddWithValue("@MoTa", (object?)moTa ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@MaVach", (object?)maVach ?? DBNull.Value);
                        cmd.ExecuteNonQuery();
                    }
                }
                TempData["Success"] = "Thêm sản phẩm thành công!";
                int maNV = HttpContext.Session.GetInt32("MaNV") ?? 0;
                LichSuController.GhiLog(_connectionString, maNV, "Thêm sản phẩm", $"Thêm SP: {tenSP}, ĐVT: {donViTinh}, Giá nhập: {giaNhap:N0}, Giá bán: {giaBan:N0}");
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Lỗi: " + ex.Message;
            }

            return RedirectToAction("DanhSach");
        }

        // POST: /SanPham/SuaSanPham
        // Yêu cầu #7: NhanVien sửa -> lưu vào bảng YeuCauPheDuyet, Admin sửa trực tiếp
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SuaSanPham(int maSP, string tenSP, string donViTinh, decimal giaNhap, decimal giaBan, int maDanhMuc, string? moTa, string? maVach)
        {
            if (giaBan <= giaNhap)
            {
                TempData["Error"] = "Lỗi: Giá bán phải lớn hơn giá nhập!";
                return RedirectToAction("DanhSach");
            }

            int maNV = HttpContext.Session.GetInt32("MaNV") ?? 0;
            string vaiTro = HttpContext.Session.GetString("VaiTro") ?? "";

            try
            {
                if (vaiTro == "NhanVien")
                {
                    // === Nhân viên: Lưu yêu cầu chờ duyệt ===
                    var duLieuMoi = JsonSerializer.Serialize(new Dictionary<string, string>
                    {
                        { "TenSP", tenSP }, { "DonViTinh", donViTinh },
                        { "GiaNhap", giaNhap.ToString() }, { "GiaBan", giaBan.ToString() },
                        { "MaDanhMuc", maDanhMuc.ToString() },
                        { "MoTa", moTa ?? "" }, { "MaVach", maVach ?? "" }
                    });

                    using (var conn = DatabaseHelper.GetConnection(_connectionString))
                    {
                        conn.Open();
                        string sql = @"INSERT INTO YeuCauPheDuyet (MaNV, PhanHe, LoaiChinhSua, MaBanGhi, DuLieuCu, DuLieuMoi, TrangThai)
                                       VALUES (@MaNV, N'SanPham', N'Sửa', @MaBanGhi, NULL, @DuLieuMoi, N'Chờ Duyệt')";
                        using (var cmd = new SqlCommand(sql, conn))
                        {
                            cmd.Parameters.AddWithValue("@MaNV", maNV);
                            cmd.Parameters.AddWithValue("@MaBanGhi", maSP);
                            cmd.Parameters.AddWithValue("@DuLieuMoi", duLieuMoi);
                            cmd.ExecuteNonQuery();
                        }
                    }
                    LichSuController.GhiLog(_connectionString, maNV, "Gửi yêu cầu sửa SP", $"SP #{maSP}: {tenSP}");
                    TempData["Success"] = "Yêu cầu chỉnh sửa đã được gửi, chờ Quản lý phê duyệt!";
                }
                else
                {
                    // === Admin: Sửa trực tiếp ===
                    using (var conn = DatabaseHelper.GetConnection(_connectionString))
                    {
                        conn.Open();
                        string sql = @"UPDATE SanPham 
                                       SET TenSP = @TenSP, DonViTinh = @DonViTinh, GiaNhap = @GiaNhap, GiaBan = @GiaBan, 
                                           MaDanhMuc = @MaDanhMuc, MoTa = @MoTa, MaVach = @MaVach
                                       WHERE MaSP = @MaSP";
                        using (var cmd = new SqlCommand(sql, conn))
                        {
                            cmd.Parameters.AddWithValue("@MaSP", maSP);
                            cmd.Parameters.AddWithValue("@TenSP", tenSP);
                            cmd.Parameters.AddWithValue("@DonViTinh", donViTinh);
                            cmd.Parameters.AddWithValue("@GiaNhap", giaNhap);
                            cmd.Parameters.AddWithValue("@GiaBan", giaBan);
                            cmd.Parameters.AddWithValue("@MaDanhMuc", maDanhMuc);
                            cmd.Parameters.AddWithValue("@MoTa", (object?)moTa ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@MaVach", (object?)maVach ?? DBNull.Value);
                            cmd.ExecuteNonQuery();
                        }
                    }
                    LichSuController.GhiLog(_connectionString, maNV, "Sửa sản phẩm", $"Cập nhật SP #{maSP}: {tenSP}");
                    TempData["Success"] = "Cập nhật sản phẩm thành công!";
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Lỗi: " + ex.Message;
            }

            return RedirectToAction("DanhSach");
        }

        // POST: /SanPham/XoaSanPham (Luật #5: Không DELETE, cập nhật trạng thái)
        // Yêu cầu #7: NhanVien xóa -> chờ duyệt
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult XoaSanPham(int maSP)
        {
            if (HttpContext.Session.GetInt32("MaNV") == null)
                return RedirectToAction("DangNhap", "TaiKhoan");

            int maNV = HttpContext.Session.GetInt32("MaNV") ?? 0;
            string vaiTro = HttpContext.Session.GetString("VaiTro") ?? "";

            try
            {
                if (vaiTro == "NhanVien")
                {
                    // Nhân viên: Gửi yêu cầu chờ duyệt (giống SuaSanPham)
                    using (var conn = DatabaseHelper.GetConnection(_connectionString))
                    {
                        conn.Open();
                        string sql = @"INSERT INTO YeuCauPheDuyet (MaNV, PhanHe, LoaiChinhSua, MaBanGhi, DuLieuCu, DuLieuMoi, TrangThai)
                                       VALUES (@MaNV, N'SanPham', N'Xóa', @MaBanGhi, NULL, N'Xóa sản phẩm', N'Chờ Duyệt')";
                        using (var cmd = new SqlCommand(sql, conn))
                        {
                            cmd.Parameters.AddWithValue("@MaNV", maNV);
                            cmd.Parameters.AddWithValue("@MaBanGhi", maSP);
                            cmd.ExecuteNonQuery();
                        }
                    }
                    LichSuController.GhiLog(_connectionString, maNV, "Gửi yêu cầu xóa SP", $"SP #{maSP}");
                    TempData["Success"] = "Yêu cầu xóa sản phẩm đã được gửi, chờ Quản lý phê duyệt!";
                }
                else
                {
                    // Admin: Xóa trực tiếp
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
                    LichSuController.GhiLog(_connectionString, maNV, "Xóa sản phẩm", $"SP #{maSP}");
                    TempData["Success"] = "Đã xoá sản phẩm (cập nhật trạng thái).";
                }
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
