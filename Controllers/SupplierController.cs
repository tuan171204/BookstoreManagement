using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BookstoreManagement.Models;

namespace BookstoreManagement.Controllers
{
    [Authorize(Roles = "Admin,Manager")]
    public class SupplierController : Controller
    {
        private readonly BookstoreContext _context;

        public SupplierController(BookstoreContext context)
        {
            _context = context;
        }

        // TRANG DANH SÁCH (INDEX)
        [HttpGet]
        public async Task<IActionResult> Index(string searchString, bool? isActive, string sortBy = "CreatedAt", string sortOrder = "desc", int pageNumber = 1, int pageSize = 10)
        {
            ViewData["CurrentFilter"] = searchString;
            ViewData["SortBy"] = sortBy;
            ViewData["SortOrder"] = sortOrder;
            if (isActive.HasValue)
            {
                ViewData["IsActiveFilter"] = isActive.Value.ToString();
            }

            var query = _context.Suppliers.AsQueryable();

            // Search filter
            if (!string.IsNullOrEmpty(searchString))
            {
                query = query.Where(s => s.Name.Contains(searchString)
                                      || (s.ContactInfo != null && s.ContactInfo.Contains(searchString))
                                      || (s.Address != null && s.Address.Contains(searchString)));
            }

            // Status filter
            if (isActive.HasValue)
            {
                query = query.Where(s => s.IsActive == isActive.Value);
            }

            // Apply sorting
            query = sortBy?.ToLower() switch
            {
                "name" => sortOrder == "asc" 
                    ? query.OrderBy(s => s.Name) 
                    : query.OrderByDescending(s => s.Name),
                "contactinfo" => sortOrder == "asc" 
                    ? query.OrderBy(s => s.ContactInfo ?? "") 
                    : query.OrderByDescending(s => s.ContactInfo ?? ""),
                "address" => sortOrder == "asc" 
                    ? query.OrderBy(s => s.Address ?? "") 
                    : query.OrderByDescending(s => s.Address ?? ""),
                "isactive" => sortOrder == "asc" 
                    ? query.OrderBy(s => s.IsActive) 
                    : query.OrderByDescending(s => s.IsActive),
                "createdat" => sortOrder == "asc" 
                    ? query.OrderBy(s => s.CreatedAt) 
                    : query.OrderByDescending(s => s.CreatedAt),
                _ => query.OrderByDescending(s => s.CreatedAt)
            };

            // Calculate pagination
            var totalItems = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            // Apply pagination
            var suppliers = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.PageNumber = pageNumber;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalItems = totalItems;
            ViewBag.IsActiveParam = isActive;

            return View(suppliers);
        }

        // TRANG THÊM MỚI (CREATE)
        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Supplier supplier)
        {
            if (ModelState.IsValid)
            {
                supplier.CreatedAt = DateTime.Now;
                supplier.IsActive = true;

                _context.Add(supplier);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Thêm nhà cung cấp thành công!";
                return RedirectToAction(nameof(Index));
            }
            return View(supplier);
        }

        // TRANG CHỈNH SỬA (EDIT)
        [HttpGet]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var supplier = await _context.Suppliers.FindAsync(id);
            if (supplier == null) return NotFound();

            return View(supplier);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Supplier supplier)
        {
            if (id != supplier.SupplierId) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    supplier.UpdatedAt = DateTime.Now;

                    _context.Update(supplier);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Cập nhật thành công!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Suppliers.Any(e => e.SupplierId == id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(supplier);
        }

        // CHỨC NĂNG XÓA (DELETE)
        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var supplier = await _context.Suppliers.FindAsync(id);
            if (supplier != null)
            {
                // Cách 1: Xóa hẳn khỏi Database (Cẩn thận mất dữ liệu lịch sử nhập hàng)
                // _context.Suppliers.Remove(supplier);

                // Cách 2: Xóa mềm (Soft Delete) - Chỉ ẩn đi
                // supplier.IsDeleted = true; 
                // _context.Update(supplier);

                // Tạm thời dùng Cách
                _context.Suppliers.Remove(supplier);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Đã xóa nhà cung cấp!";
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var supplier = await _context.Suppliers
                .Include(s => s.SupplierBooks)
                    .ThenInclude(sb => sb.Book)
                .FirstOrDefaultAsync(m => m.SupplierId == id);

            if (supplier == null) return NotFound();

            return View(supplier);
        }
    }
}