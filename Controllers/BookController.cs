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
        public async Task<IActionResult> Index(string searchString, int? authorId, int? publisherId, int pageNumber = 1, int pageSize = 10)
        {
            ViewData["CurrentFilter"] = searchString;
            ViewData["AuthorId"] = authorId;
            ViewData["PublisherId"] = publisherId;

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
                .FirstOrDefaultAsync(m => m.BookId == id);

            if (book == null)
            {
                return NotFound();
            }

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
                LowStockThreshold = book.LowStockThreshold,
                CreatedAt = book.CreatedAt,
                UpdatedAt = book.UpdatedAt,
                IsDeleted = book.IsDeleted
            };

            return View(viewModel);
        }

        // GET: Book/Create
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

                var book = new Book
                {
                    Title = viewModel.Title,
                    ImageUrl = imagePath,
                    AuthorId = viewModel.AuthorId,
                    PublisherId = viewModel.PublisherId,
                    PublicationYear = viewModel.PublicationYear,
                    Price = viewModel.Price,
                    StockQuantity = viewModel.StockQuantity ?? 0,
                    Description = viewModel.Description,
                    LowStockThreshold = viewModel.LowStockThreshold ?? 10,
                    CreatedAt = DateTime.Now,
                    IsDeleted = false
                };

                _context.Add(book);
                await _context.SaveChangesAsync();

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

            return View(viewModel);
        }

        // GET: Book/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var book = await _context.Books.FindAsync(id);
            if (book == null)
            {
                return NotFound();
            }

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
                LowStockThreshold = book.LowStockThreshold,
                IsDeleted = book.IsDeleted,
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
                    var book = await _context.Books.FindAsync(id);
                    if (book == null)
                    {
                        return NotFound();
                    }

                    if (viewModel.ImageFile != null)
                    {
                        book.ImageUrl = await SaveImage(viewModel.ImageFile);
                    }

                    book.Title = viewModel.Title;
                    book.AuthorId = viewModel.AuthorId;
                    book.PublisherId = viewModel.PublisherId;
                    book.PublicationYear = viewModel.PublicationYear;
                    book.Price = viewModel.Price;
                    book.StockQuantity = viewModel.StockQuantity;
                    book.Description = viewModel.Description;
                    book.LowStockThreshold = viewModel.LowStockThreshold;
                    book.UpdatedAt = DateTime.Now;

                    _context.Update(book);
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

            return View(viewModel);
        }

        // POST: Book/Delete/5
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