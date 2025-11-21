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
                // A. Xử lý khách hàng
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

                // B. Lấy ID User và Payment Method ID
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

                // Lấy PaymentMethodId từ request (được gửi lên là string CodeId)
                if (!int.TryParse(request.PaymentMethod, out int paymentMethodId))
                {
                    // Trường hợp lỗi: Gán một giá trị mặc định hoặc trả về lỗi
                    paymentMethodId = 1;
                }

                var order = new Order
                {
                    CustomerId = customer.CustomerId,
                    UserId = userId ?? "1",
                    OrderDate = DateTime.Now,
                    PromotionId = request.PromotionId == 0 ? null : request.PromotionId,
                    Status = "Completed",
                    PaymentMethodId = paymentMethodId, // <-- ĐÃ SỬA: Dùng ID từ client
                    TotalAmount = 0,
                    DiscountAmount = 0,
                    FinalAmount = 0
                };

                _context.Orders.Add(order);
                await _context.SaveChangesAsync();

                // C. Chi tiết đơn hàng
                decimal subTotal = 0;

                foreach (var item in request.CartItems)
                {
                    var book = await _context.Books.FindAsync(item.BookId);
                    if (book == null) throw new Exception($"Sách ID {item.BookId} không tồn tại");

                    if ((book.StockQuantity ?? 0) < item.Quantity)
                    {
                        throw new Exception($"Sách '{book.Title}' không đủ hàng (Còn: {book.StockQuantity})");
                    }

                    book.StockQuantity -= item.Quantity;

                    var detail = new OrderDetail
                    {
                        OrderId = order.OrderId,
                        BookId = item.BookId,
                        Quantity = item.Quantity,
                        UnitPrice = book.Price,
                        Subtotal = book.Price * item.Quantity
                    };
                    _context.OrderDetails.Add(detail);

                    // SỬA LỖI 2: Bỏ đoạn "?? 0m" vì Subtotal là kiểu decimal (không null)
                    subTotal += detail.Subtotal;
                }

                // D. Tính toán cuối cùng
                order.TotalAmount = subTotal;

                decimal discountVal = 0;
                if (order.PromotionId != null)
                {
                    var promo = await _context.Promotions.FindAsync(order.PromotionId);
                    if (promo != null)
                    {
                        // Lưu ý: Nếu DiscountPercent là nullable (decimal?) thì cần .Value hoặc ?? 0
                        discountVal = subTotal * (promo.DiscountPercent ?? 0m) / 100;
                    }
                }

                order.DiscountAmount = discountVal;
                order.FinalAmount = subTotal - discountVal;

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Json(new { success = true, message = "Thanh toán thành công!", orderId = order.OrderId });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return Json(new { success = false, message = "Lỗi: " + ex.Message });
            }
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