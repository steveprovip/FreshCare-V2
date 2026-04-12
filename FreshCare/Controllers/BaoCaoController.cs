using FreshCare.Helpers;
using FreshCare.Models;
using FreshCare.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace FreshCare.Controllers
{
    /// <summary>
    /// BaoCaoController - Báo cáo tồn kho & hao hụt
    /// Luật #13: Doanh thu chỉ tính từ "Bán Hàng"; "Hủy Hàng" tính vào thất thoát
    /// </summary>
    public class BaoCaoController : Controller
    {
        private readonly string _connectionString;

        public BaoCaoController(string connectionString)
        {
            _connectionString = connectionString;
        }

        // GET: /BaoCao/TonKho
        public IActionResult TonKho()
        {
            if (HttpContext.Session.GetInt32("MaNV") == null)
                return RedirectToAction("DangNhap", "TaiKhoan");

            var list = new List<BaoCaoTonKho>();

            try
            {
                using (var conn = DatabaseHelper.GetConnection(_connectionString))
                {
                    conn.Open();

                    // Lấy tồn kho theo sản phẩm
                    string sql = @"SELECT sp.MaSP, sp.TenSP, sp.DonViTinh, dm.TenDanhMuc,
                                          ISNULL(SUM(lh.SoLuongTon), 0) AS TongTon,
                                          COUNT(lh.MaLo) AS SoLo
                                   FROM SanPham sp
                                   INNER JOIN DanhMuc dm ON sp.MaDanhMuc = dm.MaDanhMuc
                                   LEFT JOIN LoHang lh ON sp.MaSP = lh.MaSP AND lh.SoLuongTon > 0 AND lh.TrangThai != N'Đã Hủy'
                                   WHERE sp.TrangThai = N'HoatDong'
                                   GROUP BY sp.MaSP, sp.TenSP, sp.DonViTinh, dm.TenDanhMuc
                                   ORDER BY sp.TenSP";

                    using (var cmd = new SqlCommand(sql, conn))
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            list.Add(new BaoCaoTonKho
                            {
                                MaSP = Convert.ToInt32(reader["MaSP"]),
                                TenSP = reader["TenSP"].ToString()!,
                                DonViTinh = reader["DonViTinh"].ToString()!,
                                TenDanhMuc = reader["TenDanhMuc"].ToString()!,
                                TongTon = Convert.ToDecimal(reader["TongTon"]),
                                SoLo = Convert.ToInt32(reader["SoLo"])
                            });
                        }
                    }

                    // Lấy chi tiết lô cho mỗi sản phẩm
                    foreach (var sp in list)
                    {
                        string sqlLo = @"SELECT MaLo, SoLuongTon, NgaySanXuat, HanSuDung, TrangThai
                                         FROM LoHang
                                         WHERE MaSP = @MaSP AND SoLuongTon > 0 AND TrangThai != N'Đã Hủy'
                                         ORDER BY HanSuDung ASC";
                        using (var cmd = new SqlCommand(sqlLo, conn))
                        {
                            cmd.Parameters.AddWithValue("@MaSP", sp.MaSP);
                            using (var reader = cmd.ExecuteReader())
                            {
                                while (reader.Read())
                                {
                                    sp.ChiTietLo.Add(new LoHang
                                    {
                                        MaLo = Convert.ToInt32(reader["MaLo"]),
                                        SoLuongTon = Convert.ToDecimal(reader["SoLuongTon"]),
                                        NgaySanXuat = Convert.ToDateTime(reader["NgaySanXuat"]),
                                        HanSuDung = Convert.ToDateTime(reader["HanSuDung"]),
                                        TrangThai = reader["TrangThai"].ToString()!
                                    });
                                }
                            }
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

        // GET: /BaoCao/DoanhThu
        public IActionResult DoanhThu(DateTime? tuNgay, DateTime? denNgay)
        {
            if (HttpContext.Session.GetInt32("MaNV") == null)
                return RedirectToAction("DangNhap", "TaiKhoan");

            // Chỉ Admin mới xem được (Luật FR8)
            if (HttpContext.Session.GetString("VaiTro") != "Admin")
            {
                TempData["Error"] = "Chỉ Quản lý mới có quyền xem báo cáo doanh thu!";
                return RedirectToAction("Index", "Home");
            }

            var model = new BaoCaoViewModel
            {
                TuNgay = tuNgay ?? DateTime.Today.AddDays(-30),
                DenNgay = denNgay ?? DateTime.Today
            };

            try
            {
                using (var conn = DatabaseHelper.GetConnection(_connectionString))
                {
                    conn.Open();

                    // Luật #13: Doanh thu chỉ tính từ phiếu "Bán Hàng"
                    string sqlDoanhThu = @"SELECT ISNULL(SUM(TongTien), 0) 
                                           FROM PhieuXuat 
                                           WHERE LoaiPhieu = N'Bán Hàng' 
                                                 AND NgayXuat >= @TuNgay AND NgayXuat <= @DenNgay";
                    using (var cmd = new SqlCommand(sqlDoanhThu, conn))
                    {
                        cmd.Parameters.AddWithValue("@TuNgay", model.TuNgay);
                        cmd.Parameters.AddWithValue("@DenNgay", model.DenNgay.Value.AddDays(1));
                        model.TongDoanhThu = Convert.ToDecimal(cmd.ExecuteScalar());
                    }

                    // Luật #13: Thất thoát tính từ phiếu "Hủy Hàng"
                    string sqlThatThoat = @"SELECT ISNULL(SUM(ctx.SoLuong * sp.GiaBan), 0)
                                            FROM PhieuXuat px
                                            INNER JOIN ChiTietXuat ctx ON px.MaPhieuXuat = ctx.MaPhieuXuat
                                            INNER JOIN LoHang lh ON ctx.MaLo = lh.MaLo
                                            INNER JOIN SanPham sp ON lh.MaSP = sp.MaSP
                                            WHERE px.LoaiPhieu = N'Hủy Hàng'
                                                  AND px.NgayXuat >= @TuNgay AND px.NgayXuat <= @DenNgay";
                    using (var cmd = new SqlCommand(sqlThatThoat, conn))
                    {
                        cmd.Parameters.AddWithValue("@TuNgay", model.TuNgay);
                        cmd.Parameters.AddWithValue("@DenNgay", model.DenNgay.Value.AddDays(1));
                        model.TongThatThoat = Convert.ToDecimal(cmd.ExecuteScalar());
                    }

                    // Danh sách phiếu bán
                    string sqlPhieuBan = @"SELECT px.MaPhieuXuat, px.NgayXuat, px.TongTien, nv.HoTen AS TenNhanVien,
                                                 (SELECT STRING_AGG(sp2.TenSP, N', ') 
                                                  FROM ChiTietXuat ct2 
                                                  INNER JOIN LoHang lh2 ON ct2.MaLo = lh2.MaLo 
                                                  INNER JOIN SanPham sp2 ON lh2.MaSP = sp2.MaSP 
                                                  WHERE ct2.MaPhieuXuat = px.MaPhieuXuat) AS TenMatHang
                                           FROM PhieuXuat px
                                           INNER JOIN NhanVien nv ON px.MaNV = nv.MaNV
                                           WHERE px.LoaiPhieu = N'Bán Hàng'
                                                 AND px.NgayXuat >= @TuNgay AND px.NgayXuat <= @DenNgay
                                           ORDER BY px.NgayXuat DESC";
                    using (var cmd = new SqlCommand(sqlPhieuBan, conn))
                    {
                        cmd.Parameters.AddWithValue("@TuNgay", model.TuNgay);
                        cmd.Parameters.AddWithValue("@DenNgay", model.DenNgay.Value.AddDays(1));
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                model.PhieuBanList.Add(new PhieuXuat
                                {
                                    MaPhieuXuat = Convert.ToInt32(reader["MaPhieuXuat"]),
                                    NgayXuat = Convert.ToDateTime(reader["NgayXuat"]),
                                    TongTien = Convert.ToDecimal(reader["TongTien"]),
                                    LoaiPhieu = "Bán Hàng",
                                    TenNhanVien = reader["TenNhanVien"].ToString(),
                                    TenMatHang = reader["TenMatHang"]?.ToString()
                                });
                            }
                        }
                    }

                    // Danh sách phiếu hủy
                    string sqlPhieuHuy = @"SELECT px.MaPhieuXuat, px.NgayXuat, px.GhiChu, nv.HoTen AS TenNhanVien,
                                                 (SELECT STRING_AGG(sp2.TenSP, N', ') 
                                                  FROM ChiTietXuat ct2 
                                                  INNER JOIN LoHang lh2 ON ct2.MaLo = lh2.MaLo 
                                                  INNER JOIN SanPham sp2 ON lh2.MaSP = sp2.MaSP 
                                                  WHERE ct2.MaPhieuXuat = px.MaPhieuXuat) AS TenMatHang
                                           FROM PhieuXuat px
                                           INNER JOIN NhanVien nv ON px.MaNV = nv.MaNV
                                           WHERE px.LoaiPhieu = N'Hủy Hàng'
                                                 AND px.NgayXuat >= @TuNgay AND px.NgayXuat <= @DenNgay
                                           ORDER BY px.NgayXuat DESC";
                    using (var cmd = new SqlCommand(sqlPhieuHuy, conn))
                    {
                        cmd.Parameters.AddWithValue("@TuNgay", model.TuNgay);
                        cmd.Parameters.AddWithValue("@DenNgay", model.DenNgay.Value.AddDays(1));
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                model.PhieuHuyList.Add(new PhieuXuat
                                {
                                    MaPhieuXuat = Convert.ToInt32(reader["MaPhieuXuat"]),
                                    NgayXuat = Convert.ToDateTime(reader["NgayXuat"]),
                                    LoaiPhieu = "Hủy Hàng",
                                    GhiChu = reader["GhiChu"]?.ToString(),
                                    TenNhanVien = reader["TenNhanVien"].ToString(),
                                    TenMatHang = reader["TenMatHang"]?.ToString()
                                });
                            }
                        }
                    }

                    // Thống kê mặt hàng bán chạy nhất (Top 5)
                    string sqlTopBanChay = @"SELECT TOP 5 sp.MaSP, sp.TenSP, sp.DonViTinh,
                                                   SUM(ct.SoLuong) AS TongSoLuong,
                                                   SUM(ct.SoLuong * ct.DonGia) AS TongDoanhThu
                                            FROM ChiTietXuat ct
                                            INNER JOIN LoHang lh ON ct.MaLo = lh.MaLo
                                            INNER JOIN SanPham sp ON lh.MaSP = sp.MaSP
                                            INNER JOIN PhieuXuat px ON ct.MaPhieuXuat = px.MaPhieuXuat
                                            WHERE px.LoaiPhieu = N'Bán Hàng'
                                                  AND px.NgayXuat >= @TuNgay AND px.NgayXuat <= @DenNgay
                                            GROUP BY sp.MaSP, sp.TenSP, sp.DonViTinh
                                            ORDER BY TongSoLuong DESC";
                    using (var cmd = new SqlCommand(sqlTopBanChay, conn))
                    {
                        cmd.Parameters.AddWithValue("@TuNgay", model.TuNgay);
                        cmd.Parameters.AddWithValue("@DenNgay", model.DenNgay.Value.AddDays(1));
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                model.TopBanChay.Add(new ThongKeSanPham
                                {
                                    MaSP = Convert.ToInt32(reader["MaSP"]),
                                    TenSP = reader["TenSP"].ToString()!,
                                    DonViTinh = reader["DonViTinh"].ToString()!,
                                    TongSoLuong = Convert.ToDecimal(reader["TongSoLuong"]),
                                    TongDoanhThu = Convert.ToDecimal(reader["TongDoanhThu"])
                                });
                            }
                        }
                    }

                    // Thống kê mặt hàng bán chậm nhất (Top 5)
                    // Loại trừ các MaSP đã nằm trong TopBanChay để tránh trùng lặp
                    var excludeIds = model.TopBanChay.Select(x => x.MaSP).ToList();
                    string excludeClause = "";
                    if (excludeIds.Any())
                    {
                        excludeClause = " AND sp.MaSP NOT IN (" + string.Join(",", excludeIds.Select((_, i) => $"@ExcSP{i}")) + ")";
                    }

                    string sqlTopBanCham = $@"SELECT TOP 5 sp.MaSP, sp.TenSP, sp.DonViTinh,
                                                   SUM(ct.SoLuong) AS TongSoLuong,
                                                   SUM(ct.SoLuong * ct.DonGia) AS TongDoanhThu
                                            FROM ChiTietXuat ct
                                            INNER JOIN LoHang lh ON ct.MaLo = lh.MaLo
                                            INNER JOIN SanPham sp ON lh.MaSP = sp.MaSP
                                            INNER JOIN PhieuXuat px ON ct.MaPhieuXuat = px.MaPhieuXuat
                                            WHERE px.LoaiPhieu = N'Bán Hàng'
                                                  AND px.NgayXuat >= @TuNgay AND px.NgayXuat <= @DenNgay
                                                  {excludeClause}
                                            GROUP BY sp.MaSP, sp.TenSP, sp.DonViTinh
                                            ORDER BY TongSoLuong ASC";
                    using (var cmd = new SqlCommand(sqlTopBanCham, conn))
                    {
                        cmd.Parameters.AddWithValue("@TuNgay", model.TuNgay);
                        cmd.Parameters.AddWithValue("@DenNgay", model.DenNgay.Value.AddDays(1));
                        for (int i = 0; i < excludeIds.Count; i++)
                        {
                            cmd.Parameters.AddWithValue($"@ExcSP{i}", excludeIds[i]);
                        }
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                model.TopBanCham.Add(new ThongKeSanPham
                                {
                                    MaSP = Convert.ToInt32(reader["MaSP"]),
                                    TenSP = reader["TenSP"].ToString()!,
                                    DonViTinh = reader["DonViTinh"].ToString()!,
                                    TongSoLuong = Convert.ToDecimal(reader["TongSoLuong"]),
                                    TongDoanhThu = Convert.ToDecimal(reader["TongDoanhThu"])
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
    }
}
