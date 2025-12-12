using BookstoreManagement.Models;
using BookstoreManagement.ViewModels.Author;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting; // Thêm namespace này để xử lý file/thư mục
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.IO; // Thêm namespace này để xử lý FileStream
using System.Linq;
using System.Threading.Tasks;

namespace BookstoreManagement.Controllers
{
    [Authorize(Roles = "Admin,Manager")]
    public class AuthorController : Controller
    {
        private readonly BookstoreContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment; 

        public AuthorController(BookstoreContext context, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
        }

        // GET: Author
        public async Task<IActionResult> Index(string searchString, string sortBy = "CreatedAt", string sortOrder = "desc", int pageNumber = 1, int pageSize = 10)
        {
            ViewData["CurrentFilter"] = searchString;
            ViewData["SortBy"] = sortBy;
            ViewData["SortOrder"] = sortOrder;

            var authorsQuery = _context.Authors
                .Include(a => a.Books)
                .AsQueryable();

            if (!string.IsNullOrEmpty(searchString))
            {
                authorsQuery = authorsQuery.Where(a =>
                    a.Name.Contains(searchString) ||
                    (a.Bio != null && a.Bio.Contains(searchString)) ||
                    (a.Pseudonym != null && a.Pseudonym.Contains(searchString)));
            }

            // Apply sorting
            authorsQuery = sortBy?.ToLower() switch
            {
                "name" => sortOrder == "asc" ? authorsQuery.OrderBy(a => a.Name) : authorsQuery.OrderByDescending(a => a.Name),
                "bookscount" => sortOrder == "asc" 
                    ? authorsQuery.OrderBy(a => a.Books.Count) 
                    : authorsQuery.OrderByDescending(a => a.Books.Count),
                "createdat" => sortOrder == "asc" 
                    ? authorsQuery.OrderBy(a => a.CreatedAt) 
                    : authorsQuery.OrderByDescending(a => a.CreatedAt),
                _ => authorsQuery.OrderByDescending(a => a.CreatedAt)
            };

            var totalItems = await authorsQuery.CountAsync();
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            var authors = await authorsQuery
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
            if (id == null) return NotFound();

            var author = await _context.Authors
                .Include(a => a.Books)
                .FirstOrDefaultAsync(m => m.AuthorId == id);

            if (author == null) return NotFound();

            var viewModel = new AuthorViewModel
            {
                AuthorId = author.AuthorId,
                Name = author.Name,
                
                Pseudonym = author.Pseudonym,
                DateOfBirth = author.DateOfBirth,
                Nationality = author.Nationality,
                Website = author.Website,
                ImageUrl = author.ImageUrl,

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
                string? uniqueFileName = null;

                if (viewModel.AvatarImage != null)
                {
                    string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images", "authors");
                    
                    if (!Directory.Exists(uploadsFolder)) 
                        Directory.CreateDirectory(uploadsFolder);

                    uniqueFileName = Guid.NewGuid().ToString() + "_" + viewModel.AvatarImage.FileName;
                    string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await viewModel.AvatarImage.CopyToAsync(fileStream);
                    }
                }

                var author = new Author
                {
                    Name = viewModel.Name,
                    
                    Pseudonym = viewModel.Pseudonym,
                    DateOfBirth = viewModel.DateOfBirth,
                    Nationality = viewModel.Nationality,
                    Website = viewModel.Website,
                    ImageUrl = uniqueFileName, 
                    
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

        // --- GET: Author/Edit/5 ---
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var author = await _context.Authors.FindAsync(id);
            if (author == null) return NotFound();

            var viewModel = new AuthorEditViewModel
            {
                AuthorId = author.AuthorId,
                Name = author.Name,
                Pseudonym = author.Pseudonym,
                DateOfBirth = author.DateOfBirth,
                Nationality = author.Nationality,
                Website = author.Website,
                Bio = author.Bio,
                ExistingImageUrl = author.ImageUrl 
            };

            return View(viewModel);
        }

        // --- POST: Author/Edit/5 ---
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, AuthorEditViewModel viewModel)
        {
            if (id != viewModel.AuthorId) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    var authorToUpdate = await _context.Authors.FindAsync(id);
                    if (authorToUpdate == null) return NotFound();

                    authorToUpdate.Name = viewModel.Name;
                    authorToUpdate.Pseudonym = viewModel.Pseudonym;
                    authorToUpdate.DateOfBirth = viewModel.DateOfBirth;
                    authorToUpdate.Nationality = viewModel.Nationality;
                    authorToUpdate.Website = viewModel.Website;
                    authorToUpdate.Bio = viewModel.Bio;
                    authorToUpdate.UpdatedAt = DateTime.Now;

                    // Xử lý logic thay đổi ảnh
                    if (viewModel.AvatarImage != null)
                    {
                        // 1. Xóa ảnh cũ nếu có (để tiết kiệm dung lượng)
                        if (!string.IsNullOrEmpty(authorToUpdate.ImageUrl))
                        {
                            string oldFilePath = Path.Combine(_webHostEnvironment.WebRootPath, "images", "authors", authorToUpdate.ImageUrl);
                            if (System.IO.File.Exists(oldFilePath))
                            {
                                System.IO.File.Delete(oldFilePath);
                            }
                        }

                        // 2. Lưu ảnh mới
                        string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images", "authors");
                        if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

                        string uniqueFileName = Guid.NewGuid().ToString() + "_" + viewModel.AvatarImage.FileName;
                        string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                        using (var fileStream = new FileStream(filePath, FileMode.Create))
                        {
                            await viewModel.AvatarImage.CopyToAsync(fileStream);
                        }

                        // 3. Cập nhật tên ảnh trong DB
                        authorToUpdate.ImageUrl = uniqueFileName;
                    }

                    _context.Update(authorToUpdate);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Cập nhật thông tin thành công!";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!AuthorExists(viewModel.AuthorId)) return NotFound();
                    else throw;
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

            if (author.Books.Any())
            {
                return Json(new { success = false, message = "Không thể xóa tác giả đang có sách" });
            }

            if (!string.IsNullOrEmpty(author.ImageUrl))
            {
                string filePath = Path.Combine(_webHostEnvironment.WebRootPath, "images", "authors", author.ImageUrl);
                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                }
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