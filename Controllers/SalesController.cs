using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BookstoreManagement.Models;
using System.Security.Claims;
using Microsoft.CodeAnalysis.Elfie.Diagnostics;

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
        public IActionResult Index()
        {
            ViewBag.Categories = _context.Categories.OrderBy(c => c.Name).ToList();

            ViewBag.Employees = _context.Users
                .Where(u => u.IsActive == true)
                .Select(u => new { u.Id, u.FullName })
                .OrderBy(x => x.FullName)
                .ToList();

            var initialBooks = _context.Books
                .Where(b => b.IsDeleted != true)
                .OrderByDescending(b => b.CreatedAt)
                .Take(20)
                .Select(b => new
                {
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
        public async Task<IActionResult> List(
            string searchString, 
            string? status, 
            string? employeeId,
            DateTime? fromDate,
            DateTime? toDate,
            string sortBy = "OrderDate",
            string sortOrder = "desc",
            int pageNumber = 1, 
            int pageSize = 10)
        {
            ViewData["CurrentFilter"] = searchString;
            ViewData["StatusFilter"] = status;
            ViewData["EmployeeFilter"] = employeeId;
            ViewData["FromDateFilter"] = fromDate?.ToString("yyyy-MM-dd");
            ViewData["ToDateFilter"] = toDate?.ToString("yyyy-MM-dd");
            ViewData["SortBy"] = sortBy;
            ViewData["SortOrder"] = sortOrder;
            ViewData["IsSalesPage"] = "true"; // Flag để pagination biết dùng "status" thay vì "statusFilter"

            var query = _context.Orders
                .Include(o => o.Customer)
                .Include(o => o.User)
                .AsQueryable();

            // Search filter
            if (!string.IsNullOrEmpty(searchString))
            {
                query = query.Where(o => 
                    o.Customer.FullName.Contains(searchString) ||
                    o.Customer.Phone.Contains(searchString) ||
                    o.User.FullName.Contains(searchString) ||
                    o.OrderId.ToString().Contains(searchString));
            }

            // Status filter
            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(o => o.Status == status);
            }

            // Employee filter
            if (!string.IsNullOrEmpty(employeeId))
            {
                query = query.Where(o => o.UserId == employeeId);
            }

            // Date range filter
            if (fromDate.HasValue)
            {
                query = query.Where(o => o.OrderDate >= fromDate.Value);
            }

            if (toDate.HasValue)
            {
                var endDate = toDate.Value.Date.AddDays(1).AddSeconds(-1);
                query = query.Where(o => o.OrderDate <= endDate);
            }

            // Apply sorting
            query = sortBy?.ToLower() switch
            {
                "orderid" => sortOrder == "asc" 
                    ? query.OrderBy(o => o.OrderId) 
                    : query.OrderByDescending(o => o.OrderId),
                "orderdate" => sortOrder == "asc" 
                    ? query.OrderBy(o => o.OrderDate) 
                    : query.OrderByDescending(o => o.OrderDate),
                "customer" => sortOrder == "asc" 
                    ? query.OrderBy(o => o.Customer.FullName) 
                    : query.OrderByDescending(o => o.Customer.FullName),
                "user" => sortOrder == "asc" 
                    ? query.OrderBy(o => o.User.FullName) 
                    : query.OrderByDescending(o => o.User.FullName),
                "finalamount" => sortOrder == "asc" 
                    ? query.OrderBy(o => o.FinalAmount) 
                    : query.OrderByDescending(o => o.FinalAmount),
                "status" => sortOrder == "asc" 
                    ? query.OrderBy(o => o.Status) 
                    : query.OrderByDescending(o => o.Status),
                _ => query.OrderByDescending(o => o.OrderDate)
            };

            // Calculate pagination
            var totalItems = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            // Apply pagination
            var orders = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.PageNumber = pageNumber;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalItems = totalItems;

            // Load dropdowns for filters
            ViewBag.Employees = _context.Users
                .Where(u => u.IsActive == true)
                .Select(u => new { u.Id, u.FullName })
                .OrderBy(x => x.FullName)
                .ToList();

            ViewBag.Statuses = _context.Orders
                .Select(o => o.Status)
                .Distinct()
                .OrderBy(s => s)
                .ToList();

            return View(orders);
        }

        // 2. API TÌM KIẾM & DỮ LIỆU HỖ TRỢ
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
                    giftPrice = p.GiftBook != null ? p.GiftBook.Price : 0
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
                    name = c.Value,
                    code = c.Key // Thêm cái này để JS nhận biết (Ví dụ: 1=Cash, 2=Transfer)
                }).ToList();
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

        // 3. API THANH TOÁN (CHECKOUT)
        [HttpPost]
        public async Task<IActionResult> Checkout([FromBody] CheckoutRequest request)
        {
            if (request == null || request.CartItems == null || !request.CartItems.Any())
                return Json(new { success = false, message = "Giỏ hàng trống!" });

            using var transaction = _context.Database.BeginTransaction();
            try
            {
                // XỬ LÝ KHÁCH HÀNG
                Customer customer;
                string phone = string.IsNullOrWhiteSpace(request.CustomerPhone) ? "00000000" : request.CustomerPhone.Trim();
                string name = string.IsNullOrWhiteSpace(request.CustomerName) ? "khách lẻ" : request.CustomerName.Trim();

                customer = await _context.Customers.FirstOrDefaultAsync(c => c.Phone == phone);

                if (customer == null)
                {
                    if (phone == "00000000")
                    {
                        customer = new Customer 
                        { 
                            CustomerId = Guid.NewGuid().ToString(),
                            FullName = "Khách lẻ", 
                            Phone = "00000000", 
                            Email = "customer@gmail.com", 
                            Address = "tại quầy", 
                            IsActive = true, 
                            CreatedAt = DateTime.Now 
                        };
                    }
                    else
                    {
                        customer = new Customer 
                        { 
                            CustomerId = Guid.NewGuid().ToString(),
                            FullName = name, 
                            Phone = phone, 
                            CreatedAt = DateTime.Now, 
                            IsActive = true 
                        };
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

                // B. XỬ LÝ NHÂN VIÊN BÁN HÀNG (MỚI)
                string userId = request.EmployeeId;

                // Nếu không chọn (hoặc null), fallback về user đang đăng nhập
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

                int paymentMethodId;

                // Cố gắng parse ID từ request (Frontend sẽ gửi số "5", "6"...)
                if (int.TryParse(request.PaymentMethod, out int pmId))
                {
                    // Kiểm tra xem ID này có thật trong DB không
                    var exists = await _context.Codes.AnyAsync(c => c.CodeId == pmId);
                    if (!exists) throw new Exception("Phương thức thanh toán không hợp lệ.");
                    paymentMethodId = pmId;
                }
                else
                {
                    throw new Exception("Dữ liệu phương thức thanh toán bị lỗi.");
                }

                // TẠO ORDER
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

                // TẠO PHIẾU XUẤT KHO
                var exportTicket = new ExportTicket
                {
                    UserId = userId,
                    ReferenceId = order.OrderId,
                    Date = DateTime.Now,
                    Status = "Completed",
                    Reason = "Bán hàng (POS)",
                    DocumentNumber = $"PX{DateTimeOffset.Now.ToUnixTimeSeconds()}",
                    CreatedAt = DateTime.Now,
                    TotalQuantity = 0
                };
                _context.ExportTickets.Add(exportTicket);

                // CHI TIẾT
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

                // KHUYẾN MÃI
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


                // --- LOGIC TÍCH ĐIỂM & THĂNG HẠNG (MỚI) ---
                // Chỉ tích điểm nếu không phải khách vãng lai (Phone != 00000000)
                if (customer.Phone != "00000000")
                {
                    // 1. Tính điểm: 10% giá trị từng cuốn sách (Dựa theo VD: 50k -> 5k điểm)
                    // Lưu ý: Tính trên giá gốc của sách trong đơn hàng (UnitPrice), bỏ qua Discount của Order
                    int earnedPoints = 0;
                    foreach (var item in request.CartItems)
                    {
                        var bookPrice = _context.OrderDetails
                                        .Where(od => od.OrderId == order.OrderId && od.BookId == item.BookId)
                                        .Select(od => od.UnitPrice)
                                        .FirstOrDefault();

                        // Công thức: Giá * Số lượng * 10%
                        earnedPoints += (int)((bookPrice * item.Quantity) * 0.1m);
                    }

                    // 2. Cộng điểm vào Customer
                    customer.Points += earnedPoints;

                    // 3. Kiểm tra và Cập nhật Hạng (Rank)
                    // Lấy danh sách Rank từ DB (Code) để lấy ID chính xác
                    var ranks = await _context.Codes.Where(c => c.Entity == "MemberRank").ToListAsync();
                    var silverRank = ranks.FirstOrDefault(r => r.Value == "Bạc")?.CodeId;
                    var goldRank = ranks.FirstOrDefault(r => r.Value == "Vàng")?.CodeId;
                    var diamondRank = ranks.FirstOrDefault(r => r.Value == "Kim Cương")?.CodeId;

                    // Logic xét hạng (Lấy hạng cao nhất thỏa mãn)
                    if (customer.Points >= 200000 && diamondRank.HasValue)
                    {
                        customer.RankId = diamondRank.Value;
                    }
                    else if (customer.Points >= 100000 && goldRank.HasValue)
                    {
                        // Chỉ update nếu chưa phải Kim Cương (tránh hạ cấp nếu logic phức tạp hơn)
                        if (customer.RankId != diamondRank) customer.RankId = goldRank.Value;
                    }
                    else if (customer.Points >= 50000 && silverRank.HasValue)
                    {
                        if (customer.RankId != diamondRank && customer.RankId != goldRank) customer.RankId = silverRank.Value;
                    }

                    _context.Customers.Update(customer);
                    await _context.SaveChangesAsync(); // Lưu thay đổi Customer
                }
                // ==================================================================================

                await transaction.CommitAsync();

                return Json(new { success = true, message = "Thanh toán thành công!", orderId = order.OrderId });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return Json(new { success = false, message = "Lỗi: " + ex.Message });
            }
        }

        public class CheckoutRequest
        {
            public string? CustomerPhone { get; set; }
            public string? CustomerName { get; set; }
            public string? EmployeeId { get; set; }
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