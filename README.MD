# FreshCare - Hệ thống Quản lý Kho Nông Sản Sạch

**FreshCare** là giải pháp quản lý kho chuyên dụng cho các cửa hàng, chuỗi cung ứng thực phẩm sạch và nông sản. Hệ thống được thiết kế để tối ưu hóa việc theo dõi hạn sử dụng, giảm thiểu thất thoát và quản lý xuất nhập kho hiệu quả.

## 🚀 Công nghệ sử dụng
- **Backend:** ASP.NET Core 10 (MVC Pattern)
- **Database:** SQL Server 2022
- **Data Access:** ADO.NET (SqlParameter, No Entity Framework)
- **Frontend:** Bootstrap 5, FontAwesome 6, jQuery & AJAX
- **Thuật toán:** FEFO (First Expired, First Out) - Hàng hết hạn trước xuất trước.

## 📋 Tính năng chính
1.  **Quản lý sản phẩm & Lô hàng:** Theo dõi chi tiết Ngày sản xuất (NSX) và Hạn sử dụng (HSD).
2.  **Hệ thống cảnh báo thông minh:** 
    - Tự động phân loại: Quá hạn (Đỏ), Cận date < 30 ngày (Cam), An toàn (Xanh).
    - Tự động tính giá sale cho hàng cận date.
3.  **Quản lý Xuất/Nhập:** 
    - Phân loại rõ phiếu xuất: Bán hàng vs Hủy hàng.
    - Áp dụng FEFO tự động khi xuất kho.
4.  **Báo cáo & Thống kê:** Doanh thu từ bán hàng và báo cáo thất thoát từ hàng hủy.

## 🛠 Hướng dẫn cài đặt
1. **Database:** Chạy script tại `FreshCare/Database/FreshCareDB.sql` trong SQL Server 2022.
2. **Cấu hình:** Cập nhật chuỗi kết nối (Connection String) trong `FreshCare/appsettings.json`.
3. **Chạy ứng dụng:** 
   ```bash
   cd FreshCare
   dotnet run
   ```

## 📜 Quy định Dự án (Coding Standard)
- Luôn sử dụng `SqlParameter` để chống SQL Injection.
- Không sử dụng lệnh `DELETE`, chỉ cập nhật trạng thái "Đã Hủy".
- Logic xuất kho dựa trên `ORDER BY HanSuDung ASC`.