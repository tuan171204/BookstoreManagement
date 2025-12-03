using BookstoreManagement.Models;
using BookstoreManagement.ViewModels.Role;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BookstoreManagement.Controllers
{
    // Thường chỉ Admin mới được quản lý quyền
    [Authorize(Roles = "Admin")]
    public class RoleController : Controller
    {
        private readonly RoleManager<AppRole> _roleManager;
        private readonly BookstoreContext _context;

        public RoleController(RoleManager<AppRole> roleManager, BookstoreContext context)
        {
            _roleManager = roleManager;
            _context = context;
        }

        // 1. GET: Danh sách quyền
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            TempData["CurrentFeature"] = "Role"; // Active menu
            var roles = await _roleManager.Roles.OrderBy(r => r.Name).ToListAsync();

            // Map sang ViewModel để hiển thị (nếu cần) hoặc dùng Model trực tiếp
            // Ở đây dùng Model AppRole trực tiếp cho Index vì đơn giản
            return View(roles);
        }

        // 2. GET: Tạo mới
        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        // POST: Tạo mới
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(RoleViewModel model)
        {
            if (ModelState.IsValid)
            {
                // Kiểm tra xem role đã tồn tại chưa
                if (await _roleManager.RoleExistsAsync(model.Name))
                {
                    ModelState.AddModelError("Name", "Tên chức vụ này đã tồn tại.");
                    return View(model);
                }

                var role = new AppRole
                {
                    Name = model.Name,
                    Description = model.Description,
                    Salary = model.Salary,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                };

                var result = await _roleManager.CreateAsync(role);
                if (result.Succeeded)
                {
                    TempData["SuccessMessage"] = "Thêm chức vụ mới thành công!";
                    return RedirectToAction(nameof(Index));
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }
            }
            return View(model);
        }

        // 3. GET: Sửa
        [HttpGet]
        public async Task<IActionResult> Edit(string id)
        {
            if (string.IsNullOrEmpty(id)) return NotFound();

            var role = await _roleManager.FindByIdAsync(id);
            if (role == null) return NotFound();

            // Lấy danh sách tất cả quyền trong DB
            var allPermissions = await _context.Permissions.OrderBy(p => p.PermissionName).ToListAsync();

            // Lấy danh sách quyền hiện tại của Role này
            var currentPermissionIds = await _context.RolePermissions
                .Where(rp => rp.RoleId == id)
                .Select(rp => rp.PermissionId)
                .ToListAsync();

            var model = new RoleViewModel
            {
                Id = role.Id,
                Name = role.Name ?? "",
                Description = role.Description,
                Salary = role.Salary,

                // Gán dữ liệu vào ViewModel
                AllPermissions = allPermissions,
                SelectedPermissionIds = currentPermissionIds
            };

            return View(model);
        }

        // POST: Sửa
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, RoleViewModel model)
        {
            if (id != model.Id) return NotFound();

            // Load lại danh sách permission nếu validate lỗi để không bị null view
            if (!ModelState.IsValid)
            {
                model.AllPermissions = await _context.Permissions.OrderBy(p => p.PermissionName).ToListAsync();
                return View(model);
            }

            var role = await _roleManager.FindByIdAsync(id);
            if (role == null) return NotFound();

            // 1. Cập nhật thông tin cơ bản
            role.Name = model.Name;
            role.Description = model.Description;
            role.Salary = model.Salary;
            role.UpdatedAt = DateTime.Now;

            var result = await _roleManager.UpdateAsync(role);
            if (result.Succeeded)
            {
                // 2. Xử lý lưu quyền (Transaction cho an toàn)
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    // A. Xóa hết quyền cũ của Role này
                    var oldPermissions = await _context.RolePermissions
                        .Where(rp => rp.RoleId == id)
                        .ToListAsync();
                    _context.RolePermissions.RemoveRange(oldPermissions);
                    await _context.SaveChangesAsync();

                    // B. Thêm quyền mới được chọn
                    if (model.SelectedPermissionIds != null && model.SelectedPermissionIds.Any())
                    {
                        var newPermissions = model.SelectedPermissionIds.Select(permId => new RolePermission
                        {
                            RoleId = id,
                            PermissionId = permId
                        });
                        await _context.RolePermissions.AddRangeAsync(newPermissions);
                        await _context.SaveChangesAsync();
                    }

                    await transaction.CommitAsync();
                    TempData["SuccessMessage"] = "Cập nhật chức vụ và phân quyền thành công!";
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    ModelState.AddModelError("", "Lỗi lưu quyền: " + ex.Message);
                    // Load lại list nếu lỗi
                    model.AllPermissions = await _context.Permissions.ToListAsync();
                    return View(model);
                }

                return RedirectToAction(nameof(Index));
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error.Description);
            }

            // Load lại list nếu lỗi Identity
            model.AllPermissions = await _context.Permissions.ToListAsync();
            return View(model);
        }

        // 4. POST: Xóa
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string id)
        {
            var role = await _roleManager.FindByIdAsync(id);
            if (role != null)
            {
                // Lưu ý: Nếu xóa Role đang có User gán vào thì Identity có thể báo lỗi hoặc User đó mất quyền.
                // Nên kiểm tra trước khi xóa nếu cần kỹ hơn.
                var result = await _roleManager.DeleteAsync(role);
                if (result.Succeeded)
                {
                    TempData["SuccessMessage"] = "Đã xóa chức vụ thành công.";
                }
                else
                {
                    TempData["ErrorMessage"] = "Lỗi khi xóa: " + string.Join(", ", result.Errors.Select(e => e.Description));
                }
            }
            return RedirectToAction(nameof(Index));
        }
    }
}