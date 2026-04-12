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
                HanSuDung = DateTime.Today.AddDays(14),
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

            // Xây dựng danh sách items: hỗ trợ cả nhập đơn (cũ) và nhập nhiều (mới)
            var danhSachItems = new List<NhapKhoItem>();
            if (model.DanhSachNhap != null && model.DanhSachNhap.Any(x => x.MaSP > 0))
            {
                danhSachItems = model.DanhSachNhap.Where(x => x.MaSP > 0 && x.SoLuong > 0).ToList();
            }
            else if (model.MaSP > 0 && model.SoLuong > 0)
            {
                danhSachItems.Add(new NhapKhoItem
                {
                    MaSP = model.MaSP,
                    SoLuong = model.SoLuong,
                    NgaySanXuat = model.NgaySanXuat,
                    HanSuDung = model.HanSuDung
                });
            }

            if (!danhSachItems.Any())
            {
                TempData["Error"] = "Lỗi: Vui lòng thêm ít nhất một mặt hàng!";
                return View("Index", model);
            }

            // Luật #17: Kiểm tra HSD >= NSX cho tất cả items
            foreach (var item in danhSachItems)
            {
                if (item.HanSuDung < item.NgaySanXuat)
                {
                    TempData["Error"] = "Lỗi: Hạn sử dụng không được nhỏ hơn Ngày sản xuất!";
                    return View("Index", model);
                }
            }

            int maNV = HttpContext.Session.GetInt32("MaNV") ?? 0;
            int maPhieuResult = 0;

            try
            {
                using (var conn = DatabaseHelper.GetConnection(_connectionString))
                {
                    conn.Open();

                    // Luật #10: Kiểm tra đơn vị tính cho từng item
                    foreach (var item in danhSachItems)
                    {
                        string donViTinh = "";
                        string sqlDVT = "SELECT DonViTinh FROM SanPham WHERE MaSP = @MaSP";
                        using (var cmdDVT = new SqlCommand(sqlDVT, conn))
                        {
                            cmdDVT.Parameters.AddWithValue("@MaSP", item.MaSP);
                            donViTinh = cmdDVT.ExecuteScalar()?.ToString() ?? "";
                        }
                        if (donViTinh != "Kg" && item.SoLuong != Math.Floor(item.SoLuong))
                        {
                            TempData["Error"] = $"Lỗi: Đơn vị \"{donViTinh}\" chỉ cho phép nhập số nguyên!";
                            return View("Index", model);
                        }
                    }

                    using (var transaction = conn.BeginTransaction())
                    {
                        try
                        {
                            // 1. Tạo 1 phiếu nhập kho chung
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

                            // 2. Vòng lặp: Tạo lô hàng + chi tiết nhập cho từng mặt hàng
                            foreach (var item in danhSachItems)
                            {
                                string trangThai = DatabaseHelper.PhanLoaiTrangThai(item.HanSuDung);
                                string sqlLoHang = @"INSERT INTO LoHang (MaSP, SoLuongNhap, SoLuongTon, NgaySanXuat, HanSuDung, NgayNhapKho, TrangThai)
                                                     OUTPUT INSERTED.MaLo
                                                     VALUES (@MaSP, @SoLuong, @SoLuong, @NgaySanXuat, @HanSuDung, GETDATE(), @TrangThai)";
                                int maLo;
                                using (var cmd = new SqlCommand(sqlLoHang, conn, transaction))
                                {
                                    cmd.Parameters.AddWithValue("@MaSP", item.MaSP);
                                    cmd.Parameters.AddWithValue("@SoLuong", item.SoLuong);
                                    cmd.Parameters.AddWithValue("@NgaySanXuat", item.NgaySanXuat);
                                    cmd.Parameters.AddWithValue("@HanSuDung", item.HanSuDung);
                                    cmd.Parameters.AddWithValue("@TrangThai", trangThai);
                                    maLo = Convert.ToInt32(cmd.ExecuteScalar());
                                }

                                string sqlChiTiet = @"INSERT INTO ChiTietNhap (MaPhieuNhap, MaLo, SoLuong)
                                                      VALUES (@MaPhieuNhap, @MaLo, @SoLuong)";
                                using (var cmd = new SqlCommand(sqlChiTiet, conn, transaction))
                                {
                                    cmd.Parameters.AddWithValue("@MaPhieuNhap", maPhieu);
                                    cmd.Parameters.AddWithValue("@MaLo", maLo);
                                    cmd.Parameters.AddWithValue("@SoLuong", item.SoLuong);
                                    cmd.ExecuteNonQuery();
                                }
                            }

                            transaction.Commit();
                            maPhieuResult = maPhieu;
                            TempData["Success"] = $"Nhập kho thành công! Phiếu nhập: PN-{maPhieu:D4} ({danhSachItems.Count} mặt hàng)";
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

            if (maPhieuResult > 0)
                return RedirectToAction("ChiTiet", new { id = maPhieuResult });

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
                    string sql = @"SELECT pn.MaPhieuNhap, pn.NgayNhap, pn.GhiChu, nv.HoTen AS TenNhanVien,
                                          (SELECT STRING_AGG(sp2.TenSP, N', ') 
                                           FROM ChiTietNhap ct2 
                                           INNER JOIN LoHang lh2 ON ct2.MaLo = lh2.MaLo 
                                           INNER JOIN SanPham sp2 ON lh2.MaSP = sp2.MaSP 
                                           WHERE ct2.MaPhieuNhap = pn.MaPhieuNhap) AS TenMatHang
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
                                TenNhanVien = reader["TenNhanVien"].ToString(),
                                TenMatHang = reader["TenMatHang"]?.ToString()
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
                    string sql = "SELECT MaSP, TenSP, DonViTinh, GiaNhap, GiaBan FROM SanPham WHERE MaSP = @MaSP";
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
                                    giaNhap = Convert.ToDecimal(reader["GiaNhap"]),
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
                    string sql = @"SELECT sp.MaSP, sp.TenSP, sp.DonViTinh, sp.GiaNhap, sp.GiaBan, dm.TenDanhMuc
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
                                GiaNhap = Convert.ToDecimal(reader["GiaNhap"]),
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

        // GET: /NhapKho/ChiTiet - Xem chi tiết phiếu nhập & In phiếu
        public IActionResult ChiTiet(int id)
        {
            var model = new ChiTietPhieuNhapViewModel();

            try
            {
                using (var conn = DatabaseHelper.GetConnection(_connectionString))
                {
                    conn.Open();

                    // 1. Lấy thông tin phiếu nhập
                    string sqlPhieu = @"SELECT pn.MaPhieuNhap, pn.NgayNhap, pn.GhiChu, nv.HoTen
                                        FROM PhieuNhapKho pn
                                        INNER JOIN NhanVien nv ON pn.MaNV = nv.MaNV
                                        WHERE pn.MaPhieuNhap = @Id";
                    using (var cmd = new SqlCommand(sqlPhieu, conn))
                    {
                        cmd.Parameters.AddWithValue("@Id", id);
                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                model.MaPhieuNhap = Convert.ToInt32(reader["MaPhieuNhap"]);
                                model.NgayNhap = Convert.ToDateTime(reader["NgayNhap"]);
                                model.GhiChu = reader["GhiChu"]?.ToString();
                                model.TenNhanVien = reader["HoTen"].ToString()!;
                            }
                        }
                    }

                    // 2. Lấy chi tiết nhập (tên hàng hóa, đơn vị, giá nhập, NSX, HSD)
                    string sqlChiTiet = @"SELECT ct.MaLo, sp.TenSP, sp.DonViTinh, sp.GiaNhap, 
                                                ct.SoLuong, lh.NgaySanXuat, lh.HanSuDung
                                         FROM ChiTietNhap ct
                                         INNER JOIN LoHang lh ON ct.MaLo = lh.MaLo
                                         INNER JOIN SanPham sp ON lh.MaSP = sp.MaSP
                                         WHERE ct.MaPhieuNhap = @Id";
                    using (var cmd = new SqlCommand(sqlChiTiet, conn))
                    {
                        cmd.Parameters.AddWithValue("@Id", id);
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                model.DanhSachChiTiet.Add(new ChiTietNhapItem
                                {
                                    MaLo = Convert.ToInt32(reader["MaLo"]),
                                    TenSanPham = reader["TenSP"].ToString()!,
                                    DonViTinh = reader["DonViTinh"].ToString()!,
                                    GiaNhap = Convert.ToDecimal(reader["GiaNhap"]),
                                    SoLuong = Convert.ToDecimal(reader["SoLuong"]),
                                    NgaySanXuat = Convert.ToDateTime(reader["NgaySanXuat"]),
                                    HanSuDung = Convert.ToDateTime(reader["HanSuDung"])
                                });
                            }
                        }
                    }

                    model.TongTien = model.DanhSachChiTiet.Sum(x => x.SoLuong * x.GiaNhap);
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Lỗi: " + ex.Message;
            }

            return View(model);
        }

        // GET: /NhapKho/InTongHopNgay?ngay=2026-04-12
        public IActionResult InTongHopNgay(DateTime? ngay)
        {
            if (HttpContext.Session.GetInt32("MaNV") == null)
                return RedirectToAction("DangNhap", "TaiKhoan");

            DateTime ngayIn = ngay ?? DateTime.Today;
            var model = new TongHopNhapNgayViewModel { Ngay = ngayIn };

            try
            {
                using (var conn = DatabaseHelper.GetConnection(_connectionString))
                {
                    conn.Open();

                    // Lấy tất cả phiếu nhập trong ngày
                    string sqlPhieu = @"SELECT pn.MaPhieuNhap, pn.NgayNhap, pn.GhiChu, nv.HoTen
                                        FROM PhieuNhapKho pn
                                        INNER JOIN NhanVien nv ON pn.MaNV = nv.MaNV
                                        WHERE CAST(pn.NgayNhap AS DATE) = @Ngay
                                        ORDER BY pn.NgayNhap ASC";
                    var danhSachPhieu = new List<ChiTietPhieuNhapViewModel>();
                    using (var cmd = new SqlCommand(sqlPhieu, conn))
                    {
                        cmd.Parameters.AddWithValue("@Ngay", ngayIn.Date);
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                danhSachPhieu.Add(new ChiTietPhieuNhapViewModel
                                {
                                    MaPhieuNhap = Convert.ToInt32(reader["MaPhieuNhap"]),
                                    NgayNhap = Convert.ToDateTime(reader["NgayNhap"]),
                                    GhiChu = reader["GhiChu"]?.ToString(),
                                    TenNhanVien = reader["HoTen"].ToString()!
                                });
                            }
                        }
                    }

                    // Lấy chi tiết cho từng phiếu
                    foreach (var phieu in danhSachPhieu)
                    {
                        string sqlCT = @"SELECT ct.MaLo, sp.TenSP, sp.DonViTinh, sp.GiaNhap,
                                                ct.SoLuong, lh.NgaySanXuat, lh.HanSuDung
                                         FROM ChiTietNhap ct
                                         INNER JOIN LoHang lh ON ct.MaLo = lh.MaLo
                                         INNER JOIN SanPham sp ON lh.MaSP = sp.MaSP
                                         WHERE ct.MaPhieuNhap = @Id";
                        using (var cmd = new SqlCommand(sqlCT, conn))
                        {
                            cmd.Parameters.AddWithValue("@Id", phieu.MaPhieuNhap);
                            using (var reader = cmd.ExecuteReader())
                            {
                                while (reader.Read())
                                {
                                    phieu.DanhSachChiTiet.Add(new ChiTietNhapItem
                                    {
                                        MaLo = Convert.ToInt32(reader["MaLo"]),
                                        TenSanPham = reader["TenSP"].ToString()!,
                                        DonViTinh = reader["DonViTinh"].ToString()!,
                                        GiaNhap = Convert.ToDecimal(reader["GiaNhap"]),
                                        SoLuong = Convert.ToDecimal(reader["SoLuong"]),
                                        NgaySanXuat = Convert.ToDateTime(reader["NgaySanXuat"]),
                                        HanSuDung = Convert.ToDateTime(reader["HanSuDung"])
                                    });
                                }
                            }
                        }
                        phieu.TongTien = phieu.DanhSachChiTiet.Sum(x => x.SoLuong * x.GiaNhap);
                    }

                    model.DanhSachPhieu = danhSachPhieu;
                    model.TongTien = danhSachPhieu.Sum(p => p.TongTien);
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
