using BookstoreManagement.Models;
using BookstoreManagement.ViewModels.Publisher;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BookstoreManagement.Controllers
{
    [Authorize(Roles = "Admin,Manager")]
    public class PublisherController : Controller
    {
        private readonly BookstoreContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public PublisherController(BookstoreContext context, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
        }

        // GET: Publisher
        public async Task<IActionResult> Index(string searchString, string sortBy = "CreatedAt", string sortOrder = "desc", int pageNumber = 1, int pageSize = 10)
        {
            ViewData["CurrentFilter"] = searchString;
            ViewData["SortBy"] = sortBy;
            ViewData["SortOrder"] = sortOrder;
            
            var publishersQuery = _context.Publishers
                .Include(p => p.Books)
                .AsQueryable();

            if (!string.IsNullOrEmpty(searchString))
            {
                publishersQuery = publishersQuery.Where(p =>
                    p.Name.Contains(searchString) ||
                    (p.Address != null && p.Address.Contains(searchString)) ||
                    (p.Phone != null && p.Phone.Contains(searchString)));
            }

            // Apply sorting
            publishersQuery = sortBy?.ToLower() switch
            {
                "name" => sortOrder == "asc" 
                    ? publishersQuery.OrderBy(p => p.Name) 
                    : publishersQuery.OrderByDescending(p => p.Name),
                "address" => sortOrder == "asc" 
                    ? publishersQuery.OrderBy(p => p.Address ?? "") 
                    : publishersQuery.OrderByDescending(p => p.Address ?? ""),
                "bookscount" => sortOrder == "asc" 
                    ? publishersQuery.OrderBy(p => p.Books.Count) 
                    : publishersQuery.OrderByDescending(p => p.Books.Count),
                "createdat" => sortOrder == "asc" 
                    ? publishersQuery.OrderBy(p => p.CreatedAt) 
                    : publishersQuery.OrderByDescending(p => p.CreatedAt),
                _ => publishersQuery.OrderByDescending(p => p.CreatedAt)
            };

            var totalItems = await publishersQuery.CountAsync();
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            var publishers = await publishersQuery
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(p => new PublisherViewModel
                {
                    PublisherId = p.PublisherId,
                    Name = p.Name,
                    Address = p.Address,
                    Phone = p.Phone,
                    Email = p.Email,
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
            if (id == null) return NotFound();

            var publisher = await _context.Publishers
                .Include(p => p.Books)
                .FirstOrDefaultAsync(m => m.PublisherId == id);

            if (publisher == null) return NotFound();

            var viewModel = new PublisherViewModel
            {
                PublisherId = publisher.PublisherId,
                Name = publisher.Name,
                Address = publisher.Address,
                Phone = publisher.Phone,
                Email = publisher.Email,
                Website = publisher.Website,
                ImageUrl = publisher.ImageUrl,
                Description = publisher.Description,
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
                string? uniqueFileName = null;
                if (viewModel.LogoImage != null)
                {
                    string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images", "publishers");
                    if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);
                    uniqueFileName = Guid.NewGuid().ToString() + "_" + viewModel.LogoImage.FileName;
                    string filePath = Path.Combine(uploadsFolder, uniqueFileName);
                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await viewModel.LogoImage.CopyToAsync(fileStream);
                    }
                }

                var publisher = new Publisher
                {
                    Name = viewModel.Name,
                    Address = viewModel.Address,
                    Phone = viewModel.Phone,
                    Email = viewModel.Email,
                    Website = viewModel.Website,
                    Description = viewModel.Description,
                    ImageUrl = uniqueFileName,
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
            if (id == null) return NotFound();

            var publisher = await _context.Publishers.FindAsync(id);
            if (publisher == null) return NotFound();

            var viewModel = new PublisherEditViewModel
            {
                PublisherId = publisher.PublisherId,
                Name = publisher.Name,
                Address = publisher.Address,
                Phone = publisher.Phone,
                Email = publisher.Email,
                Website = publisher.Website,
                Description = publisher.Description,
                ExistingLogoUrl = publisher.ImageUrl
            };

            return View(viewModel);
        }

        // POST: Publisher/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, PublisherEditViewModel viewModel)
        {
            if (id != viewModel.PublisherId) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    var publisher = await _context.Publishers.FindAsync(id);
                    if (publisher == null) return NotFound();

                    publisher.Name = viewModel.Name;
                    publisher.Address = viewModel.Address;
                    publisher.Phone = viewModel.Phone;
                    publisher.Email = viewModel.Email;
                    publisher.Website = viewModel.Website;
                    publisher.Description = viewModel.Description;
                    publisher.UpdatedAt = DateTime.Now;

                    if (viewModel.LogoImage != null)
                    {
                        if (!string.IsNullOrEmpty(publisher.ImageUrl))
                        {
                            string oldPath = Path.Combine(_webHostEnvironment.WebRootPath, "images", "publishers", publisher.ImageUrl);
                            if (System.IO.File.Exists(oldPath)) System.IO.File.Delete(oldPath);
                        }

                        string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images", "publishers");
                        if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);
                        string uniqueFileName = Guid.NewGuid().ToString() + "_" + viewModel.LogoImage.FileName;
                        string filePath = Path.Combine(uploadsFolder, uniqueFileName);
                        using (var fileStream = new FileStream(filePath, FileMode.Create))
                        {
                            await viewModel.LogoImage.CopyToAsync(fileStream);
                        }
                        publisher.ImageUrl = uniqueFileName;
                    }

                    _context.Update(publisher);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Cập nhật thành công!";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!PublisherExists(viewModel.PublisherId)) return NotFound();
                    else throw;
                }
            }
            return View(viewModel);
        }

        // POST: Publisher/Delete 
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var publisher = await _context.Publishers.Include(p => p.Books).FirstOrDefaultAsync(p => p.PublisherId == id);
            if (publisher == null) return Json(new { success = false, message = "Không tìm thấy" });
            if (publisher.Books.Any()) return Json(new { success = false, message = "Không thể xóa NXB đang có sách" });

            if (!string.IsNullOrEmpty(publisher.ImageUrl))
            {
                string filePath = Path.Combine(_webHostEnvironment.WebRootPath, "images", "publishers", publisher.ImageUrl);
                if (System.IO.File.Exists(filePath)) System.IO.File.Delete(filePath);
            }

            _context.Publishers.Remove(publisher);
            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Xóa thành công" });
        }

        private bool PublisherExists(int id) => _context.Publishers.Any(e => e.PublisherId == id);
    }
}