using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BookstoreManagement.Models;
using System.Security.Claims;

namespace BookstoreManagement.Controllers
{
    public class SalesController : Controller
    {
        private readonly BookstoreContext _context;

        public SalesController(BookstoreContext context)
        {
            _context = context;
        }

        // 1. CÁC ACTION TRẢ VỀ GIAO DIỆN
        [HttpGet]
        [Authorize]
        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        [Authorize]
        public IActionResult List()
        {
            var orders = _context.Orders
                .Include(o => o.Customer)
                .Include(o => o.User)
                .OrderByDescending(o => o.OrderDate)
                .ToList();
            return View(orders);
        }

        // 2. API TÌM KIẾM & KHUYẾN MÃI
        [HttpGet]
        public IActionResult SearchBooks(string term)
        {
            if (string.IsNullOrEmpty(term)) return Json(new List<object>());

            var books = _context.Books
                .Where(b => b.Title.Contains(term) && b.IsDeleted != true && b.StockQuantity > 0)
                .Select(b => new
                {
                    id = b.BookId,
                    title = b.Title,
                    price = b.Price,
                    stock = b.StockQuantity ?? 0,
                    isbn = b.BookId.ToString()
                })
                .Take(10).ToList();

            return Json(books);
        }

        [HttpGet]
        public IActionResult GetActivePromotions()
        {
            var today = DateTime.Now;
            var promos = _context.Promotions
                .Where(p => p.IsActive == true
                         && (p.StartDate == null || p.StartDate <= today)
                         && (p.EndDate == null || p.EndDate >= today))
                .Select(p => new
                {
                    id = p.PromotionId,
                    name = p.Name,
                    discount = p.DiscountPercent ?? 0
                }).ToList();

            return Json(promos);
        }

        [HttpGet]
        public IActionResult GetPaymentMethods()
        {
            var paymentMethods = _context.Codes
                .Where(c => c.Entity == "PaymentMethod")
                .Select(c => new
                {
                    id = c.CodeId,
                    name = c.Value
                })
                .ToList();

            return Json(paymentMethods);
        }

        // 3. API THANH TOÁN (ĐÃ SỬA LỖI Ở ĐÂY)
        // ... (Giữ nguyên các phần trên)

        // 3. API THANH TOÁN (ĐÃ CẬP NHẬT LOGIC TẠO PHIẾU XUẤT)
        [HttpPost]
        public async Task<IActionResult> Checkout([FromBody] CheckoutRequest request)
        {
            if (request == null || request.CartItems == null || !request.CartItems.Any())
            {
                return Json(new { success = false, message = "Giỏ hàng trống!" });
            }

            using var transaction = _context.Database.BeginTransaction();
            try
            {
                // A. Xử lý khách hàng (Giữ nguyên)
                var customer = _context.Customers.FirstOrDefault(c => c.Phone == request.CustomerPhone);
                if (customer == null)
                {
                    customer = new Customer
                    {
                        FullName = string.IsNullOrEmpty(request.CustomerName) ? "Khách lẻ" : request.CustomerName,
                        Phone = request.CustomerPhone ?? "0000000000",
                        CreatedAt = DateTime.Now,
                        IsActive = true
                    };
                    _context.Customers.Add(customer);
                    await _context.SaveChangesAsync();
                }

                // B. Lấy ID User và Payment Method
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (!int.TryParse(request.PaymentMethod, out int paymentMethodId)) paymentMethodId = 1;

                // --- 1. TẠO HÓA ĐƠN (ORDER) ---
                var order = new Order
                {
                    CustomerId = customer.CustomerId,
                    UserId = userId ?? "1",
                    OrderDate = DateTime.Now,
                    PromotionId = request.PromotionId == 0 ? null : request.PromotionId,
                    Status = "Completed",
                    PaymentMethodId = paymentMethodId,
                    TotalAmount = 0,
                    DiscountAmount = 0,
                    FinalAmount = 0
                };
                _context.Orders.Add(order);

                // Lưu Order trước để có OrderId dùng cho ExportTicket (nếu cần tham chiếu ID cứng)
                await _context.SaveChangesAsync();

                // --- 2. TẠO PHIẾU XUẤT KHO (EXPORT TICKET) ---
                // Đây là phần bổ sung để quản lý quá trình xuất hàng
                var exportTicket = new ExportTicket
                {
                    UserId = userId ?? "1",          // Người bán cũng là người xuất kho
                    ReferenceId = order.OrderId,     // Tham chiếu đến đơn hàng vừa tạo
                    Date = DateTime.Now,
                    Status = "Completed",            // Xuất kho thành công ngay lập tức
                    Reason = "Bán hàng (POS)",       // Lý do xuất
                    DocumentNumber = $"PX{DateTimeOffset.Now.ToUnixTimeSeconds()}", // Mã phiếu tự sinh: PX...
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now,
                    TotalQuantity = 0 // Sẽ cộng dồn trong vòng lặp
                };
                _context.ExportTickets.Add(exportTicket);

                // C. Xử lý chi tiết
                decimal subTotal = 0;
                int totalQty = 0;

                foreach (var item in request.CartItems)
                {
                    var book = await _context.Books.FindAsync(item.BookId);
                    if (book == null) throw new Exception($"Sách ID {item.BookId} không tồn tại");

                    // --- 3. TRỪ TỒN KHO (QUẢN LÝ KHO) ---
                    if ((book.StockQuantity ?? 0) < item.Quantity)
                    {
                        throw new Exception($"Sách '{book.Title}' không đủ hàng (Còn: {book.StockQuantity})");
                    }
                    book.StockQuantity -= item.Quantity; // Trừ trực tiếp

                    // Thêm chi tiết Hóa đơn (Order Detail)
                    var orderDetail = new OrderDetail
                    {
                        OrderId = order.OrderId,
                        BookId = item.BookId,
                        Quantity = item.Quantity,
                        UnitPrice = book.Price,
                        Subtotal = book.Price * item.Quantity
                    };
                    _context.OrderDetails.Add(orderDetail);

                    // Thêm chi tiết Phiếu xuất (Export Detail) - Để lưu lịch sử kho
                    var exportDetail = new ExportDetail
                    {
                        ExportId = exportTicket.ExportId, // EF Core sẽ tự gán ID sau khi SaveChanges nếu dùng Add vào Collection, nhưng gán thủ công cũng được nếu ExportTicket chưa save
                        Export = exportTicket, // Gán object để EF tự hiểu
                        BookId = item.BookId,
                        Quantity = item.Quantity,
                        UnitPrice = book.Price,
                        Subtotal = book.Price * item.Quantity,
                        Note = "Xuất bán tại quầy"
                    };
                    _context.ExportDetails.Add(exportDetail);

                    subTotal += orderDetail.Subtotal;
                    totalQty += item.Quantity;
                }

                // D. Cập nhật các con số tổng
                // Cập nhật Order
                order.TotalAmount = subTotal;
                decimal discountVal = 0;
                if (order.PromotionId != null)
                {
                    var promo = await _context.Promotions.FindAsync(order.PromotionId);
                    if (promo != null)
                    {
                        discountVal = subTotal * (promo.DiscountPercent ?? 0m) / 100;
                    }
                }
                order.DiscountAmount = discountVal;
                order.FinalAmount = subTotal - discountVal;

                // Cập nhật ExportTicket
                exportTicket.TotalQuantity = totalQty;

                // Lưu tất cả thay đổi (Update Order, Insert ExportTicket, Insert Details, Update Books)
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Json(new { success = true, message = "Thanh toán và xuất kho thành công!", orderId = order.OrderId });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return Json(new { success = false, message = "Lỗi: " + ex.Message });
            }
        }

        // Class DTO
        public class CheckoutRequest
        {
            public string? CustomerPhone { get; set; }
            public string? CustomerName { get; set; }
            public int PromotionId { get; set; }
            public string? PaymentMethod { get; set; }
            public List<CartItemRequest>? CartItems { get; set; }
        }

        public class CartItemRequest
        {
            public int BookId { get; set; }
            public string? Title { get; set; }
            public int Quantity { get; set; }
        }
    }
}