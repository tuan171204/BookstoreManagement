using BookstoreManagement.Models;
using BookstoreManagement.ViewModels.Author;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BookstoreManagement.Controllers
{
    [Authorize]
    public class AuthorController : Controller
    {
        private readonly BookstoreContext _context;

        public AuthorController(BookstoreContext context)
        {
            _context = context;
        }

        // GET: Author
        public async Task<IActionResult> Index(string searchString, int pageNumber = 1, int pageSize = 10)
        {
            ViewData["CurrentFilter"] = searchString;

            var authorsQuery = _context.Authors.AsQueryable();

            if (!string.IsNullOrEmpty(searchString))
            {
                authorsQuery = authorsQuery.Where(a =>
                    a.Name.Contains(searchString) ||
                    (a.Bio != null && a.Bio.Contains(searchString)));
            }

            var totalItems = await authorsQuery.CountAsync();
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            var authors = await authorsQuery
                .OrderByDescending(a => a.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(a => new AuthorViewModel
                {
                    AuthorId = a.AuthorId,
                    Name = a.Name,
                    Bio = a.Bio,
                    BooksCount = a.Books.Count,
                    CreatedAt = a.CreatedAt,
                    UpdatedAt = a.UpdatedAt
                })
                .ToListAsync();

            ViewBag.PageNumber = pageNumber;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalItems = totalItems;

            return View(authors);
        }

        // GET: Author/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var author = await _context.Authors
                .Include(a => a.Books)
                .FirstOrDefaultAsync(m => m.AuthorId == id);

            if (author == null)
            {
                return NotFound();
            }

            var viewModel = new AuthorViewModel
            {
                AuthorId = author.AuthorId,
                Name = author.Name,
                Bio = author.Bio,
                BooksCount = author.Books.Count,
                CreatedAt = author.CreatedAt,
                UpdatedAt = author.UpdatedAt
            };

            return View(viewModel);
        }

        // GET: Author/Create
        public IActionResult Create()
        {
            return View(new AuthorCreateViewModel());
        }

        // POST: Author/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(AuthorCreateViewModel viewModel)
        {
            if (ModelState.IsValid)
            {
                var author = new Author
                {
                    Name = viewModel.Name,
                    Bio = viewModel.Bio,
                    CreatedAt = DateTime.Now
                };

                _context.Add(author);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Thêm tác giả thành công!";
                return RedirectToAction(nameof(Index));
            }

            return View(viewModel);
        }

        // GET: Author/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var author = await _context.Authors.FindAsync(id);
            if (author == null)
            {
                return NotFound();
            }

            var viewModel = new AuthorEditViewModel
            {
                AuthorId = author.AuthorId,
                Name = author.Name,
                Bio = author.Bio
            };

            return View(viewModel);
        }

        // POST: Author/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, AuthorEditViewModel viewModel)
        {
            if (id != viewModel.AuthorId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var author = await _context.Authors.FindAsync(id);
                    if (author == null)
                    {
                        return NotFound();
                    }

                    author.Name = viewModel.Name;
                    author.Bio = viewModel.Bio;
                    author.UpdatedAt = DateTime.Now;

                    _context.Update(author);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Cập nhật tác giả thành công!";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!AuthorExists(viewModel.AuthorId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
            }

            return View(viewModel);
        }

        // POST: Author/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var author = await _context.Authors
                .Include(a => a.Books)
                .FirstOrDefaultAsync(a => a.AuthorId == id);

            if (author == null)
            {
                return Json(new { success = false, message = "Không tìm thấy tác giả" });
            }

            // Check if author has books
            if (author.Books.Any())
            {
                return Json(new { success = false, message = "Không thể xóa tác giả đang có sách" });
            }

            _context.Authors.Remove(author);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Xóa tác giả thành công" });
        }

        private bool AuthorExists(int id)
        {
            return _context.Authors.Any(e => e.AuthorId == id);
        }
    }
}
