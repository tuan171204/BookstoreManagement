using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BookstoreManagement.Models;

namespace BookstoreManagement.Controllers
{
    [Authorize]
    public class SupplierController : Controller
    {
        private readonly BookstoreContext _context;

        public SupplierController(BookstoreContext context)
        {
            _context = context;
        }

        // TRANG DANH SÁCH (INDEX)
        [HttpGet]
        public async Task<IActionResult> Index(string searchString)
        {
            ViewData["CurrentFilter"] = searchString;

            var query = _context.Suppliers.AsQueryable();

            if (!string.IsNullOrEmpty(searchString))
            {
                query = query.Where(s => s.Name.Contains(searchString)
                                      || (s.ContactInfo != null && s.ContactInfo.Contains(searchString)));
            }

            return View(await query.ToListAsync());
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