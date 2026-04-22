using FreshCare.Helpers;
using FreshCare.Models;
using FreshCare.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Text.Json;

namespace FreshCare.Controllers
{
    /// <summary>
    /// PheDuyetController - Quản lý yêu cầu chỉnh sửa từ nhân viên
    /// Yêu cầu #7: Nhân viên sửa phải chờ Quản lý duyệt
    /// </summary>
    public class PheDuyetController : Controller
    {
        private readonly string _connectionString;

        public PheDuyetController(string connectionString)
        {
            _connectionString = connectionString;
        }

        // GET: /PheDuyet/Index - Danh sách yêu cầu chờ duyệt (Admin only)
        public IActionResult Index()
        {
            if (HttpContext.Session.GetString("VaiTro") != "Admin")
            {
                TempData["Error"] = "Chỉ Quản lý mới có quyền duyệt yêu cầu!";
                return RedirectToAction("Index", "Home");
            }

            var list = new List<YeuCauPheDuyet>();

            try
            {
                using (var conn = DatabaseHelper.GetConnection(_connectionString))
                {
                    conn.Open();
                    string sql = @"SELECT yc.*, nv.HoTen AS TenNhanVienGhi, nd.HoTen AS TenNguoiDuyet
                                   FROM YeuCauPheDuyet yc
                                   INNER JOIN NhanVien nv ON yc.MaNV = nv.MaNV
                                   LEFT JOIN NhanVien nd ON yc.NguoiDuyet = nd.MaNV
                                   ORDER BY 
                                       CASE WHEN yc.TrangThai = N'Chờ Duyệt' THEN 0 ELSE 1 END,
                                       yc.NgayGui DESC";
                    using (var cmd = new SqlCommand(sql, conn))
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            list.Add(new YeuCauPheDuyet
                            {
                                MaYeuCau = Convert.ToInt32(reader["MaYeuCau"]),
                                MaNV = Convert.ToInt32(reader["MaNV"]),
                                PhanHe = reader["PhanHe"].ToString()!,
                                LoaiChinhSua = reader["LoaiChinhSua"].ToString()!,
                                MaBanGhi = Convert.ToInt32(reader["MaBanGhi"]),
                                DuLieuCu = reader["DuLieuCu"]?.ToString(),
                                DuLieuMoi = reader["DuLieuMoi"].ToString()!,
                                NgayGui = Convert.ToDateTime(reader["NgayGui"]),
                                TrangThai = reader["TrangThai"].ToString()!,
                                NguoiDuyet = reader["NguoiDuyet"] != DBNull.Value ? Convert.ToInt32(reader["NguoiDuyet"]) : null,
                                NgayDuyet = reader["NgayDuyet"] != DBNull.Value ? Convert.ToDateTime(reader["NgayDuyet"]) : null,
                                GhiChu = reader["GhiChu"]?.ToString(),
                                TenNhanVienGhi = reader["TenNhanVienGhi"].ToString(),
                                TenNguoiDuyet = reader["TenNguoiDuyet"]?.ToString()
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

        // POST: /PheDuyet/Duyet - Chấp nhận yêu cầu
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Duyet(int maYeuCau)
        {
            int maNVDuyet = HttpContext.Session.GetInt32("MaNV") ?? 0;

            try
            {
                using (var conn = DatabaseHelper.GetConnection(_connectionString))
                {
                    conn.Open();

                    // Lấy thông tin yêu cầu
                    YeuCauPheDuyet? yeuCau = null;
                    string sqlGet = "SELECT * FROM YeuCauPheDuyet WHERE MaYeuCau = @MaYeuCau AND TrangThai = N'Chờ Duyệt'";
                    using (var cmd = new SqlCommand(sqlGet, conn))
                    {
                        cmd.Parameters.AddWithValue("@MaYeuCau", maYeuCau);
                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                yeuCau = new YeuCauPheDuyet
                                {
                                    MaYeuCau = Convert.ToInt32(reader["MaYeuCau"]),
                                    PhanHe = reader["PhanHe"].ToString()!,
                                    LoaiChinhSua = reader["LoaiChinhSua"].ToString()!,
                                    MaBanGhi = Convert.ToInt32(reader["MaBanGhi"]),
                                    DuLieuMoi = reader["DuLieuMoi"].ToString()!,
                                    MaNV = Convert.ToInt32(reader["MaNV"])
                                };
                            }
                        }
                    }

                    if (yeuCau == null)
                    {
                        TempData["Error"] = "Yêu cầu không tồn tại hoặc đã được xử lý!";
                        return RedirectToAction("Index");
                    }

                    // Áp dụng thay đổi tùy theo loại
                    if (yeuCau.PhanHe == "SanPham" && yeuCau.LoaiChinhSua == "Sửa")
                    {
                        var data = JsonSerializer.Deserialize<Dictionary<string, string>>(yeuCau.DuLieuMoi);
                        if (data != null)
                        {
                            string sqlUpdate = @"UPDATE SanPham SET TenSP = @TenSP, DonViTinh = @DonViTinh, 
                                                 GiaNhap = @GiaNhap, GiaBan = @GiaBan, MaDanhMuc = @MaDanhMuc,
                                                 MoTa = @MoTa, MaVach = @MaVach WHERE MaSP = @MaSP";
                            using (var cmd = new SqlCommand(sqlUpdate, conn))
                            {
                                cmd.Parameters.AddWithValue("@MaSP", yeuCau.MaBanGhi);
                                cmd.Parameters.AddWithValue("@TenSP", data.GetValueOrDefault("TenSP", ""));
                                cmd.Parameters.AddWithValue("@DonViTinh", data.GetValueOrDefault("DonViTinh", ""));
                                cmd.Parameters.AddWithValue("@GiaNhap", decimal.Parse(data.GetValueOrDefault("GiaNhap", "0")));
                                cmd.Parameters.AddWithValue("@GiaBan", decimal.Parse(data.GetValueOrDefault("GiaBan", "0")));
                                cmd.Parameters.AddWithValue("@MaDanhMuc", int.Parse(data.GetValueOrDefault("MaDanhMuc", "0")));
                                cmd.Parameters.AddWithValue("@MoTa", (object?)data.GetValueOrDefault("MoTa") ?? DBNull.Value);
                                cmd.Parameters.AddWithValue("@MaVach", (object?)data.GetValueOrDefault("MaVach") ?? DBNull.Value);
                                cmd.ExecuteNonQuery();
                            }
                        }
                    }
                    else if (yeuCau.PhanHe == "SanPham" && yeuCau.LoaiChinhSua == "Xóa")
                    {
                        string sqlDel = "UPDATE SanPham SET TrangThai = N'DaXoa' WHERE MaSP = @MaSP";
                        using (var cmd = new SqlCommand(sqlDel, conn))
                        {
                            cmd.Parameters.AddWithValue("@MaSP", yeuCau.MaBanGhi);
                            cmd.ExecuteNonQuery();
                        }
                    }

                    // Cập nhật trạng thái yêu cầu
                    string sqlApprove = @"UPDATE YeuCauPheDuyet SET TrangThai = N'Đã Duyệt', NguoiDuyet = @NguoiDuyet, 
                                          NgayDuyet = GETDATE() WHERE MaYeuCau = @MaYeuCau";
                    using (var cmd = new SqlCommand(sqlApprove, conn))
                    {
                        cmd.Parameters.AddWithValue("@NguoiDuyet", maNVDuyet);
                        cmd.Parameters.AddWithValue("@MaYeuCau", maYeuCau);
                        cmd.ExecuteNonQuery();
                    }

                    // Ghi log
                    LichSuController.GhiLog(_connectionString, maNVDuyet, "Duyệt yêu cầu",
                        $"Duyệt yêu cầu #{maYeuCau} ({yeuCau.PhanHe} - {yeuCau.LoaiChinhSua})");

                    TempData["Success"] = $"Đã duyệt yêu cầu #{maYeuCau} thành công!";
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Lỗi duyệt: " + ex.Message;
            }

            return RedirectToAction("Index");
        }

        // POST: /PheDuyet/TuChoi - Từ chối yêu cầu
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult TuChoi(int maYeuCau, string? ghiChu)
        {
            int maNVDuyet = HttpContext.Session.GetInt32("MaNV") ?? 0;

            try
            {
                using (var conn = DatabaseHelper.GetConnection(_connectionString))
                {
                    conn.Open();
                    string sql = @"UPDATE YeuCauPheDuyet SET TrangThai = N'Từ Chối', NguoiDuyet = @NguoiDuyet, 
                                   NgayDuyet = GETDATE(), GhiChu = @GhiChu WHERE MaYeuCau = @MaYeuCau";
                    using (var cmd = new SqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@NguoiDuyet", maNVDuyet);
                        cmd.Parameters.AddWithValue("@MaYeuCau", maYeuCau);
                        cmd.Parameters.AddWithValue("@GhiChu", (object?)ghiChu ?? DBNull.Value);
                        cmd.ExecuteNonQuery();
                    }

                    LichSuController.GhiLog(_connectionString, maNVDuyet, "Từ chối yêu cầu",
                        $"Từ chối yêu cầu #{maYeuCau}. Lý do: {ghiChu}");

                    TempData["Success"] = $"Đã từ chối yêu cầu #{maYeuCau}.";
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Lỗi: " + ex.Message;
            }

            return RedirectToAction("Index");
        }
    }
}
