---
trigger: always_on
---

1. Sử dụng kiến trúc MVC (Model–View–Controller) trên nền tảng ASP.NET Core.
2. Sử dụng hệ quản trị cơ sở dữ liệu SQL Server 2022.
3. Sử dụng ADO.NET (SqlConnection, SqlCommand) để truy xuất dữ liệu, không dùng Entity Framework.
4. Mọi truy vấn dữ liệu phải thông qua SqlParameter để chống SQL Injection.
5. Không sử dụng lệnh DELETE; thay vào đó cập nhật trạng thái thành "Đã Hủy".
6. Nếu hàng hết hạn, số ngày còn lại phải hiển thị bằng 0 (không cho phép giá trị âm).
7. Tự động phân loại trạng thái lô hàng: Quá hạn (Đỏ), Cận date <= 14 ngày (Cam), An toàn (Xanh).
8. Tự động tính giá sale cho hàng cận date theo % giảm giá của từng danh mục.
9. Mỗi sản phẩm bắt buộc phải có đơn vị tính (Bó, Kg, Hộp, Khay...).
10. Ràng buộc số lượng theo đơn vị: Bó/Hộp nhập số nguyên, Kg cho phép số thập phân.
11. Áp dụng thuật toán xuất kho FEFO (hết hạn trước xuất trước) bằng ORDER BY HanSuDung ASC.
12. Phân loại rõ phiếu xuất: "Bán Hàng" và "Hủy Hàng".
13. Doanh thu chỉ tính từ phiếu "Bán Hàng"; phiếu "Hủy Hàng" tính vào thất thoát.
14. Sử dụng AJAX và jQuery để quét mã vạch, cập nhật dữ liệu ngay không reload trang.
15. Thiết kế thanh menu dạng Navbar cố định gồm: Trang chủ, Quản lý sản phẩm, Báo cáo, Tài khoản.
16. Xây dựng chức năng đăng nhập và đăng ký cho nhân viên kho.
17. Kiểm tra logic nhập kho: Hạn sử dụng ≥ Ngày sản xuất.
18. Thiết kế giao diện bằng Bootstrap 5 và FontAwesome 6.
19. Hiển thị thông báo bằng TempData dưới dạng Alert (Toast).
20. Controller phải sử dụng using và try-catch để xử lý lỗi và tránh rò rỉ bộ nhớ.
21. luôn kiểm tra hệ thống theo chu ISO/IEC 25010 và OWASP Top 10