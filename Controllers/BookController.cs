using BookstoreManagement.Models;
using BookstoreManagement.ViewModels.Book;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace BookstoreManagement.Controllers
{
    [Authorize]
    public class BookController : Controller
    {
        private readonly BookstoreContext _context;

        public BookController(BookstoreContext context)
        {
            _context = context;
        }

        // GET: Book
        [Authorize(Policy = "Book.View")]
        public async Task<IActionResult> Index(string searchString, int? authorId, int? publisherId, int? categoryId, int pageNumber = 1, int pageSize = 10)
        {
            ViewData["CurrentFilter"] = searchString;
            ViewData["AuthorId"] = authorId;
            ViewData["PublisherId"] = publisherId;
            ViewData["CategoryId"] = categoryId;

            var booksQuery = _context.Books
                .Include(b => b.Author)
                .Include(b => b.Publisher)
                .Where(b => b.IsDeleted != true)
                .AsQueryable();

            // Search filter
            if (!string.IsNullOrEmpty(searchString))
            {
                booksQuery = booksQuery.Where(b =>
                    b.Title.Contains(searchString) ||
                    b.Author.Name.Contains(searchString) ||
                    b.Publisher.Name.Contains(searchString));
            }

            // Author filter
            if (authorId.HasValue)
            {
                booksQuery = booksQuery.Where(b => b.AuthorId == authorId);
            }

            // Publisher filter
            if (publisherId.HasValue)
            {
                booksQuery = booksQuery.Where(b => b.PublisherId == publisherId);
            }

            if (categoryId.HasValue)
            {
                var bookIdsInCat = _context.BookCategories
                                    .Where(bc => bc.CategoryId == categoryId)
                                    .Select(bc => bc.BookId);
                booksQuery = booksQuery.Where(b => bookIdsInCat.Contains(b.BookId));
            }

            var totalItems = await booksQuery.CountAsync();
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            var books = await booksQuery
                .OrderByDescending(b => b.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(b => new BookViewModel
                {
                    BookId = b.BookId,
                    Title = b.Title,
                    ImageUrl = b.ImageUrl,
                    AuthorName = b.Author.Name,
                    AuthorId = b.AuthorId,
                    PublisherName = b.Publisher.Name,
                    PublisherId = b.PublisherId,
                    PublicationYear = b.PublicationYear,
                    Price = b.Price,
                    StockQuantity = b.StockQuantity,
                    Description = b.Description,
                    LowStockThreshold = b.LowStockThreshold,
                    CreatedAt = b.CreatedAt,
                    UpdatedAt = b.UpdatedAt,
                    IsDeleted = b.IsDeleted
                })
                .ToListAsync();

            ViewBag.PageNumber = pageNumber;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalItems = totalItems;

            // Load dropdowns for filters
            ViewBag.Authors = new SelectList(await _context.Authors.OrderBy(a => a.Name).ToListAsync(), "AuthorId", "Name");
            ViewBag.Publishers = new SelectList(await _context.Publishers.OrderBy(p => p.Name).ToListAsync(), "PublisherId", "Name");
            ViewBag.Categories = new SelectList(await _context.Categories.OrderBy(c => c.Name).ToListAsync(), "CategoryId", "Name");

            return View(books);
        }

        // GET: Book/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var book = await _context.Books
                .Include(b => b.Author)
                .Include(b => b.Publisher)
                .Include(b => b.SupplierBooks)
                    .ThenInclude(sb => sb.Supplier)
                .Include(b => b.PriceHistories)
                .FirstOrDefaultAsync(m => m.BookId == id);

            if (book == null)
            {
                return NotFound();
            }

            var primarySupplierBook = book.SupplierBooks.FirstOrDefault();

            var categoryNames = await _context.BookCategories
                .Where(bc => bc.BookId == id)
                .Include(bc => bc.Category)
                .Select(bc => bc.Category.Name)
                .ToListAsync();

            var viewModel = new BookViewModel
            {
                BookId = book.BookId,
                Title = book.Title,
                ImageUrl = book.ImageUrl,
                AuthorName = book.Author.Name,
                AuthorId = book.AuthorId,
                PublisherName = book.Publisher.Name,
                PublisherId = book.PublisherId,
                PublicationYear = book.PublicationYear,
                Price = book.Price,
                StockQuantity = book.StockQuantity,
                Description = book.Description,
                SupplierName = primarySupplierBook?.Supplier?.Name,
                DefaultCostPrice = primarySupplierBook?.DefaultCostPrice ?? 0,
                LowStockThreshold = book.LowStockThreshold,
                CreatedAt = book.CreatedAt,
                UpdatedAt = book.UpdatedAt,
                IsDeleted = book.IsDeleted
            };

            var historyData = book.PriceHistories
                .OrderBy(h => h.EffectiveDate)
                .Select(h => new
                {
                    date = h.EffectiveDate.ToString("dd/MM/yyyy HH:mm"),
                    cost = h.CostPrice,
                    price = h.SellingPrice,
                    margin = h.ProfitMargin
                })
                .ToList();

            // Nếu chưa có lịch sử (sách mới tạo), thêm giá hiện tại vào làm mốc đầu tiên
            if (!historyData.Any())
            {
                historyData.Add(new
                {
                    date = book.CreatedAt?.ToString("dd/MM/yyyy HH:mm") ?? DateTime.Now.ToString("dd/MM/yyyy HH:mm"),
                    cost = book.CostPrice,
                    price = book.Price,
                    margin = book.ProfitMargin
                });
            }

            // Serialize sang JSON để JS đọc được
            ViewBag.PriceHistoryJson = System.Text.Json.JsonSerializer.Serialize(historyData);

            return View(viewModel);
        }

        // GET: Book/Create
        [Authorize(Policy = "Book.Create")]
        public async Task<IActionResult> Create()
        {
            var viewModel = new BookCreateViewModel
            {
                Authors = await _context.Authors
                    .OrderBy(a => a.Name)
                    .Select(a => new SelectListItem
                    {
                        Value = a.AuthorId.ToString(),
                        Text = a.Name
                    })
                    .ToListAsync(),
                Publishers = await _context.Publishers
                    .OrderBy(p => p.Name)
                    .Select(p => new SelectListItem
                    {
                        Value = p.PublisherId.ToString(),
                        Text = p.Name
                    })
                    .ToListAsync(),

                Suppliers = await _context.Suppliers
                    .Where(s => s.IsActive == true) // Chỉ lấy nhà cung cấp đang hoạt động
                    .OrderBy(s => s.Name)
                    .Select(s => new SelectListItem { Value = s.SupplierId.ToString(), Text = s.Name })
                    .ToListAsync(),

                Categories = await _context.Categories
                    .OrderBy(c => c.Name)
                    .Select(c => new SelectListItem { Value = c.CategoryId.ToString(), Text = c.Name })
                    .ToListAsync()
            };

            return View(viewModel);
        }

        // POST: Book/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(BookCreateViewModel viewModel)
        {
            if (ModelState.IsValid)
            {
                string? imagePath = null;
                if (viewModel.ImageFile != null)
                {
                    imagePath = await SaveImage(viewModel.ImageFile);
                }

                // --- LOGIC TÍNH GIÁ MỚI ---
                // Giá bán = Giá vốn + (Giá vốn * %Lợi nhuận / 100)
                decimal calculatedPrice = viewModel.CostPrice + (viewModel.CostPrice * (decimal)viewModel.ProfitMargin / 100);

                // Làm tròn giá bán (ví dụ: làm tròn đến hàng nghìn) - Tùy chọn
                // calculatedPrice = Math.Ceiling(calculatedPrice / 1000) * 1000;

                var book = new Book
                {
                    Title = viewModel.Title,
                    ImageUrl = imagePath,
                    AuthorId = viewModel.AuthorId,
                    PublisherId = viewModel.PublisherId,
                    PublicationYear = viewModel.PublicationYear,
                    CostPrice = viewModel.CostPrice,
                    ProfitMargin = viewModel.ProfitMargin,
                    Price = calculatedPrice,
                    StockQuantity = 0,
                    Description = viewModel.Description,
                    LowStockThreshold = viewModel.LowStockThreshold ?? 10,
                    CreatedAt = DateTime.Now,
                    IsDeleted = false
                };

                _context.Add(book);
                await _context.SaveChangesAsync();

                // --- LƯU LỊCH SỬ GIÁ (MỚI) ---
                var priceHistory = new BookPriceHistory
                {
                    BookId = book.BookId,
                    CostPrice = book.CostPrice,
                    ProfitMargin = book.ProfitMargin,
                    SellingPrice = book.Price,
                    EffectiveDate = DateTime.Now,
                    UpdatedBy = User.Identity?.Name ?? "System" // Lưu người tạo (nếu có đăng nhập)
                };
                _context.BookPriceHistories.Add(priceHistory);

                if (viewModel.SupplierId.HasValue)
                {
                    var supplierBook = new SupplierBook
                    {
                        BookId = book.BookId,
                        SupplierId = viewModel.SupplierId.Value,
                        DefaultCostPrice = viewModel.CostPrice
                    };
                    _context.Add(supplierBook);
                    await _context.SaveChangesAsync();
                }

                if (viewModel.SelectedCategoryIds != null)
                {
                    foreach (var catId in viewModel.SelectedCategoryIds)
                    {
                        var bookCategory = new BookCategory
                        {
                            BookId = book.BookId,
                            CategoryId = catId
                        };
                        _context.Add(bookCategory);
                    }
                    await _context.SaveChangesAsync();
                }


                TempData["SuccessMessage"] = "Thêm sách thành công!";
                return RedirectToAction(nameof(Index));
            }

            // Reload dropdowns if validation fails
            viewModel.Authors = await _context.Authors
                .OrderBy(a => a.Name)
                .Select(a => new SelectListItem
                {
                    Value = a.AuthorId.ToString(),
                    Text = a.Name
                })
                .ToListAsync();

            viewModel.Publishers = await _context.Publishers
                .OrderBy(p => p.Name)
                .Select(p => new SelectListItem
                {
                    Value = p.PublisherId.ToString(),
                    Text = p.Name
                })
                .ToListAsync();

            viewModel.Suppliers = await _context.Suppliers
                .Where(s => s.IsActive == true)
                .OrderBy(s => s.Name)
                .Select(s => new SelectListItem { Value = s.SupplierId.ToString(), Text = s.Name }).ToListAsync();

            viewModel.Categories = await _context.Categories
                .OrderBy(c => c.Name)
                .Select(c => new SelectListItem { Value = c.CategoryId.ToString(), Text = c.Name })
                .ToListAsync();


            return View(viewModel);
        }

        [Authorize(Policy = "Book.Update")]
        // GET: Book/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var book = await _context.Books
            .Include(b => b.SupplierBooks)
                .ThenInclude(sb => sb.Supplier)
            .FirstOrDefaultAsync(m => m.BookId == id);

            if (book == null)
            {
                return NotFound();
            }

            var primarySupplierBook = book.SupplierBooks.FirstOrDefault();

            var allSuppliers = await _context.Suppliers.ToListAsync();
            var allAuthors = await _context.Authors.ToListAsync();
            var allPublishers = await _context.Publishers.ToListAsync();

            var currentCategoryIds = await _context.BookCategories
                .Where(bc => bc.BookId == id)
                .Select(bc => bc.CategoryId)
                .ToListAsync();

            var viewModel = new BookEditViewModel
            {
                BookId = book.BookId,
                Title = book.Title,
                ImageUrl = book.ImageUrl,
                AuthorId = book.AuthorId,
                PublisherId = book.PublisherId,
                PublicationYear = book.PublicationYear,
                Price = book.Price,
                StockQuantity = book.StockQuantity,
                Description = book.Description,
                SupplierId = primarySupplierBook.SupplierId,
                DefaultCostPrice = primarySupplierBook?.DefaultCostPrice ?? 0,
                CostPrice = book.CostPrice,
                ProfitMargin = book.ProfitMargin,
                LowStockThreshold = book.LowStockThreshold,
                IsDeleted = book.IsDeleted,
                SupplierList = new SelectList(allSuppliers, "SupplierId", "Name", primarySupplierBook?.SupplierId),
                Authors = new SelectList(allAuthors, "AuthorId", "Name", book.AuthorId).ToList(),
                Publishers = new SelectList(allPublishers, "PublisherId", "Name", book.PublisherId).ToList(),

                SelectedCategoryIds = currentCategoryIds,
                Categories = await _context.Categories
                    .OrderBy(c => c.Name)
                    .Select(c => new SelectListItem { Value = c.CategoryId.ToString(), Text = c.Name })
                    .ToListAsync()
            };

            return View(viewModel);
        }

        // POST: Book/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, BookEditViewModel viewModel)
        {
            if (id != viewModel.BookId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var book = await _context.Books
                    .Include(b => b.SupplierBooks)
                        .ThenInclude(sb => sb.Supplier)
                    .FirstOrDefaultAsync(m => m.BookId == id);

                    if (book == null)
                    {
                        return NotFound();
                    }

                    if (viewModel.ImageFile != null)
                    {
                        book.ImageUrl = await SaveImage(viewModel.ImageFile);
                    }

                    var primarySupplierBook = book.SupplierBooks.FirstOrDefault();

                    // --- XỬ LÝ THAY ĐỔI GIÁ ---
                    bool priceChanged = false;

                    // Nếu Giá vốn hoặc % Lợi nhuận thay đổi
                    if (book.CostPrice != viewModel.CostPrice || book.ProfitMargin != viewModel.ProfitMargin)
                    {
                        priceChanged = true;

                        // Tính lại giá bán
                        decimal newPrice = viewModel.CostPrice + (viewModel.CostPrice * (decimal)viewModel.ProfitMargin / 100);

                        // Cập nhật Book
                        book.CostPrice = viewModel.CostPrice;
                        book.ProfitMargin = viewModel.ProfitMargin;
                        book.Price = newPrice;
                    }

                    book.Title = viewModel.Title;
                    book.AuthorId = viewModel.AuthorId;
                    book.PublisherId = viewModel.PublisherId;
                    book.PublicationYear = viewModel.PublicationYear;
                    // book.StockQuantity = viewModel.StockQuantity;
                    book.Description = viewModel.Description;
                    book.LowStockThreshold = viewModel.LowStockThreshold;
                    book.UpdatedAt = DateTime.Now;
                    // primarySupplierBook.SupplierId = viewModel.SupplierId;
                    primarySupplierBook.DefaultCostPrice = viewModel.CostPrice;

                    var oldCategories = await _context.BookCategories.Where(bc => bc.BookId == id).ToListAsync();
                    _context.BookCategories.RemoveRange(oldCategories);

                    if (viewModel.SelectedCategoryIds != null)
                    {
                        foreach (var catId in viewModel.SelectedCategoryIds)
                        {
                            _context.Add(new BookCategory { BookId = id, CategoryId = catId });
                        }
                    }

                    _context.Update(book);

                    // --- LƯU LỊCH SỬ NẾU GIÁ ĐỔI ---
                    if (priceChanged)
                    {
                        var history = new BookPriceHistory
                        {
                            BookId = book.BookId,
                            CostPrice = book.CostPrice,
                            ProfitMargin = book.ProfitMargin,
                            SellingPrice = book.Price,
                            EffectiveDate = DateTime.Now,
                            UpdatedBy = User.Identity?.Name ?? "System"
                        };
                        _context.BookPriceHistories.Add(history);
                    }

                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Cập nhật sách thành công!";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!BookExists(viewModel.BookId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
            }

            // Reload dropdowns if validation fails
            viewModel.Authors = await _context.Authors
                .OrderBy(a => a.Name)
                .Select(a => new SelectListItem
                {
                    Value = a.AuthorId.ToString(),
                    Text = a.Name
                })
                .ToListAsync();

            viewModel.Publishers = await _context.Publishers
                .OrderBy(p => p.Name)
                .Select(p => new SelectListItem
                {
                    Value = p.PublisherId.ToString(),
                    Text = p.Name
                })
                .ToListAsync();

            viewModel.Categories = await _context.Categories
                .OrderBy(c => c.Name)
                .Select(c => new SelectListItem { Value = c.CategoryId.ToString(), Text = c.Name })
                .ToListAsync();

            return View(viewModel);
        }

        // POST: Book/Delete/5
        [Authorize(Policy = "Book.Delete")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var book = await _context.Books.FindAsync(id);
            if (book == null)
            {
                return Json(new { success = false, message = "Không tìm thấy sách" });
            }

            // Soft delete
            book.IsDeleted = true;
            book.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Xóa sách thành công" });
        }

        private bool BookExists(int id)
        {
            return _context.Books.Any(e => e.BookId == id);
        }

        private async Task<string> SaveImage(IFormFile imageFile)
        {
            string uniqueFileName = Guid.NewGuid().ToString() + "_" + imageFile.FileName;

            string uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "books");

            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            string filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await imageFile.CopyToAsync(fileStream);
            }

            return "/images/books/" + uniqueFileName;
        }
    }
}