using BookstoreManagement.Models;
using BookstoreManagement.ViewModels.Publisher;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BookstoreManagement.Controllers
{
    [Authorize]
    public class PublisherController : Controller
    {
        private readonly BookstoreContext _context;

        public PublisherController(BookstoreContext context)
        {
            _context = context;
        }

        // GET: Publisher
        public async Task<IActionResult> Index(string searchString, int pageNumber = 1, int pageSize = 10)
        {
            ViewData["CurrentFilter"] = searchString;

            var publishersQuery = _context.Publishers.AsQueryable();

            if (!string.IsNullOrEmpty(searchString))
            {
                publishersQuery = publishersQuery.Where(p =>
                    p.Name.Contains(searchString) ||
                    (p.Address != null && p.Address.Contains(searchString)));
            }

            var totalItems = await publishersQuery.CountAsync();
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            var publishers = await publishersQuery
                .OrderByDescending(p => p.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(p => new PublisherViewModel
                {
                    PublisherId = p.PublisherId,
                    Name = p.Name,
                    Address = p.Address,
                    BooksCount = p.Books.Count,
                    CreatedAt = p.CreatedAt,
                    UpdatedAt = p.UpdatedAt
                })
                .ToListAsync();

            ViewBag.PageNumber = pageNumber;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalItems = totalItems;

            return View(publishers);
        }

        // GET: Publisher/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var publisher = await _context.Publishers
                .Include(p => p.Books)
                .FirstOrDefaultAsync(m => m.PublisherId == id);

            if (publisher == null)
            {
                return NotFound();
            }

            var viewModel = new PublisherViewModel
            {
                PublisherId = publisher.PublisherId,
                Name = publisher.Name,
                Address = publisher.Address,
                BooksCount = publisher.Books.Count,
                CreatedAt = publisher.CreatedAt,
                UpdatedAt = publisher.UpdatedAt
            };

            return View(viewModel);
        }

        // GET: Publisher/Create
        public IActionResult Create()
        {
            return View(new PublisherCreateViewModel());
        }

        // POST: Publisher/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(PublisherCreateViewModel viewModel)
        {
            if (ModelState.IsValid)
            {
                var publisher = new Publisher
                {
                    Name = viewModel.Name,
                    Address = viewModel.Address,
                    CreatedAt = DateTime.Now
                };

                _context.Add(publisher);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Thêm nhà xuất bản thành công!";
                return RedirectToAction(nameof(Index));
            }

            return View(viewModel);
        }

        // GET: Publisher/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var publisher = await _context.Publishers.FindAsync(id);
            if (publisher == null)
            {
                return NotFound();
            }

            var viewModel = new PublisherEditViewModel
            {
                PublisherId = publisher.PublisherId,
                Name = publisher.Name,
                Address = publisher.Address
            };

            return View(viewModel);
        }

        // POST: Publisher/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, PublisherEditViewModel viewModel)
        {
            if (id != viewModel.PublisherId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var publisher = await _context.Publishers.FindAsync(id);
                    if (publisher == null)
                    {
                        return NotFound();
                    }

                    publisher.Name = viewModel.Name;
                    publisher.Address = viewModel.Address;
                    publisher.UpdatedAt = DateTime.Now;

                    _context.Update(publisher);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Cập nhật nhà xuất bản thành công!";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!PublisherExists(viewModel.PublisherId))
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

        // POST: Publisher/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var publisher = await _context.Publishers
                .Include(p => p.Books)
                .FirstOrDefaultAsync(p => p.PublisherId == id);

            if (publisher == null)
            {
                return Json(new { success = false, message = "Không tìm thấy nhà xuất bản" });
            }

            // Check if publisher has books
            if (publisher.Books.Any())
            {
                return Json(new { success = false, message = "Không thể xóa nhà xuất bản đang có sách" });
            }

            _context.Publishers.Remove(publisher);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Xóa nhà xuất bản thành công" });
        }

        private bool PublisherExists(int id)
        {
            return _context.Publishers.Any(e => e.PublisherId == id);
        }
    }
}
