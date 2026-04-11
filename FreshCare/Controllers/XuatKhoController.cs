using FreshCare.Helpers;
using FreshCare.Models;
using FreshCare.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace FreshCare.Controllers
{
    /// <summary>
    /// XuatKhoController - Xuất kho theo thuật toán FEFO & Hủy hàng
    /// Luật #11: FEFO bằng ORDER BY HanSuDung ASC
    /// Luật #12: Phân loại phiếu "Bán Hàng" và "Hủy Hàng"
    /// Luật #5: Không sử dụng DELETE
    /// </summary>
    public class XuatKhoController : Controller
    {
        private readonly string _connectionString;

        public XuatKhoController(string connectionString)
        {
            _connectionString = connectionString;
        }

        // GET: /XuatKho/BanHang
        public IActionResult BanHang()
        {
            if (HttpContext.Session.GetInt32("MaNV") == null)
                return RedirectToAction("DangNhap", "TaiKhoan");

            var model = new XuatKhoViewModel
            {
                DanhSachSanPham = LayDanhSachSanPhamConTon()
            };
            return View(model);
        }

        /// <summary>
        /// POST: /XuatKho/ThanhToan
        /// THUẬT TOÁN FEFO (First Expired, First Out) - Luật #11
        /// ORDER BY HanSuDung ASC → ưu tiên xuất lô hết hạn sớm nhất
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ThanhToan(XuatKhoViewModel model)
        {
            int maNV = HttpContext.Session.GetInt32("MaNV") ?? 0;

            try
            {
                using (var conn = DatabaseHelper.GetConnection(_connectionString))
                {
                    conn.Open();

                    // === BƯỚC 1: Kiểm tra tổng tồn kho an toàn (chỉ lô chưa quá hạn) ===
                    string sqlCheckTon = @"SELECT ISNULL(SUM(SoLuongTon), 0) 
                                           FROM LoHang 
                                           WHERE MaSP = @MaSP AND SoLuongTon > 0 
                                                 AND TrangThai NOT IN (N'Quá Hạn', N'Đã Hủy')";
                    decimal tongTon;
                    using (var cmd = new SqlCommand(sqlCheckTon, conn))
                    {
                        cmd.Parameters.AddWithValue("@MaSP", model.MaSP);
                        tongTon = Convert.ToDecimal(cmd.ExecuteScalar());
                    }

                    // Chặn xuất quá tồn
                    if (model.SoLuong > tongTon)
                    {
                        TempData["Error"] = $"Lỗi: Kho không đủ hàng an toàn để xuất! Tồn kho hiện có: {tongTon:N2}";
                        return RedirectToAction("BanHang");
                    }

                    // Luật #10: Kiểm tra đơn vị tính - chỉ Kg cho phép số thập phân
                    string donViTinh = "";
                    string sqlDVT = "SELECT DonViTinh FROM SanPham WHERE MaSP = @MaSP";
                    using (var cmdDVT = new SqlCommand(sqlDVT, conn))
                    {
                        cmdDVT.Parameters.AddWithValue("@MaSP", model.MaSP);
                        donViTinh = cmdDVT.ExecuteScalar()?.ToString() ?? "";
                    }

                    if (donViTinh != "Kg" && model.SoLuong != Math.Floor(model.SoLuong))
                    {
                        TempData["Error"] = $"Lỗi: Đơn vị \"{donViTinh}\" chỉ cho phép nhập số nguyên!";
                        return RedirectToAction("BanHang");
                    }

                    using (var transaction = conn.BeginTransaction())
                    {
                        try
                        {
                            // === BƯỚC 2: Tạo phiếu xuất loại "Bán Hàng" (Luật #12) ===
                            string sqlPhieu = @"INSERT INTO PhieuXuat (NgayXuat, MaNV, LoaiPhieu, TongTien, GhiChu)
                                                OUTPUT INSERTED.MaPhieuXuat
                                                VALUES (GETDATE(), @MaNV, N'Bán Hàng', 0, @GhiChu)";
                            int maPhieuXuat;
                            using (var cmd = new SqlCommand(sqlPhieu, conn, transaction))
                            {
                                cmd.Parameters.AddWithValue("@MaNV", maNV);
                                cmd.Parameters.AddWithValue("@GhiChu", (object?)model.GhiChu ?? DBNull.Value);
                                maPhieuXuat = Convert.ToInt32(cmd.ExecuteScalar());
                            }

                            // === BƯỚC 3: Thuật toán FEFO + FIFO backup ===
                            // FEFO: HanSuDung ASC (hết hạn trước xuất trước)
                            // FIFO backup: NgayNhapKho ASC (nếu cùng HSD thì nhập trước xuất trước)
                            string sqlFEFO = @"SELECT MaLo, SoLuongTon, HanSuDung
                                               FROM LoHang
                                               WHERE MaSP = @MaSP AND SoLuongTon > 0 
                                                     AND TrangThai NOT IN (N'Quá Hạn', N'Đã Hủy')
                                               ORDER BY HanSuDung ASC, NgayNhapKho ASC";

                            var danhSachLo = new List<(int MaLo, decimal SoLuongTon, DateTime HanSuDung)>();
                            using (var cmd = new SqlCommand(sqlFEFO, conn, transaction))
                            {
                                cmd.Parameters.AddWithValue("@MaSP", model.MaSP);
                                using (var reader = cmd.ExecuteReader())
                                {
                                    while (reader.Read())
                                    {
                                        danhSachLo.Add((
                                            Convert.ToInt32(reader["MaLo"]),
                                            Convert.ToDecimal(reader["SoLuongTon"]),
                                            Convert.ToDateTime(reader["HanSuDung"])
                                        ));
                                    }
                                }
                            }

                            // Lấy giá bán và % sale
                            decimal giaBan = 0, phanTramSale = 0;
                            string sqlGia = @"SELECT sp.GiaBan, dm.PhanTramSale
                                              FROM SanPham sp 
                                              INNER JOIN DanhMuc dm ON sp.MaDanhMuc = dm.MaDanhMuc
                                              WHERE sp.MaSP = @MaSP";
                            using (var cmd = new SqlCommand(sqlGia, conn, transaction))
                            {
                                cmd.Parameters.AddWithValue("@MaSP", model.MaSP);
                                using (var reader = cmd.ExecuteReader())
                                {
                                    if (reader.Read())
                                    {
                                        giaBan = Convert.ToDecimal(reader["GiaBan"]);
                                        phanTramSale = Convert.ToDecimal(reader["PhanTramSale"]);
                                    }
                                }
                            }

                            // VÒNG LẶP TRỪ KHO (mô tả trong báo cáo Chương V - 5.4.3)
                            decimal soLuongConLai = model.SoLuong;
                            decimal tongTienXuat = 0;

                            foreach (var lo in danhSachLo)
                            {
                                if (soLuongConLai <= 0) break;

                                // Luật #8: Tính giá sale LŨY TIẾN cho lô cận date
                                string trangThaiLo = DatabaseHelper.PhanLoaiTrangThai(lo.HanSuDung);
                                decimal donGiaXuat = giaBan;
                                if (trangThaiLo == "Cận Date" && phanTramSale > 0)
                                {
                                    int soNgayConLai = (lo.HanSuDung.Date - DateTime.Now.Date).Days;
                                    if (soNgayConLai < 0) soNgayConLai = 0;
                                    decimal factor = (decimal)(14 - soNgayConLai) / 14m;
                                    decimal actualSale = phanTramSale * (1m + factor);
                                    if (actualSale > 80m) actualSale = 80m;
                                    actualSale = Math.Round(actualSale, 2);
                                    donGiaXuat = giaBan * (100 - actualSale) / 100;
                                }

                                decimal soLuongTru;

                                if (lo.SoLuongTon >= soLuongConLai)
                                {
                                    // Lô này đủ → trừ hết lượng cần xuất
                                    soLuongTru = soLuongConLai;
                                    soLuongConLai = 0;
                                }
                                else
                                {
                                    // Lô này không đủ → trừ sạch, chuyển sang lô kế tiếp
                                    soLuongTru = lo.SoLuongTon;
                                    soLuongConLai -= lo.SoLuongTon;
                                }

                                // Cập nhật SoLuongTon của lô
                                string sqlUpdateLo = "UPDATE LoHang SET SoLuongTon = SoLuongTon - @SoLuongTru WHERE MaLo = @MaLo";
                                using (var cmd = new SqlCommand(sqlUpdateLo, conn, transaction))
                                {
                                    cmd.Parameters.AddWithValue("@SoLuongTru", soLuongTru);
                                    cmd.Parameters.AddWithValue("@MaLo", lo.MaLo);
                                    cmd.ExecuteNonQuery();
                                }

                                // Ghi chi tiết xuất
                                string sqlChiTiet = @"INSERT INTO ChiTietXuat (MaPhieuXuat, MaLo, SoLuong, DonGia)
                                                      VALUES (@MaPhieuXuat, @MaLo, @SoLuong, @DonGia)";
                                using (var cmd = new SqlCommand(sqlChiTiet, conn, transaction))
                                {
                                    cmd.Parameters.AddWithValue("@MaPhieuXuat", maPhieuXuat);
                                    cmd.Parameters.AddWithValue("@MaLo", lo.MaLo);
                                    cmd.Parameters.AddWithValue("@SoLuong", soLuongTru);
                                    cmd.Parameters.AddWithValue("@DonGia", donGiaXuat);
                                    cmd.ExecuteNonQuery();
                                }

                                tongTienXuat += soLuongTru * donGiaXuat;
                            }

                            // Cập nhật tổng tiền phiếu xuất
                            string sqlUpdatePhieu = "UPDATE PhieuXuat SET TongTien = @TongTien WHERE MaPhieuXuat = @MaPhieuXuat";
                            using (var cmd = new SqlCommand(sqlUpdatePhieu, conn, transaction))
                            {
                                cmd.Parameters.AddWithValue("@TongTien", tongTienXuat);
                                cmd.Parameters.AddWithValue("@MaPhieuXuat", maPhieuXuat);
                                cmd.ExecuteNonQuery();
                            }

                            transaction.Commit();

                            TempData["Success"] = $"Xuất kho thành công! Hóa đơn: HD-{maPhieuXuat:D4}. Tổng tiền: {tongTienXuat:N0}đ";
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
                TempData["Error"] = "Lỗi xuất kho: " + ex.Message;
            }

            return RedirectToAction("BanHang");
        }

        /// <summary>
        /// POST: /XuatKho/HuyHang - Xuất hủy hàng hết hạn
        /// Luật #5: Không DELETE, cập nhật trạng thái → "Đã Hủy"
        /// Luật #12: LoaiPhieu = "Hủy Hàng"
        /// </summary>
        [HttpPost]
        public IActionResult HuyHang(int maLo)
        {
            int maNV = HttpContext.Session.GetInt32("MaNV") ?? 0;

            try
            {
                using (var conn = DatabaseHelper.GetConnection(_connectionString))
                {
                    conn.Open();
                    using (var transaction = conn.BeginTransaction())
                    {
                        try
                        {
                            // Lấy thông tin lô hàng
                            decimal soLuongHuy = 0;
                            decimal giaBanGoc = 0;
                            string sqlLo = @"SELECT lh.SoLuongTon, sp.GiaBan
                                             FROM LoHang lh 
                                             INNER JOIN SanPham sp ON lh.MaSP = sp.MaSP
                                             WHERE lh.MaLo = @MaLo";
                            using (var cmd = new SqlCommand(sqlLo, conn, transaction))
                            {
                                cmd.Parameters.AddWithValue("@MaLo", maLo);
                                using (var reader = cmd.ExecuteReader())
                                {
                                    if (reader.Read())
                                    {
                                        soLuongHuy = Convert.ToDecimal(reader["SoLuongTon"]);
                                        giaBanGoc = Convert.ToDecimal(reader["GiaBan"]);
                                    }
                                }
                            }

                            // Tạo phiếu xuất loại "Hủy Hàng" (Luật #12)
                            string sqlPhieu = @"INSERT INTO PhieuXuat (NgayXuat, MaNV, LoaiPhieu, TongTien, GhiChu)
                                                OUTPUT INSERTED.MaPhieuXuat
                                                VALUES (GETDATE(), @MaNV, N'Hủy Hàng', 0, N'Xuất hủy lô hàng quá hạn')";
                            int maPhieuXuat;
                            using (var cmd = new SqlCommand(sqlPhieu, conn, transaction))
                            {
                                cmd.Parameters.AddWithValue("@MaNV", maNV);
                                maPhieuXuat = Convert.ToInt32(cmd.ExecuteScalar());
                            }

                            // Ghi chi tiết xuất hủy (giá = 0 vì hủy)
                            string sqlChiTiet = @"INSERT INTO ChiTietXuat (MaPhieuXuat, MaLo, SoLuong, DonGia)
                                                  VALUES (@MaPhieuXuat, @MaLo, @SoLuong, 0)";
                            using (var cmd = new SqlCommand(sqlChiTiet, conn, transaction))
                            {
                                cmd.Parameters.AddWithValue("@MaPhieuXuat", maPhieuXuat);
                                cmd.Parameters.AddWithValue("@MaLo", maLo);
                                cmd.Parameters.AddWithValue("@SoLuong", soLuongHuy);
                                cmd.ExecuteNonQuery();
                            }

                            // Luật #5: Cập nhật trạng thái → "Đã Hủy", SoLuongTon = 0
                            string sqlUpdate = "UPDATE LoHang SET SoLuongTon = 0, TrangThai = N'Đã Hủy' WHERE MaLo = @MaLo";
                            using (var cmd = new SqlCommand(sqlUpdate, conn, transaction))
                            {
                                cmd.Parameters.AddWithValue("@MaLo", maLo);
                                cmd.ExecuteNonQuery();
                            }

                            transaction.Commit();

                            TempData["Success"] = $"Đã xuất hủy lô LO-{maLo:D4}. Giá trị thất thoát: {soLuongHuy * giaBanGoc:N0}đ";
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
                TempData["Error"] = "Lỗi hủy hàng: " + ex.Message;
            }

            return RedirectToAction("Index", "Home");
        }

        // GET: /XuatKho/LichSu
        public IActionResult LichSu()
        {
            if (HttpContext.Session.GetInt32("MaNV") == null)
                return RedirectToAction("DangNhap", "TaiKhoan");

            var list = new List<PhieuXuat>();
            try
            {
                using (var conn = DatabaseHelper.GetConnection(_connectionString))
                {
                    conn.Open();
                    string sql = @"SELECT px.MaPhieuXuat, px.NgayXuat, px.LoaiPhieu, px.TongTien, px.GhiChu, nv.HoTen AS TenNhanVien
                                   FROM PhieuXuat px
                                   INNER JOIN NhanVien nv ON px.MaNV = nv.MaNV
                                   ORDER BY px.NgayXuat DESC";
                    using (var cmd = new SqlCommand(sql, conn))
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            list.Add(new PhieuXuat
                            {
                                MaPhieuXuat = Convert.ToInt32(reader["MaPhieuXuat"]),
                                NgayXuat = Convert.ToDateTime(reader["NgayXuat"]),
                                LoaiPhieu = reader["LoaiPhieu"].ToString()!,
                                TongTien = Convert.ToDecimal(reader["TongTien"]),
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

        // AJAX: Lấy tồn kho theo sản phẩm (Luật #14)
        [HttpGet]
        public IActionResult LayTonKho(int maSP)
        {
            try
            {
                using (var conn = DatabaseHelper.GetConnection(_connectionString))
                {
                    conn.Open();
                    string sql = @"SELECT ISNULL(SUM(SoLuongTon), 0) AS TongTon
                                   FROM LoHang
                                   WHERE MaSP = @MaSP AND SoLuongTon > 0 
                                         AND TrangThai NOT IN (N'Quá Hạn', N'Đã Hủy')";
                    using (var cmd = new SqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@MaSP", maSP);
                        var ton = Convert.ToDecimal(cmd.ExecuteScalar());

                        // Lấy đơn vị tính
                        string sqlDVT = "SELECT DonViTinh FROM SanPham WHERE MaSP = @MaSP";
                        using (var cmd2 = new SqlCommand(sqlDVT, conn))
                        {
                            cmd2.Parameters.AddWithValue("@MaSP", maSP);
                            var dvt = cmd2.ExecuteScalar()?.ToString() ?? "";
                            return Json(new { success = true, tongTon = ton, donViTinh = dvt });
                        }
                    }
                }
            }
            catch { }
            return Json(new { success = false });
        }

        private List<SanPham> LayDanhSachSanPhamConTon()
        {
            var list = new List<SanPham>();
            try
            {
                using (var conn = DatabaseHelper.GetConnection(_connectionString))
                {
                    conn.Open();
                    string sql = @"SELECT DISTINCT sp.MaSP, sp.TenSP, sp.DonViTinh, sp.GiaBan
                                   FROM SanPham sp
                                   INNER JOIN LoHang lh ON sp.MaSP = lh.MaSP
                                   WHERE sp.TrangThai = N'HoatDong' AND lh.SoLuongTon > 0
                                         AND lh.TrangThai NOT IN (N'Quá Hạn', N'Đã Hủy')
                                   ORDER BY sp.TenSP";
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
                                GiaBan = Convert.ToDecimal(reader["GiaBan"])
                            });
                        }
                    }
                }
            }
            catch { }
            return list;
        }
        //xem chi tiet hoa don
        public IActionResult ChiTiet(int id)
        {
            var model = new ChiTietPhieuXuatViewModel();

            try
            {
                using (var conn = DatabaseHelper.GetConnection(_connectionString))
                {
                    conn.Open();

                    // 1. Lấy thông tin phiếu xuất
                    string sqlPhieu = @"SELECT px.MaPhieuXuat, px.NgayXuat, px.LoaiPhieu, px.TongTien,
                                       nv.HoTen
                                FROM PhieuXuat px
                                INNER JOIN NhanVien nv ON px.MaNV = nv.MaNV
                                WHERE px.MaPhieuXuat = @Id";

                    using (var cmd = new SqlCommand(sqlPhieu, conn))
                    {
                        cmd.Parameters.AddWithValue("@Id", id);
                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                model.MaPhieuXuat = Convert.ToInt32(reader["MaPhieuXuat"]);
                                model.NgayXuat = Convert.ToDateTime(reader["NgayXuat"]);
                                model.LoaiPhieu = reader["LoaiPhieu"].ToString();
                                model.TongTien = Convert.ToDecimal(reader["TongTien"]);
                                model.TenNhanVien = reader["HoTen"].ToString();
                            }
                        }
                    }

                    // 2. Lấy chi tiết
                    string sqlChiTiet = @"SELECT ct.MaLo, sp.TenSP, ct.SoLuong, ct.DonGia
                                 FROM ChiTietXuat ct
                                 INNER JOIN LoHang lh ON ct.MaLo = lh.MaLo
                                 INNER JOIN SanPham sp ON lh.MaSP = sp.MaSP
                                 WHERE ct.MaPhieuXuat = @Id";

                    using (var cmd = new SqlCommand(sqlChiTiet, conn))
                    {
                        cmd.Parameters.AddWithValue("@Id", id);
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                model.DanhSachChiTiet.Add(new ChiTietItem
                                {
                                    MaLo = Convert.ToInt32(reader["MaLo"]),
                                    TenSanPham = reader["TenSP"].ToString(),
                                    SoLuong = Convert.ToDecimal(reader["SoLuong"]),
                                    DonGia = Convert.ToDecimal(reader["DonGia"])
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
            }

            return View(model);
        }
    }
}
