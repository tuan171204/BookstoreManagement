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
                // A. Xử lý khách hàng

                string? customerId = null;

                // TRƯỜNG HỢP 1: Có nhập số điện thoại -> Tìm hoặc Tạo khách hàng cụ thể
                if (!string.IsNullOrEmpty(request.CustomerPhone))
                {
                    var customer = _context.Customers.FirstOrDefault(c => c.Phone == request.CustomerPhone);
                    if (customer == null)
                    {
                        // Tạo khách hàng mới (Khách lẻ)    
                        customer = new Customer
                        {
                            CustomerId = Guid.NewGuid().ToString(),
                            FullName = string.IsNullOrEmpty(request.CustomerName) ? "Khách mới" : request.CustomerName,
                            Phone = request.CustomerPhone,
                            Email = "guest@bookstore.com", // Email giả định nếu không có
                            CreatedAt = DateTime.Now,
                            IsActive = true
                        };
                        _context.Customers.Add(customer);
                        await _context.SaveChangesAsync();
                    }
                    customerId = customer.CustomerId; // Gán ID nếu tìm/tạo được
                }

                // TRƯỜNG HỢP 2: Không nhập SĐT -> Gán cho "Khách lẻ" (Walk-in Customer)
                else
                {
                    // Quy ước: SĐT 0000000000 là Khách lẻ
                    var guestCustomer = await _context.Customers.FirstOrDefaultAsync(c => c.Phone == "0000000000");

                    if (guestCustomer == null)
                    {
                        // Nếu chưa có trong DB thì tạo mới (chỉ chạy 1 lần đầu tiên)
                        guestCustomer = new Customer
                        {
                            CustomerId = Guid.NewGuid().ToString(),
                            FullName = "Khách lẻ",       // Tên hiển thị
                            Phone = "0000000000",        // SĐT quy ước
                            Email = "walkin@store.com",  // Email dummy
                            Address = "Tại quầy",
                            CreatedAt = DateTime.Now,
                            UpdatedAt = DateTime.Now,
                            IsActive = true              // Vẫn phải Active để bán hàng được
                        };
                        _context.Customers.Add(guestCustomer);
                        await _context.SaveChangesAsync();
                    }
                    customerId = guestCustomer.CustomerId;
                }

                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (!int.TryParse(request.PaymentMethod, out int paymentMethodId)) paymentMethodId = 1;

                // --- TẠO ORDER HEADER ---
                var order = new Order
                {
                    CustomerId = customerId,
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
                await _context.SaveChangesAsync();

                // --- TẠO PHIẾU XUẤT KHO ---
                var exportTicket = new ExportTicket
                {
                    UserId = userId ?? "1",
                    ReferenceId = order.OrderId,
                    Date = DateTime.Now,
                    Status = "Completed",
                    Reason = "Bán hàng (POS)",
                    DocumentNumber = $"PX{DateTimeOffset.Now.ToUnixTimeSeconds()}",
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now,
                    TotalQuantity = 0
                };
                _context.ExportTickets.Add(exportTicket);

                // C. Xử lý chi tiết 
                decimal subTotal = 0;
                int totalQty = 0;

                foreach (var item in request.CartItems)
                {
                    var book = await _context.Books.FindAsync(item.BookId);
                    if (book == null) throw new Exception($"Sách ID {item.BookId} không tồn tại");

                    if ((book.StockQuantity ?? 0) < item.Quantity)
                        throw new Exception($"Sách '{book.Title}' không đủ hàng (Còn: {book.StockQuantity})");

                    book.StockQuantity -= item.Quantity;

                    var orderDetail = new OrderDetail
                    {
                        OrderId = order.OrderId,
                        BookId = item.BookId,
                        Quantity = item.Quantity,
                        UnitPrice = book.Price,
                        Subtotal = book.Price * item.Quantity
                    };
                    _context.OrderDetails.Add(orderDetail);

                    var exportDetail = new ExportDetail
                    {
                        Export = exportTicket,
                        BookId = item.BookId,
                        Quantity = item.Quantity,
                        UnitPrice = book.Price,
                        Subtotal = book.Price * item.Quantity,
                        Note = "Xuất bán"
                    };
                    _context.ExportDetails.Add(exportDetail);

                    subTotal += orderDetail.Subtotal;
                    totalQty += item.Quantity;
                }

                // ========================================================
                // D. LOGIC KHUYẾN MÃI (ĐÃ SỬA LOGIC HẾT QUÀ -> TRỪ TIỀN)
                // ========================================================
                decimal discountVal = 0;

                if (order.PromotionId != null)
                {
                    var promo = await _context.Promotions.FindAsync(order.PromotionId);
                    var now = DateTime.Now;

                    if (promo != null && promo.IsActive == true &&
                       (promo.StartDate == null || promo.StartDate <= now) &&
                       (promo.EndDate == null || promo.EndDate >= now))
                    {
                        decimal minSpend = promo.MinPurchaseAmount ?? 0;
                        if (subTotal >= minSpend)
                        {
                            switch (promo.TypeId)
                            {
                                case 1: // PHẦN TRĂM (%)
                                    discountVal = subTotal * (promo.DiscountPercent ?? 0m) / 100;
                                    break;

                                case 2: // CỐ ĐỊNH (TIỀN MẶT)
                                    discountVal = promo.DiscountPercent ?? 0m;
                                    break;

                                case 3: // TẶNG SÁCH (GIFT BOOK)
                                    discountVal = 0;
                                    if (promo.GiftBookId != null)
                                    {
                                        var giftBook = await _context.Books.FindAsync(promo.GiftBookId);

                                        // 1. KIỂM TRA TỒN KHO SÁCH QUÀ
                                        if (giftBook != null && (giftBook.StockQuantity ?? 0) > 0)
                                        {
                                            // CÒN HÀNG: Tặng sách
                                            giftBook.StockQuantity -= 1;

                                            // Thêm vào đơn giá 0đ
                                            _context.OrderDetails.Add(new OrderDetail { OrderId = order.OrderId, BookId = giftBook.BookId, Quantity = 1, UnitPrice = 0, Subtotal = 0 });

                                            // Thêm vào phiếu xuất
                                            _context.ExportDetails.Add(new ExportDetail { Export = exportTicket, BookId = giftBook.BookId, Quantity = 1, UnitPrice = 0, Subtotal = 0, Note = "Hàng tặng" });

                                            totalQty += 1;
                                        }
                                        else if (giftBook != null)
                                        {
                                            // 2. HẾT HÀNG: Trừ tiền = Giá bán sách đó
                                            discountVal = giftBook.Price;
                                        }
                                    }
                                    break;
                            }
                        }
                    }
                }

                // Bảo vệ: Không giảm quá tổng tiền
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