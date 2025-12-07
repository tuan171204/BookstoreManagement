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

        // ============================================================
        // 1. CÁC ACTION TRẢ VỀ GIAO DIỆN
        // ============================================================
        [HttpGet]
        public IActionResult Index()
        {
            ViewBag.Categories = _context.Categories.OrderBy(c => c.Name).ToList();

            ViewBag.Employees = _context.Users
                .Where(u => u.IsActive == true) // Chỉ lấy nhân viên đang hoạt động
                .Select(u => new { u.Id, u.FullName })
                .OrderBy(x => x.FullName)
                .ToList();

            var initialBooks = _context.Books
                .Where(b => b.IsDeleted != true)
                .OrderByDescending(b => b.CreatedAt)
                .Take(20)
                .Select(b => new
                {
                    // SỬA: Dùng chữ thường cho đồng bộ JS
                    id = b.BookId,
                    title = b.Title,
                    price = b.Price,
                    stock = b.StockQuantity ?? 0,
                    imageUrl = b.ImageUrl,
                    //isbn = b.SKU 
                }).ToList();

            ViewBag.InitialBooksJson = System.Text.Json.JsonSerializer.Serialize(initialBooks);
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

        // ============================================================
        // 2. API TÌM KIẾM & DỮ LIỆU HỖ TRỢ
        // ============================================================
        [HttpGet]
        public IActionResult SearchBooks(string term, int? categoryId)
        {
            var query = _context.Books.AsQueryable();

            if (!string.IsNullOrEmpty(term))
                query = query.Where(b => b.Title.Contains(term));

            if (categoryId.HasValue && categoryId > 0)
            {
                var bookIdsInCat = _context.BookCategories
                   .Where(bc => bc.CategoryId == categoryId)
                   .Select(bc => bc.BookId);
                query = query.Where(b => bookIdsInCat.Contains(b.BookId));
            }

            var books = query
                .Where(b => b.IsDeleted != true && b.StockQuantity > 0)
                .Select(b => new
                {
                    id = b.BookId,
                    title = b.Title,
                    price = b.Price,
                    stock = b.StockQuantity ?? 0,
                    //isbn = b.SKU ?? "",
                    imageUrl = b.ImageUrl
                })
                .Take(10).ToList();

            return Json(books);
        }

        // --- API LẤY KHUYẾN MÃI (ĐÃ CẬP NHẬT LẤY GIÁ SÁCH TẶNG) ---
        [HttpGet]
        public IActionResult GetActivePromotions()
        {
            var today = DateTime.Now;
            var promos = _context.Promotions
                .Include(p => p.GiftBook)
                .Where(p => p.IsActive == true
                        && (p.StartDate == null || p.StartDate <= today)
                        && (p.EndDate == null || p.EndDate >= today))
                .Select(p => new
                {
                    id = p.PromotionId,
                    name = p.Name,
                    typeId = p.TypeId,
                    value = p.DiscountPercent ?? 0,
                    min = p.MinPurchaseAmount ?? 0,
                    giftName = p.GiftBook != null ? p.GiftBook.Title : "",
                    giftStock = p.GiftBook != null ? (p.GiftBook.StockQuantity ?? 0) : 0,
                    // THÊM: Lấy giá bán của sách tặng
                    giftPrice = p.GiftBook != null ? p.GiftBook.Price : 0
                }).ToList();

            return Json(promos);
        }



        [HttpGet]
        public IActionResult GetPaymentMethods()
        {
            var paymentMethods = _context.Codes
                .Where(c => c.Entity == "PaymentMethod")
                .Select(c => new { id = c.CodeId, name = c.Value }).ToList();
            return Json(paymentMethods);
        }

        [HttpGet]
        public IActionResult GetCustomerInfo(string phone)
        {
            if (string.IsNullOrEmpty(phone)) return Json(new { success = false });

            var customer = _context.Customers
                .Where(c => c.Phone == phone && c.IsActive == true)
                .Select(c => new { name = c.FullName, phone = c.Phone })
                .FirstOrDefault();

            if (customer != null) return Json(new { success = true, data = customer });
            return Json(new { success = false });
        }

        [HttpGet]
        public IActionResult SearchCustomers(string keyword)
        {
            var customers = _context.Customers
                .Where(c => c.Phone.Contains(keyword))
                .Select(c => c.Phone)
                .Take(5).ToList();
            return Json(customers);
        }

        // ============================================================
        // 3. API THANH TOÁN (CHECKOUT)
        // ============================================================
        [HttpPost]
        public async Task<IActionResult> Checkout([FromBody] CheckoutRequest request)
        {
            if (request == null || request.CartItems == null || !request.CartItems.Any())
                return Json(new { success = false, message = "Giỏ hàng trống!" });

            using var transaction = _context.Database.BeginTransaction();
            try
            {
                // A. XỬ LÝ KHÁCH HÀNG
                Customer customer;
                string phone = string.IsNullOrWhiteSpace(request.CustomerPhone) ? "00000000" : request.CustomerPhone.Trim();
                string name = string.IsNullOrWhiteSpace(request.CustomerName) ? "khách lẻ" : request.CustomerName.Trim();

                customer = await _context.Customers.FirstOrDefaultAsync(c => c.Phone == phone);

                if (customer == null)
                {
                    if (phone == "00000000")
                    {
                        customer = new Customer { FullName = "Khách lẻ", Phone = "00000000", Email = "customer@gmail.com", Address = "tại quầy", IsActive = true, CreatedAt = DateTime.Now };
                    }
                    else
                    {
                        customer = new Customer { FullName = name, Phone = phone, CreatedAt = DateTime.Now, IsActive = true };
                    }
                    _context.Customers.Add(customer);
                    await _context.SaveChangesAsync();
                }
                else
                {
                    if (phone != "00000000" && !string.IsNullOrEmpty(name) && customer.FullName != name)
                    {
                        customer.FullName = name;
                        _context.Customers.Update(customer);
                        await _context.SaveChangesAsync();
                    }
                }

                // --- B. XỬ LÝ NHÂN VIÊN BÁN HÀNG (MỚI) ---
                // Ưu tiên lấy ID nhân viên từ Dropdown
                string userId = request.EmployeeId;

                // Nếu không chọn (hoặc null), fallback về người đang đăng nhập
                if (string.IsNullOrEmpty(userId))
                {
                    userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                }
                // Nếu vẫn null (lỗi session), lấy đại admin đầu tiên để không lỗi DB
                if (string.IsNullOrEmpty(userId))
                {
                    userId = await _context.Users.Select(u => u.Id).FirstOrDefaultAsync();
                }
                // -----------------------------------------

                int paymentMethodId = (request.PaymentMethod == "Transfer") ? 2 : 1;

                // C. TẠO ORDER
                var order = new Order
                {
                    CustomerId = customer.CustomerId,
                    UserId = userId, // Lưu người bán
                    OrderDate = DateTime.Now,
                    PromotionId = request.PromotionId == 0 ? null : request.PromotionId,
                    Status = "Completed",
                    PaymentMethodId = paymentMethodId,
                    TotalAmount = 0,
                    DiscountAmount = 0,
                    FinalAmount = 0,
                    CreatedAt = DateTime.Now
                };
                _context.Orders.Add(order);
                await _context.SaveChangesAsync();

                // D. TẠO PHIẾU XUẤT KHO
                var exportTicket = new ExportTicket
                {
                    UserId = userId, // Lưu người xuất kho
                    ReferenceId = order.OrderId,
                    Date = DateTime.Now,
                    Status = "Completed",
                    Reason = "Bán hàng (POS)",
                    DocumentNumber = $"PX{DateTimeOffset.Now.ToUnixTimeSeconds()}",
                    CreatedAt = DateTime.Now,
                    TotalQuantity = 0
                };
                _context.ExportTickets.Add(exportTicket);

                // E. CHI TIẾT
                decimal subTotal = 0;
                int totalQty = 0;

                foreach (var item in request.CartItems)
                {
                    var book = await _context.Books.FindAsync(item.BookId);
                    if (book == null) throw new Exception($"Sách ID {item.BookId} không tồn tại");
                    if ((book.StockQuantity ?? 0) < item.Quantity) throw new Exception($"Sách '{book.Title}' không đủ hàng");

                    book.StockQuantity -= item.Quantity;

                    var orderDetail = new OrderDetail { OrderId = order.OrderId, BookId = item.BookId, Quantity = item.Quantity, UnitPrice = book.Price, Subtotal = book.Price * item.Quantity };
                    _context.OrderDetails.Add(orderDetail);

                    var exportDetail = new ExportDetail { Export = exportTicket, BookId = item.BookId, Quantity = item.Quantity, UnitPrice = book.Price, Subtotal = book.Price * item.Quantity, Note = "Xuất bán" };
                    _context.ExportDetails.Add(exportDetail);

                    subTotal += orderDetail.Subtotal;
                    totalQty += item.Quantity;
                }

                // F. KHUYẾN MÃI
                decimal discountVal = 0;
                if (order.PromotionId != null)
                {
                    var promo = await _context.Promotions.FindAsync(order.PromotionId);
                    var now = DateTime.Now;
                    if (promo != null && promo.IsActive == true && (promo.StartDate == null || promo.StartDate <= now) && (promo.EndDate == null || promo.EndDate >= now))
                    {
                        if (subTotal >= (promo.MinPurchaseAmount ?? 0))
                        {
                            switch (promo.TypeId)
                            {
                                case 1: discountVal = subTotal * (promo.DiscountPercent ?? 0m) / 100; break;
                                case 2: discountVal = promo.DiscountPercent ?? 0m; break;
                                case 3:
                                    discountVal = 0;
                                    if (promo.GiftBookId != null)
                                    {
                                        var giftBook = await _context.Books.FindAsync(promo.GiftBookId);
                                        if (giftBook != null && (giftBook.StockQuantity ?? 0) > 0)
                                        {
                                            giftBook.StockQuantity -= 1;
                                            _context.OrderDetails.Add(new OrderDetail { OrderId = order.OrderId, BookId = giftBook.BookId, Quantity = 1, UnitPrice = 0, Subtotal = 0 });
                                            _context.ExportDetails.Add(new ExportDetail { Export = exportTicket, BookId = giftBook.BookId, Quantity = 1, UnitPrice = 0, Subtotal = 0, Note = "Hàng tặng" });
                                            totalQty += 1;
                                        }
                                        else if (giftBook != null) { discountVal = giftBook.Price; }
                                    }
                                    break;
                            }
                        }
                    }
                }

                if (discountVal > subTotal) discountVal = subTotal;
                order.TotalAmount = subTotal;
                order.DiscountAmount = discountVal;
                order.FinalAmount = subTotal - discountVal;
                exportTicket.TotalQuantity = totalQty;

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

        // --- Class DTO ---
        public class CheckoutRequest
        {
            public string? CustomerPhone { get; set; }
            public string? CustomerName { get; set; }
            public string? EmployeeId { get; set; } // <--- THÊM TRƯỜNG NÀY
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