using BookstoreManagement.Models;
using BookstoreManagement.ViewModels.Promotion;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace BookstoreManagement.Controllers
{
    [Authorize]
    public class PromotionController : Controller
    {
        private readonly BookstoreContext _context;

        public PromotionController(BookstoreContext context)
        {
            _context = context;
        }

        // GET: Promotion
        public async Task<IActionResult> Index(string searchString, bool? isActive, string sortBy = "CreatedAt", string sortOrder = "desc", int pageNumber = 1, int pageSize = 10)
        {
            ViewData["CurrentFilter"] = searchString;
            ViewData["SortBy"] = sortBy;
            ViewData["SortOrder"] = sortOrder;
            if (isActive.HasValue)
            {
                ViewData["IsActiveFilter"] = isActive.Value.ToString();
            }
            ViewData["IsActive"] = isActive;

            var promotionsQuery = _context.Promotions
                .Include(p => p.Type)
                .Include(p => p.GiftBook)
                .AsQueryable();

            // Search filter
            if (!string.IsNullOrEmpty(searchString))
            {
                promotionsQuery = promotionsQuery.Where(p =>
                    p.Name.Contains(searchString) ||
                    p.Type.Value.Contains(searchString));
            }

            // Active filter
            if (isActive.HasValue)
            {
                promotionsQuery = promotionsQuery.Where(p => p.IsActive == isActive.Value);
            }

            // Apply sorting
            promotionsQuery = sortBy?.ToLower() switch
            {
                "name" => sortOrder == "asc" 
                    ? promotionsQuery.OrderBy(p => p.Name) 
                    : promotionsQuery.OrderByDescending(p => p.Name),
                "type" => sortOrder == "asc" 
                    ? promotionsQuery.OrderBy(p => p.Type.Value) 
                    : promotionsQuery.OrderByDescending(p => p.Type.Value),
                "discountpercent" => sortOrder == "asc" 
                    ? promotionsQuery.OrderBy(p => p.DiscountPercent ?? 0) 
                    : promotionsQuery.OrderByDescending(p => p.DiscountPercent ?? 0),
                "startdate" => sortOrder == "asc" 
                    ? promotionsQuery.OrderBy(p => p.StartDate ?? DateTime.MinValue) 
                    : promotionsQuery.OrderByDescending(p => p.StartDate ?? DateTime.MinValue),
                "enddate" => sortOrder == "asc" 
                    ? promotionsQuery.OrderBy(p => p.EndDate ?? DateTime.MaxValue) 
                    : promotionsQuery.OrderByDescending(p => p.EndDate ?? DateTime.MaxValue),
                "isactive" => sortOrder == "asc" 
                    ? promotionsQuery.OrderBy(p => p.IsActive) 
                    : promotionsQuery.OrderByDescending(p => p.IsActive),
                "createdat" => sortOrder == "asc" 
                    ? promotionsQuery.OrderBy(p => p.CreatedAt) 
                    : promotionsQuery.OrderByDescending(p => p.CreatedAt),
                _ => promotionsQuery.OrderByDescending(p => p.CreatedAt)
            };

            var totalItems = await promotionsQuery.CountAsync();
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            var promotions = await promotionsQuery
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(p => new PromotionViewModel
                {
                    PromotionId = p.PromotionId,
                    Name = p.Name,
                    TypeName = p.Type.Value,
                    TypeId = p.TypeId,
                    DiscountPercent = p.DiscountPercent,
                    StartDate = p.StartDate,
                    EndDate = p.EndDate,
                    MinPurchaseAmount = p.MinPurchaseAmount,
                    GiftBookName = p.GiftBook != null ? p.GiftBook.Title : null,
                    GiftBookId = p.GiftBookId,
                    IsActive = p.IsActive,
                    AppliedBooksCount = _context.Set<BookPromotion>().Count(bp => bp.PromotionId == p.PromotionId),
                    CreatedAt = p.CreatedAt,
                    UpdatedAt = p.UpdatedAt
                })
                .ToListAsync();

            ViewBag.PageNumber = pageNumber;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalItems = totalItems;
            ViewBag.IsActiveParam = isActive;

            return View(promotions);
        }

        // GET: Promotion/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var promotion = await _context.Promotions
                .Include(p => p.Type)
                .Include(p => p.GiftBook)
                .FirstOrDefaultAsync(m => m.PromotionId == id);

            if (promotion == null)
            {
                return NotFound();
            }

            var viewModel = new PromotionViewModel
            {
                PromotionId = promotion.PromotionId,
                Name = promotion.Name,
                TypeName = promotion.Type.Value,
                TypeId = promotion.TypeId,
                DiscountPercent = promotion.DiscountPercent,
                StartDate = promotion.StartDate,
                EndDate = promotion.EndDate,
                MinPurchaseAmount = promotion.MinPurchaseAmount,
                GiftBookName = promotion.GiftBook?.Title,
                GiftBookId = promotion.GiftBookId,
                IsActive = promotion.IsActive,
                AppliedBooksCount = await _context.Set<BookPromotion>().CountAsync(bp => bp.PromotionId == id),
                CreatedAt = promotion.CreatedAt,
                UpdatedAt = promotion.UpdatedAt
            };

            // Get applied books
            var appliedBooks = await _context.Set<BookPromotion>()
                .Where(bp => bp.PromotionId == id)
                .Include(bp => bp.Book)
                .Select(bp => bp.Book)
                .ToListAsync();

            ViewBag.AppliedBooks = appliedBooks;

            return View(viewModel);
        }

        // GET: Promotion/Create
        public async Task<IActionResult> Create()
        {
            var viewModel = new PromotionCreateViewModel
            {
                PromotionTypes = await GetPromotionTypesAsync(),
                Books = await GetBooksAsync(),
                StartDate = DateTime.Now,
                EndDate = DateTime.Now.AddMonths(1)
            };

            return View(viewModel);
        }

        // POST: Promotion/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(PromotionCreateViewModel viewModel)
        {
            if (ModelState.IsValid)
            {
                var promotion = new Models.Promotion
                {
                    Name = viewModel.Name,
                    TypeId = viewModel.TypeId,
                    DiscountPercent = viewModel.DiscountPercent,
                    StartDate = viewModel.StartDate,
                    EndDate = viewModel.EndDate,
                    MinPurchaseAmount = viewModel.MinPurchaseAmount,
                    GiftBookId = viewModel.GiftBookId,
                    IsActive = viewModel.IsActive,
                    CreatedAt = DateTime.Now
                };

                _context.Add(promotion);
                await _context.SaveChangesAsync();

                if (viewModel.SelectedBookIds != null && viewModel.SelectedBookIds.Any())
                {
                    foreach (var bookId in viewModel.SelectedBookIds)
                    {
                        _context.Set<BookPromotion>().Add(new BookPromotion
                        {
                            PromotionId = promotion.PromotionId,
                            BookId = bookId
                        });
                    }
                    await _context.SaveChangesAsync();
                }

                TempData["SuccessMessage"] = "Thêm chương trình khuyến mãi thành công!";
                return RedirectToAction(nameof(Index));
            }

            viewModel.PromotionTypes = await GetPromotionTypesAsync();
            viewModel.Books = await GetBooksAsync();

            return View(viewModel);
        }

        // GET: Promotion/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var promotion = await _context.Promotions.FindAsync(id);
            if (promotion == null)
            {
                return NotFound();
            }

            var selectedBookIds = await _context.Set<BookPromotion>()
                .Where(bp => bp.PromotionId == id)
                .Select(bp => bp.BookId)
                .ToListAsync();

            var viewModel = new PromotionEditViewModel
            {
                PromotionId = promotion.PromotionId,
                Name = promotion.Name,
                TypeId = promotion.TypeId,
                DiscountPercent = promotion.DiscountPercent,
                StartDate = promotion.StartDate,
                EndDate = promotion.EndDate,
                MinPurchaseAmount = promotion.MinPurchaseAmount,
                GiftBookId = promotion.GiftBookId,
                IsActive = promotion.IsActive ?? false,
                SelectedBookIds = selectedBookIds,
                PromotionTypes = await GetPromotionTypesAsync(),
                Books = await GetBooksAsync()
            };

            return View(viewModel);
        }

        // POST: Promotion/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, PromotionEditViewModel viewModel)
        {
            if (id != viewModel.PromotionId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var promotion = await _context.Promotions.FindAsync(id);
                    if (promotion == null)
                    {
                        return NotFound();
                    }

                    promotion.Name = viewModel.Name;
                    promotion.TypeId = viewModel.TypeId;
                    promotion.DiscountPercent = viewModel.DiscountPercent;
                    promotion.StartDate = viewModel.StartDate;
                    promotion.EndDate = viewModel.EndDate;
                    promotion.MinPurchaseAmount = viewModel.MinPurchaseAmount;
                    promotion.GiftBookId = viewModel.GiftBookId;
                    promotion.IsActive = viewModel.IsActive;
                    promotion.UpdatedAt = DateTime.Now;

                    _context.Update(promotion);

                    var existingBookPromotions = await _context.Set<BookPromotion>()
                        .Where(bp => bp.PromotionId == id)
                        .ToListAsync();

                    _context.Set<BookPromotion>().RemoveRange(existingBookPromotions);

                    if (viewModel.SelectedBookIds != null && viewModel.SelectedBookIds.Any())
                    {
                        foreach (var bookId in viewModel.SelectedBookIds)
                        {
                            _context.Set<BookPromotion>().Add(new BookPromotion
                            {
                                PromotionId = id,
                                BookId = bookId
                            });
                        }
                    }

                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Cập nhật chương trình khuyến mãi thành công!";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!PromotionExists(viewModel.PromotionId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
            }

            viewModel.PromotionTypes = await GetPromotionTypesAsync();
            viewModel.Books = await GetBooksAsync();

            return View(viewModel);
        }

        // POST: Promotion/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var promotion = await _context.Promotions
                .Include(p => p.Orders)
                .FirstOrDefaultAsync(p => p.PromotionId == id);

            if (promotion == null)
            {
                return Json(new { success = false, message = "Không tìm thấy chương trình khuyến mãi" });
            }

            if (promotion.Orders.Any())
            {
                return Json(new { success = false, message = "Không thể xóa chương trình khuyến mãi đã được sử dụng trong đơn hàng" });
            }

            var bookPromotions = await _context.Set<BookPromotion>()
                .Where(bp => bp.PromotionId == id)
                .ToListAsync();
            _context.Set<BookPromotion>().RemoveRange(bookPromotions);

            _context.Promotions.Remove(promotion);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Xóa chương trình khuyến mãi thành công" });
        }

        // POST: Promotion/ToggleActive/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleActive(int id)
        {
            var promotion = await _context.Promotions.FindAsync(id);
            if (promotion == null)
            {
                return Json(new { success = false, message = "Không tìm thấy chương trình khuyến mãi" });
            }

            promotion.IsActive = !promotion.IsActive;
            promotion.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            var status = promotion.IsActive == true ? "kích hoạt" : "vô hiệu hóa";
            return Json(new { success = true, message = $"Đã {status} chương trình khuyến mãi", isActive = promotion.IsActive });
        }

        private bool PromotionExists(int id)
        {
            return _context.Promotions.Any(e => e.PromotionId == id);
        }

        private async Task<List<SelectListItem>> GetPromotionTypesAsync()
        {
            return await _context.Codes
                .Where(c => c.Entity == "PromotionType")
                .OrderBy(c => c.Key)
                .Select(c => new SelectListItem
                {
                    Value = c.CodeId.ToString(),
                    Text = c.Value
                })
                .ToListAsync();
        }

        private async Task<List<SelectListItem>> GetBooksAsync()
        {
            return await _context.Books
                .Where(b => b.IsDeleted != true)
                .OrderBy(b => b.Title)
                .Select(b => new SelectListItem
                {
                    Value = b.BookId.ToString(),
                    Text = b.Title
                })
                .ToListAsync();
        }
    }
}