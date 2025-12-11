using BookstoreManagement.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace BookstoreManagement.Controllers
{
    // Không dùng [Authorize] để khách vãng lai có thể truy cập
    [AllowAnonymous]
    public class ShoppingController : Controller
    {
        private readonly BookstoreContext _context;
        private readonly UserManager<AppUser> _userManager;

        public ShoppingController(BookstoreContext context, UserManager<AppUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index(string type = "normal",
                                                bool isAjax = false,
                                                int? categoryId = null,
                                                int? authorId = null,
                                                int? publisherId = null,
                                                decimal? minPrice = null,
                                                decimal? maxPrice = null)
        {
            // 1. Lấy danh sách thể loại cho Menu (giữ nguyên)
            ViewBag.Categories = await _context.Categories.OrderBy(c => c.Name).ToListAsync();
            ViewBag.Authors = await _context.Authors.OrderBy(a => a.Name).Select(a => new { a.AuthorId, a.Name }).ToListAsync();
            ViewBag.Publishers = await _context.Publishers.OrderBy(p => p.Name).Select(p => new { p.PublisherId, p.Name }).ToListAsync();

            // --- LOGIC LẤY DỮ LIỆU FEATURED (Giữ nguyên logic cũ của bạn) ---
            List<Book> featuredBooks = new List<Book>();
            string featuredTitle = "";

            switch (type.ToLower())
            {
                case "hot":
                    featuredTitle = "Các cuốn sách bán chạy nhất";
                    var topBookIds = await _context.OrderDetails
                        .GroupBy(od => od.BookId)
                        .OrderByDescending(g => g.Sum(od => od.Quantity))
                        .Select(g => g.Key).Take(10).ToListAsync();

                    if (topBookIds.Any())
                    {
                        var books = await _context.Books.Include(b => b.Author).Where(b => topBookIds.Contains(b.BookId)).ToListAsync();
                        featuredBooks = topBookIds.Join(books, id => id, b => b.BookId, (id, b) => b).ToList();
                    }
                    break;

                case "new":
                    var today = DateTime.Now;
                    var startOfMonth = new DateTime(today.Year, today.Month, 1);
                    featuredBooks = await _context.Books.Include(b => b.Author)
                        .Where(b => b.CreatedAt >= startOfMonth && b.IsDeleted != true)
                        .OrderByDescending(b => b.CreatedAt).ToListAsync();

                    if (featuredBooks.Any()) featuredTitle = "Sách mới trong tháng";
                    else
                    {
                        featuredTitle = "Các cuốn sách mới nhất";
                        featuredBooks = await _context.Books.Include(b => b.Author).Where(b => b.IsDeleted != true).OrderByDescending(b => b.CreatedAt).Take(10).ToListAsync();
                    }
                    break;

                case "promotion":
                    featuredTitle = "Sách đang khuyến mãi";
                    var now = DateTime.Now;
                    var activePromotions = _context.Promotions.Where(p => p.IsActive == true && (p.EndDate == null || p.EndDate >= now));
                    var activePromoIds = await activePromotions.Select(p => p.PromotionId).ToListAsync();
                    var bookIdsInPromo = await _context.BookPromotions.Where(bp => activePromoIds.Contains(bp.PromotionId)).Select(bp => bp.BookId).ToListAsync();
                    var giftBookIds = await activePromotions.Where(p => p.GiftBookId != null).Select(p => p.GiftBookId.Value).ToListAsync();
                    var allPromoBookIds = bookIdsInPromo.Concat(giftBookIds).Distinct().ToList();

                    featuredBooks = await _context.Books.Include(b => b.Author).Where(b => allPromoBookIds.Contains(b.BookId) && b.IsDeleted != true).ToListAsync();
                    break;

                case "filter":
                    featuredTitle = "Kết quả tìm kiếm";

                    // Khởi tạo query cơ bản
                    var query = _context.Books
                        .Include(b => b.Author)
                        .Include(b => b.Publisher) // Include thêm Publisher để hiển thị nếu cần
                        .Where(b => b.IsDeleted != true && b.StockQuantity > 0);

                    // 1. Lọc theo Thể loại
                    if (categoryId.HasValue)
                    {
                        // Dùng bảng trung gian BookCategory
                        query = query.Where(b => b.BookCategories.Any(bc => bc.CategoryId == categoryId));
                    }

                    // 2. Lọc theo Tác giả
                    if (authorId.HasValue)
                    {
                        query = query.Where(b => b.AuthorId == authorId);
                    }

                    // 3. Lọc theo Nhà xuất bản (Tiêu chí gợi ý thêm)
                    if (publisherId.HasValue)
                    {
                        query = query.Where(b => b.PublisherId == publisherId);
                    }

                    // 4. Lọc theo Khoảng giá
                    if (minPrice.HasValue)
                    {
                        query = query.Where(b => b.Price >= minPrice.Value);
                    }
                    if (maxPrice.HasValue)
                    {
                        query = query.Where(b => b.Price <= maxPrice.Value);
                    }

                    // Thực thi query
                    featuredBooks = await query.OrderByDescending(b => b.CreatedAt).ToListAsync();
                    break;

                default:
                    featuredTitle = "Sách nổi bật";
                    featuredBooks = await _context.Books.Include(b => b.Author)
                                                        .Where(b => b.IsDeleted != true && b.StockQuantity > 0)
                                                        .OrderByDescending(b => b.CreatedAt)
                                                        .Take(8).ToListAsync();
                    break;
            }

            ViewBag.FeaturedTitle = featuredTitle;
            ViewBag.CurrentType = type;

            if (isAjax)
            {
                // Nếu là AJAX, chỉ trả về Partial View chứa danh sách sách
                return PartialView("_FeaturedSection", featuredBooks);
            }

            // --- TRƯỜNG HỢP LOAD TRANG BÌNH THƯỜNG ---
            ViewBag.FeaturedBooks = featuredBooks; // Đẩy dữ liệu vào ViewBag để View chính dùng lần đầu

            var allBooks = await _context.Books
                .Include(b => b.Author)
                .Where(b => b.IsDeleted != true)
                .OrderBy(b => b.Title).Take(50).ToListAsync();

            return View(allBooks);
        }


        public async Task<IActionResult> GetBookDetails(int id)
        {
            var book = await _context.Books
                .Include(b => b.Author)
                .Include(b => b.Publisher)
                .Include(b => b.BookCategories).ThenInclude(bc => bc.Category)
                .FirstOrDefaultAsync(b => b.BookId == id);

            if (book == null)
            {
                return NotFound();
            }

            int currentUserRating = 0;
            if (User.Identity.IsAuthenticated)
            {
                var userId = _userManager.GetUserId(User);
                var rating = await _context.BookRatings
                    .FirstOrDefaultAsync(r => r.BookId == id && r.UserId == userId);
                if (rating != null)
                {
                    currentUserRating = rating.RatingValue;
                }
            }

            ViewBag.CurrentUserRating = currentUserRating;

            // --- LOGIC LẤY KHUYẾN MÃI (BỔ SUNG) ---
            object promotionInfo = null;
            var now = DateTime.Now;

            // 1. Kiểm tra BookPromotion (Sách nằm trong danh sách áp dụng)
            promotionInfo = await _context.BookPromotions
                .Where(bp => bp.BookId == id && bp.Promotion.IsActive == true && (bp.Promotion.EndDate == null || bp.Promotion.EndDate >= now))
                .Select(bp => new { Name = bp.Promotion.Name, TypeId = bp.Promotion.TypeId, Discount = bp.Promotion.DiscountPercent, MinPurchase = bp.Promotion.MinPurchaseAmount })
                .FirstOrDefaultAsync();

            // 2. Kiểm tra GiftBook (Sách là quà tặng)
            if (promotionInfo == null)
            {
                promotionInfo = await _context.Promotions
                    .Where(p => p.GiftBookId == id && p.IsActive == true && (p.EndDate == null || p.EndDate >= now))
                    .Select(p => new { Name = p.Name, TypeId = p.TypeId, Discount = p.DiscountPercent, MinPurchase = p.MinPurchaseAmount })
                    .FirstOrDefaultAsync();
            }

            ViewBag.ActivePromotion = promotionInfo;
            // ----------------------------------------

            // Trả về PartialView chứa HTML chi tiết sách
            return PartialView("_BookDetailsModal", book);
        }


        [HttpPost]
        [Authorize] // Bắt buộc đăng nhập mới được đánh giá
        public async Task<IActionResult> RateBook(int bookId, int ratingValue, string? comment)
        {
            if (ratingValue < 1 || ratingValue > 5)
            {
                return Json(new { success = false, message = "Điểm đánh giá không hợp lệ." });
            }

            var userId = _userManager.GetUserId(User);
            var book = await _context.Books.FindAsync(bookId);

            if (book == null) return Json(new { success = false, message = "Sách không tồn tại." });

            // 1. Kiểm tra xem đã đánh giá chưa
            var existingRating = await _context.BookRatings
                .FirstOrDefaultAsync(r => r.BookId == bookId && r.UserId == userId);

            if (existingRating != null)
            {
                // Cập nhật đánh giá cũ
                existingRating.RatingValue = ratingValue;
                existingRating.Comment = comment;
                existingRating.CreatedAt = DateTime.Now;
                _context.Update(existingRating);
            }
            else
            {
                // Tạo đánh giá mới
                var newRating = new BookRating
                {
                    BookId = bookId,
                    UserId = userId,
                    RatingValue = ratingValue,
                    Comment = comment,
                    CreatedAt = DateTime.Now
                };
                _context.BookRatings.Add(newRating);
            }

            await _context.SaveChangesAsync();

            // 2. Tính toán lại điểm trung bình cho sách và lưu vào bảng Book
            // Việc này giúp khi hiển thị trang chủ không cần tính toán lại
            var ratings = await _context.BookRatings.Where(r => r.BookId == bookId).ToListAsync();

            if (ratings.Any())
            {
                book.AverageRating = Math.Round(ratings.Average(r => r.RatingValue), 1); // Làm tròn 1 chữ số thập phân
                book.TotalRatings = ratings.Count;
            }
            else
            {
                book.AverageRating = 0;
                book.TotalRatings = 0;
            }

            _context.Update(book);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Cảm ơn bạn đã đánh giá!", newAverage = book.AverageRating, newTotal = book.TotalRatings });
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> SearchAll(string term)
        {
            if (string.IsNullOrWhiteSpace(term))
            {
                return Json(new { authors = new List<object>(), books = new List<object>() });
            }

            string keyword = term.Trim();
            var now = DateTime.Now;

            // 1. Tìm kiếm Tác giả
            var matchingAuthors = await _context.Authors
                .Include(a => a.Books)
                .Where(a => a.Name.Contains(keyword) || (a.Pseudonym != null && a.Pseudonym.Contains(keyword)))
                .Select(a => new
                {
                    Id = a.AuthorId,
                    Name = a.Name,
                    ImageUrl = a.ImageUrl,
                    BookCount = a.Books.Count,
                    Type = "Author"
                })
                .Take(5)
                .ToListAsync();

            // 2. Tìm kiếm Sách
            var matchingBooks = await _context.Books
                .Include(b => b.Author)
                .Where(b => b.Title.Contains(keyword) && b.IsDeleted != true && b.StockQuantity > 0)
                .Select(b => new
                {
                    Id = b.BookId,
                    Title = b.Title,
                    AuthorName = b.Author.Name,
                    Price = b.Price,
                    ImageUrl = b.ImageUrl,
                    AverageRating = b.AverageRating,
                    TotalRatings = b.TotalRatings,
                    Type = "Book",
                    // Kiểm tra khuyến mãi ngay trong query để hiển thị badge
                    IsOnSale = _context.BookPromotions.Any(bp =>
                                        bp.BookId == b.BookId &&
                                        bp.Promotion.IsActive == true &&
                                        (bp.Promotion.EndDate == null || bp.Promotion.EndDate >= now))
                               || _context.Promotions.Any(p =>
                                        p.GiftBookId == b.BookId &&
                                        p.IsActive == true &&
                                        (p.EndDate == null || p.EndDate >= now))
                })
                .Take(15)
                .ToListAsync();

            return Json(new
            {
                authors = matchingAuthors,
                books = matchingBooks
            });
        }

        public async Task<IActionResult> GetAuthorDetails(int id)
        {
            var author = await _context.Authors
                .Include(a => a.Books) // Load sách của tác giả
                .FirstOrDefaultAsync(a => a.AuthorId == id);

            if (author == null) return NotFound();

            // Lọc bỏ sách đã xóa (Soft Delete) và sắp xếp mới nhất
            // Lưu ý: EF Core không cho filter trực tiếp trên Include dễ dàng ở bản cũ, 
            // nên ta có thể filter trên View hoặc dùng query explicit loading.
            // Ở đây ta dùng cách đơn giản: Load hết rồi filter bên View cho gọn code.

            return PartialView("_AuthorDetailsModal", author);
        }


        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetCartDetails([FromQuery] string bookIds)
        {
            if (string.IsNullOrEmpty(bookIds))
            {
                return Json(new { success = true, items = new List<object>() });
            }

            // Chuyển chuỗi ID thành List<int>
            var ids = bookIds.Split(',').Select(int.Parse).ToList();

            var cartItems = await _context.Books
                .Where(b => ids.Contains(b.BookId) && b.IsDeleted != true)
                .Select(b => new
                {
                    bookId = b.BookId,
                    title = b.Title,
                    price = b.Price,
                    imageUrl = b.ImageUrl,
                    stock = b.StockQuantity,
                    // Thêm các thông tin khuyến mãi nếu cần
                })
                .ToListAsync();

            return Json(new { success = true, items = cartItems });
        }

        [HttpGet]
        public IActionResult Checkout()
        {
            return View();
        }

        // 2. POST: Xử lý đặt hàng (API)
        [HttpPost]
        public async Task<IActionResult> PlaceOrder([FromBody] OnlineCheckoutRequest request)
        {
            if (request == null || request.CartItems == null || !request.CartItems.Any())
                return Json(new { success = false, message = "Giỏ hàng trống!" });

            using var transaction = _context.Database.BeginTransaction();
            try
            {
                // A. XỬ LÝ KHÁCH HÀNG & USER
                // Logic: Nếu đã đăng nhập -> Lấy thông tin User. Nếu chưa -> Tạo Customer vãng lai hoặc tìm theo SĐT

                string userId = User.FindFirstValue(ClaimTypes.NameIdentifier); // ID tài khoản đăng nhập (nếu có)
                string customerId = null;

                // Kiểm tra khách hàng trong bảng Customer dựa trên SĐT
                var customer = await _context.Customers.FirstOrDefaultAsync(c => c.Phone == request.CustomerPhone);

                if (customer == null)
                {
                    // Tạo khách hàng mới
                    customer = new Customer
                    {
                        FullName = request.CustomerName ?? "Khách Online",
                        Phone = request.CustomerPhone,
                        Address = request.Address,
                        Email = "online@guest.com", // Có thể update nếu có field email
                        IsActive = true,
                        CreatedAt = DateTime.Now
                    };
                    _context.Customers.Add(customer);
                    await _context.SaveChangesAsync();
                }
                customerId = customer.CustomerId;

                // Nếu user chưa đăng nhập, ta cần một UserID đại diện để lưu vào bảng Order (vì bảng Order yêu cầu UserId - người tạo đơn)
                // Bạn có thể lấy ID của Admin mặc định hoặc tạo một User "System" cho đơn online.
                // Ở đây tôi tạm lấy User đầu tiên tìm thấy nếu khách không đăng nhập (LƯU Ý: Bạn nên tạo 1 user tên "OnlineBot" để gán thì tốt hơn)
                if (string.IsNullOrEmpty(userId))
                {
                    var defaultUser = await _context.Users.FirstOrDefaultAsync();
                    userId = defaultUser?.Id ?? "unknown";
                }

                string methodKey = request.PaymentMethod == "Transfer" ? "Chuyển khoản" : "Tiền mặt";
                // Lưu ý: Cần đảm bảo trong bảng Code bạn có cột Key lưu "Transfer"/"Cash" 
                // hoặc tìm theo Value tiếng Việt: "Chuyển khoản"/"Tiền mặt"

                var paymentCode = await _context.Codes
                    .FirstOrDefaultAsync(c => c.Entity == "PaymentMethod" && (c.Value == methodKey || c.Value == (request.PaymentMethod == "Transfer" ? "Chuyển khoản" : "Tiền mặt")));

                if (paymentCode == null)
                {
                    // Fallback: Nếu không tìm thấy trong DB, lấy cái đầu tiên thuộc PaymentMethod để không lỗi
                    paymentCode = await _context.Codes.FirstOrDefaultAsync(c => c.Entity == "PaymentMethod");
                }

                int paymentMethodId = paymentCode?.CodeId ?? 1;

                // B. TẠO ĐƠN HÀNG (ORDER)
                var order = new Order
                {
                    CustomerId = customerId,
                    UserId = userId, // Người tạo đơn (Khách hàng nếu đã login, hoặc Admin/Bot)
                    OrderDate = DateTime.Now,
                    Status = "Pending", // Đơn online thường là Pending (Chờ xử lý) thay vì Completed ngay
                    PaymentMethodId = paymentMethodId, // 1: Cash (COD), 2: Transfer
                    TotalAmount = 0,
                    FinalAmount = 0,
                    DiscountAmount = 0,
                    CreatedAt = DateTime.Now
                };
                _context.Orders.Add(order);
                await _context.SaveChangesAsync();

                // C. TẠO PHIẾU XUẤT KHO (EXPORT TICKET) - Logic tự động trừ kho
                var exportTicket = new ExportTicket
                {
                    UserId = userId,
                    ReferenceId = order.OrderId,
                    Date = DateTime.Now,
                    Status = "Completed", // Xuất kho luôn để giữ hàng
                    Reason = "Bán hàng Online",
                    DocumentNumber = $"EX-OL-{DateTimeOffset.Now.ToUnixTimeSeconds()}",
                    CreatedAt = DateTime.Now,
                    TotalQuantity = 0
                };
                _context.ExportTickets.Add(exportTicket);

                // D. CHI TIẾT ĐƠN HÀNG & TRỪ TỒN KHO
                decimal subTotal = 0;
                int totalQty = 0;

                foreach (var item in request.CartItems)
                {
                    var book = await _context.Books.FindAsync(item.BookId);

                    // --- KIỂM TRA TỒN KHO (Như bạn yêu cầu) ---
                    if (book == null) throw new Exception($"Sách ID {item.BookId} không tồn tại");
                    if ((book.StockQuantity ?? 0) < item.Quantity)
                        throw new Exception($"Sách '{book.Title}' không đủ hàng (Còn: {book.StockQuantity})");

                    // Trừ tồn kho
                    book.StockQuantity -= item.Quantity;

                    // Tạo chi tiết đơn hàng
                    var orderDetail = new OrderDetail
                    {
                        OrderId = order.OrderId,
                        BookId = item.BookId,
                        Quantity = item.Quantity,
                        UnitPrice = book.Price,
                        Subtotal = book.Price * item.Quantity
                    };
                    _context.OrderDetails.Add(orderDetail);

                    // Tạo chi tiết xuất kho
                    var exportDetail = new ExportDetail
                    {
                        Export = exportTicket,
                        BookId = item.BookId,
                        Quantity = item.Quantity,
                        UnitPrice = book.Price,
                        Subtotal = book.Price * item.Quantity,
                        Note = "Đơn Online"
                    };
                    _context.ExportDetails.Add(exportDetail);

                    subTotal += orderDetail.Subtotal;
                    totalQty += item.Quantity;
                }

                // E. CẬP NHẬT TỔNG TIỀN
                order.TotalAmount = subTotal;
                order.FinalAmount = subTotal; // Chưa tính khuyến mãi phức tạp cho online, có thể mở rộng sau
                exportTicket.TotalQuantity = totalQty;

                await _context.SaveChangesAsync();



                // ==================================================================================
                // --- LOGIC TÍCH ĐIỂM (Copy logic tương tự SalesController nhưng điều chỉnh biến) ---
                if (customer.Phone != "00000000") // Giả sử khách online cũng có thể nhập sđt rác, check cho chắc
                {
                    int earnedPoints = 0;
                    foreach (var item in request.CartItems)
                    {
                        // Lấy giá sách từ DB (vì request online có thể không tin cậy về giá)
                        var book = await _context.Books.FindAsync(item.BookId);
                        if (book != null)
                        {
                            earnedPoints += (int)((book.Price * item.Quantity) * 0.1m); // 10%
                        }
                    }

                    customer.Points += earnedPoints;

                    // Lấy ID hạng từ DB
                    var ranks = await _context.Codes.Where(c => c.Entity == "MemberRank").ToListAsync();
                    var silverRank = ranks.FirstOrDefault(r => r.Value == "Bạc")?.CodeId;
                    var goldRank = ranks.FirstOrDefault(r => r.Value == "Vàng")?.CodeId;
                    var diamondRank = ranks.FirstOrDefault(r => r.Value == "Kim Cương")?.CodeId;

                    // Xét hạng
                    if (customer.Points >= 200000 && diamondRank.HasValue)
                        customer.RankId = diamondRank.Value;
                    else if (customer.Points >= 100000 && goldRank.HasValue)
                    {
                        if (customer.RankId != diamondRank) customer.RankId = goldRank.Value;
                    }
                    else if (customer.Points >= 50000 && silverRank.HasValue)
                    {
                        if (customer.RankId != diamondRank && customer.RankId != goldRank) customer.RankId = silverRank.Value;
                    }

                    _context.Customers.Update(customer);
                    await _context.SaveChangesAsync();
                }
                // ==================================================================================


                await transaction.CommitAsync();

                return Json(new { success = true, message = "Đặt hàng thành công!", orderId = order.OrderId });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return Json(new { success = false, message = "Lỗi: " + ex.Message });
            }
        }

        // 4. QUẢN LÝ ĐƠN HÀNG CÁ NHÂN & TÀI KHOẢN
        // ---------------------------------------------------------

        // GET: /Shopping/MyOrders
        [Authorize]
        public async Task<IActionResult> MyOrders()
        {
            var userId = _userManager.GetUserId(User);

            var orders = await _context.Orders
                .Include(o => o.PaymentMethod)
                .Where(o => o.UserId == userId)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            return View(orders);
        }

        // GET: /Shopping/GetOrderDetails/5 (API cho Modal)
        [Authorize]
        public async Task<IActionResult> GetOrderDetails(int id)
        {
            var userId = _userManager.GetUserId(User);

            var order = await _context.Orders
                .Include(o => o.PaymentMethod)
                .Include(o => o.Promotion)
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Book)
                .FirstOrDefaultAsync(o => o.OrderId == id && o.UserId == userId); // Bảo mật: Chỉ xem đơn của chính mình

            if (order == null) return NotFound();

            return PartialView("_OrderDetailsModal", order);
        }

        // GET: /Shopping/MyAccount
        [Authorize]
        public async Task<IActionResult> MyAccount()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            // Lấy thông tin Customer kèm Rank
            var customerInfo = await _context.Customers
                .Include(c => c.Rank) // Include bảng Code
                .FirstOrDefaultAsync(c => c.Phone == user.PhoneNumber || c.Email == user.Email);

            ViewBag.CustomerAddress = customerInfo?.Address ?? "Chưa cập nhật";

            // --- DỮ LIỆU THẬT ---
            ViewBag.LoyaltyPoints = customerInfo?.Points ?? 0;
            ViewBag.MemberRank = customerInfo?.Rank?.Value ?? "Thành viên mới"; // Nếu null thì là New
                                                                                // --------------------

            return View(user);
        }


        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProfile(string fullName, string phoneNumber, string address, string email)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            // 1. Cập nhật bảng Users (AppUser)
            user.FullName = fullName;
            user.PhoneNumber = phoneNumber;
            user.Address = address;
            user.UpdatedAt = DateTime.Now;

            // (Tùy chọn) Nếu cho phép đổi Email thì cần logic phức tạp hơn (gửi mail xác nhận lại)
            // Ở đây tạm thời chỉ update field Email, nhưng Username vẫn giữ nguyên để tránh lỗi đăng nhập
            user.Email = email;

            var result = await _userManager.UpdateAsync(user);

            if (!result.Succeeded)
            {
                TempData["ErrorMessage"] = "Lỗi cập nhật tài khoản: " + string.Join(", ", result.Errors.Select(e => e.Description));
                return RedirectToAction(nameof(MyAccount));
            }

            // 2. Cập nhật bảng Customers (Để đồng bộ dữ liệu mua hàng)
            // Tìm Customer liên kết với User này (qua SĐT cũ hoặc Email cũ)
            var customer = await _context.Customers
                .FirstOrDefaultAsync(c => c.Phone == user.PhoneNumber || c.Email == user.Email);

            if (customer != null)
            {
                // Nếu đã có hồ sơ khách hàng -> Cập nhật
                customer.FullName = fullName;
                customer.Phone = phoneNumber; // Cập nhật SĐT mới
                customer.Address = address;
                customer.Email = email;
                customer.UpdatedAt = DateTime.Now;
                _context.Customers.Update(customer);
            }
            else
            {
                // Nếu chưa có hồ sơ khách hàng -> Tạo mới (Để tích điểm sau này)
                customer = new Customer
                {
                    FullName = fullName,
                    Phone = phoneNumber,
                    Email = email,
                    Address = address,
                    IsActive = true,
                    CreatedAt = DateTime.Now,
                    Points = 0
                };
                _context.Customers.Add(customer);
            }

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Cập nhật thông tin thành công!";
            return RedirectToAction(nameof(MyAccount));
        }


        // Class DTO nhận dữ liệu từ Client
        public class OnlineCheckoutRequest
        {
            public string? CustomerName { get; set; }
            public string? CustomerPhone { get; set; }
            public string? Address { get; set; }
            public string? PaymentMethod { get; set; } // "Cash" (COD) hoặc "Transfer"
            public List<CartItemDto> CartItems { get; set; }
        }

        public class CartItemDto
        {
            public int BookId { get; set; }
            public int Quantity { get; set; }
        }

    }
}