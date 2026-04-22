using FreshCare.Helpers;
using FreshCare.Models;
using FreshCare.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace FreshCare.Controllers
{
    /// <summary>
    /// LichSuController - Xem lịch sử mọi thao tác trên hệ thống
    /// Yêu cầu #6: Mọi thao tác chỉnh sửa phải xem được
    /// </summary>
    public class LichSuController : Controller
    {
        private readonly string _connectionString;

        public LichSuController(string connectionString)
        {
            _connectionString = connectionString;
        }

        // GET: /LichSu/Index
        public IActionResult Index(string? timKiem, int trang = 1)
        {
            if (HttpContext.Session.GetString("VaiTro") != "Admin")
            {
                TempData["Error"] = "Chỉ Quản lý mới có quyền xem lịch sử hệ thống!";
                return RedirectToAction("Index", "Home");
            }

            int soLuongMoiTrang = 20;
            var model = new LichSuHeThongViewModel
            {
                TimKiem = timKiem,
                Trang = trang
            };

            try
            {
                using (var conn = DatabaseHelper.GetConnection(_connectionString))
                {
                    conn.Open();

                    // Đếm tổng
                    string sqlCount = @"SELECT COUNT(*) FROM LichSuHeThong ls
                                        INNER JOIN NhanVien nv ON ls.MaNV = nv.MaNV
                                        WHERE 1=1";
                    if (!string.IsNullOrWhiteSpace(timKiem))
                        sqlCount += " AND (ls.ThaoTac LIKE @TimKiem OR ls.ChiTiet LIKE @TimKiem OR nv.HoTen LIKE @TimKiem)";

                    using (var cmd = new SqlCommand(sqlCount, conn))
                    {
                        if (!string.IsNullOrWhiteSpace(timKiem))
                            cmd.Parameters.AddWithValue("@TimKiem", "%" + timKiem + "%");
                        int total = Convert.ToInt32(cmd.ExecuteScalar());
                        model.TongTrang = (int)Math.Ceiling((double)total / soLuongMoiTrang);
                    }

                    if (model.Trang < 1) model.Trang = 1;
                    if (model.Trang > model.TongTrang && model.TongTrang > 0) model.Trang = model.TongTrang;
                    int offset = (model.Trang - 1) * soLuongMoiTrang;

                    // Lấy danh sách log
                    string sql = @"SELECT ls.MaLog, ls.MaNV, ls.ThaoTac, ls.ChiTiet, ls.NgayTao, nv.HoTen AS TenNhanVien
                                   FROM LichSuHeThong ls
                                   INNER JOIN NhanVien nv ON ls.MaNV = nv.MaNV
                                   WHERE 1=1";
                    if (!string.IsNullOrWhiteSpace(timKiem))
                        sql += " AND (ls.ThaoTac LIKE @TimKiem OR ls.ChiTiet LIKE @TimKiem OR nv.HoTen LIKE @TimKiem)";

                    sql += " ORDER BY ls.NgayTao DESC OFFSET @Offset ROWS FETCH NEXT @Fetch ROWS ONLY";

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
                                model.DanhSach.Add(new LichSuHeThong
                                {
                                    MaLog = Convert.ToInt32(reader["MaLog"]),
                                    MaNV = Convert.ToInt32(reader["MaNV"]),
                                    ThaoTac = reader["ThaoTac"].ToString()!,
                                    ChiTiet = reader["ChiTiet"]?.ToString(),
                                    NgayTao = Convert.ToDateTime(reader["NgayTao"]),
                                    TenNhanVien = reader["TenNhanVien"].ToString()
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

            return View(model);
        }

        /// <summary>
        /// Ghi log thao tác vào bảng LichSuHeThong (static helper)
        /// </summary>
        public static void GhiLog(string connectionString, int maNV, string thaoTac, string? chiTiet)
        {
            try
            {
                using (var conn = DatabaseHelper.GetConnection(connectionString))
                {
                    conn.Open();

                    // Đảm bảo bảng LichSuHeThong tồn tại
                    string ensureSql = @"IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'LichSuHeThong')
                                         BEGIN
                                             CREATE TABLE LichSuHeThong (
                                                 MaLog INT IDENTITY(1,1) PRIMARY KEY,
                                                 MaNV INT NOT NULL,
                                                 ThaoTac NVARCHAR(50) NOT NULL,
                                                 ChiTiet NVARCHAR(MAX) NULL,
                                                 NgayTao DATETIME NOT NULL DEFAULT GETDATE(),
                                                 CONSTRAINT FK_LichSu_NhanVien FOREIGN KEY (MaNV) REFERENCES NhanVien(MaNV)
                                             );
                                         END";
                    using (var ensureCmd = new SqlCommand(ensureSql, conn))
                    {
                        ensureCmd.ExecuteNonQuery();
                    }

                    string sql = @"INSERT INTO LichSuHeThong (MaNV, ThaoTac, ChiTiet, NgayTao) 
                                   VALUES (@MaNV, @ThaoTac, @ChiTiet, GETDATE())";
                    using (var cmd = new SqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@MaNV", maNV);
                        cmd.Parameters.AddWithValue("@ThaoTac", thaoTac);
                        cmd.Parameters.AddWithValue("@ChiTiet", (object?)chiTiet ?? DBNull.Value);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch { /* Silent fail for logging */ }
        }
    }
}
