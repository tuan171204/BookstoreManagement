using BookstoreManagement.Models;
using Microsoft.EntityFrameworkCore; // Cần cho Transaction
using System.Threading.Tasks;

namespace BookstoreManagement.Services
{
    public class ImportService
    {
        private readonly BookstoreContext _context;

        public ImportService(BookstoreContext context)
        {
            _context = context;
        }

        public async Task CreateImportTicketAsync(ImportTicket ticket)
        {
            // Bắt đầu một Transaction (giao dịch)
            // Nếu một trong các bước bị lỗi, tất cả sẽ được rollback (hủy bỏ)
            using var transaction = await _context.Database.BeginTransactionAsync();
            
            try
            {
                // 1. Tính toán lại tổng tiền và số lượng
                decimal totalCost = 0;
                int totalQty = 0;

                foreach (var detail in ticket.ImportDetails)
                {
                    // Tính thành tiền cho từng dòng (dựa trên Model ImportDetail.cs)
                    detail.Subtotal = detail.Quantity * detail.CostPrice * (1 - (detail.Discount ?? 0) / 100);
                    totalCost += detail.Subtotal ?? 0;
                    totalQty += detail.Quantity;
                }

                // 2. Cập nhật thông tin cho phiếu nhập chính (Master)
                ticket.TotalCost = totalCost;
                ticket.TotalQuantity = totalQty;
                ticket.Status = "Completed"; // Đánh dấu là đã hoàn thành
                ticket.CreatedAt = DateTime.Now;
                ticket.UpdatedAt = DateTime.Now;
                // ticket.UserId đã được gán từ Controller

                // 3. Lưu phiếu nhập và chi tiết (Master-Detail) vào DB
                _context.ImportTickets.Add(ticket);
                await _context.SaveChangesAsync(); 
                // Khi lưu xong, 'ticket' sẽ tự động có ImportId mới
                // và các 'detail' cũng tự động có ImportId

                // 4. CẬP NHẬT TỒN KHO (Rất quan trọng)
                foreach (var detail in ticket.ImportDetails)
                {
                    // Tìm sách tương ứng
                    var book = await _context.Books.FindAsync(detail.BookId);
                    if (book != null)
                    {
                        // Cộng dồn vào số lượng tồn kho hiện tại
                        book.StockQuantity = (book.StockQuantity ?? 0) + detail.Quantity;
                        book.UpdatedAt = DateTime.Now;
                        _context.Books.Update(book);
                    }
                }
                await _context.SaveChangesAsync(); // Lưu thay đổi tồn kho

                // 5. Nếu mọi thứ thành công, commit transaction
                await transaction.CommitAsync();
            }
            catch (Exception)
            {
                // 6. Nếu có lỗi ở bất kỳ đâu, rollback tất cả
                await transaction.RollbackAsync();
                throw; // Báo lỗi ra ngoài để Controller xử lý
            }
        }
    }
}