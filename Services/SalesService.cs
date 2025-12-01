using BookstoreManagement.Models;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace BookstoreManagement.Services
{
    public class SalesService
    {
        private readonly BookstoreContext _context;

        public SalesService(BookstoreContext context)
        {
            _context = context;
        }

        // Dùng Transaction để đảm bảo an toàn dữ liệu
        public async Task CreateSalesOrderAsync(Order order)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                decimal calculatedTotal = 0;

                // 1. TRỪ TỒN KHO VÀ TÍNH TOÁN LẠI
                foreach (var detail in order.OrderDetails)
                {
                    var book = await _context.Books.FindAsync(detail.BookId);
                    if (book == null)
                    {
                        throw new Exception($"Không tìm thấy sách với ID {detail.BookId}.");
                    }

                    // KIỂM TRA TỒN KHO
                    if ((book.StockQuantity ?? 0) < detail.Quantity)
                    {
                        throw new Exception($"Không đủ tồn kho cho sách '{book.Title}'. (Chỉ còn: {book.StockQuantity})");
                    }

                    // TRỪ TỒN KHO
                    book.StockQuantity -= detail.Quantity;
                    book.UpdatedAt = DateTime.Now;
                    _context.Books.Update(book);

                    // Tính toán (Không tin tưởng giá từ client, dùng giá từ DB)
                    detail.UnitPrice = book.Price;
                    detail.Subtotal = detail.Quantity * detail.UnitPrice;
                    calculatedTotal += detail.Subtotal;
                }

                // 2. CẬP NHẬT HÓA ĐƠN CHÍNH
                order.OrderDate = DateTime.Now;
                order.TotalAmount = calculatedTotal;
                // (Bạn có thể thêm logic khuyến mãi ở đây nếu muốn)
                order.FinalAmount = calculatedTotal - (order.DiscountAmount ?? 0);
                order.Status = "Completed"; // Đánh dấu hoàn thành
                order.CreatedAt = DateTime.Now;
                order.UpdatedAt = DateTime.Now;
                // order.UserId và order.CustomerId đã được gán từ Controller

                // 3. LƯU HÓA ĐƠN VÀ CHI TIẾT
                _context.Orders.Add(order);

                // 4. LƯU TẤT CẢ THAY ĐỔI (cả tồn kho và hóa đơn)
                await _context.SaveChangesAsync();

                // 5. COMMIT TRANSACTION
                await transaction.CommitAsync();
            }
            catch (Exception)
            {
                // 6. NẾU CÓ LỖI (ví dụ: hết hàng), ROLLBACK TẤT CẢ
                await transaction.RollbackAsync();
                throw; // Báo lỗi ra ngoài Controller
            }
        }
    }
}