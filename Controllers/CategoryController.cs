using BookstoreManagement.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BookstoreManagement.Controllers
{
    [Authorize(Roles = "Admin,Manager")]
    public class CategoryController : Controller
    {
        private readonly BookstoreContext _context;

        public CategoryController(BookstoreContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(string sortBy = "CreatedAt", string sortOrder = "desc")
        {
            ViewData["SortBy"] = sortBy;
            ViewData["SortOrder"] = sortOrder;

            var categoriesQuery = _context.Categories.AsQueryable();

            // Apply sorting
            categoriesQuery = sortBy?.ToLower() switch
            {
                "name" => sortOrder == "asc" 
                    ? categoriesQuery.OrderBy(c => c.Name) 
                    : categoriesQuery.OrderByDescending(c => c.Name),
                "createdat" => sortOrder == "asc" 
                    ? categoriesQuery.OrderBy(c => c.CreatedAt) 
                    : categoriesQuery.OrderByDescending(c => c.CreatedAt),
                "updatedat" => sortOrder == "asc" 
                    ? categoriesQuery.OrderBy(c => c.UpdatedAt ?? DateTime.MinValue) 
                    : categoriesQuery.OrderByDescending(c => c.UpdatedAt ?? DateTime.MinValue),
                _ => categoriesQuery.OrderByDescending(c => c.CreatedAt)
            };

            return View(await categoriesQuery.ToListAsync());
        }

        public IActionResult Create() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Category category)
        {
            if (ModelState.IsValid)
            {
                category.CreatedAt = DateTime.Now;
                _context.Add(category);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Thêm thể loại thành công!";
                return RedirectToAction(nameof(Index));
            }
            return View(category);
        }

        // 3. Sửa (GET + POST)
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var category = await _context.Categories.FindAsync(id);
            if (category == null) return NotFound();
            return View(category);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Category category)
        {
            if (id != category.CategoryId) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    var existingCat = await _context.Categories.FindAsync(id);
                    existingCat.Name = category.Name;
                    existingCat.Description = category.Description;
                    existingCat.UpdatedAt = DateTime.Now;

                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Cập nhật thành công!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Categories.Any(e => e.CategoryId == id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(category);
        }

        // 4. Xóa
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category != null)
            {
                bool hasBooks = await _context.BookCategories.AnyAsync(bc => bc.CategoryId == id);
                if (hasBooks)
                {
                    return Json(new { success = false, message = "Không thể xóa thể loại đang có sách." });
                }

                _context.Categories.Remove(category);
                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Xóa thể loại thành công." });
            }
            return Json(new { success = false, message = "Không tìm thấy thể loại." });
        }
    }
}