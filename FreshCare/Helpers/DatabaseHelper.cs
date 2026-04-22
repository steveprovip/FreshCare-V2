using Microsoft.Data.SqlClient;
using System.Security.Cryptography;
using System.Text;

namespace FreshCare.Helpers
{
    /// <summary>
    /// Helper class cho ADO.NET - Luật #3
    /// Cung cấp SqlConnection từ connection string
    /// </summary>
    public static class DatabaseHelper
    {
        /// <summary>
        /// Tạo SqlConnection mới từ connection string
        /// Luật #20: sử dụng using để tránh rò rỉ bộ nhớ
        /// </summary>
        public static SqlConnection GetConnection(string connectionString)
        {
            return new SqlConnection(connectionString);
        }

        /// <summary>
        /// Mã hóa mật khẩu SHA256 - Luật NFR4: hash 1 chiều
        /// </summary>
        public static string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                var sb = new StringBuilder();
                foreach (var b in bytes)
                    sb.Append(b.ToString("x2"));
                return sb.ToString();
            }
        }

        /// <summary>
        /// Luật #7: Tự động phân loại trạng thái lô hàng dựa trên HSD
        /// Quá hạn (Đỏ), Cận date < 14 ngày (Cam), An toàn (Xanh)
        /// </summary>
        public static string PhanLoaiTrangThai(DateTime hanSuDung)
        {
            int soNgay = (hanSuDung.Date - DateTime.Now.Date).Days;
            if (soNgay < 0)
                return "Quá Hạn";
            if (soNgay <= 14)
                return "Cận Date";
            return "An Toàn";
        }
    }
}
