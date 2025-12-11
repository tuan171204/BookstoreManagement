using BookstoreManagement.Models;
using BookstoreManagement.ViewModels.Employee;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace BookstoreManagement.Controllers
{

    [Authorize(Roles = "Admin,Manager")]
    public class EmployeeController : Controller
    {
        private readonly BookstoreContext _context;
        private readonly UserManager<AppUser> _userManager;
        private readonly RoleManager<AppRole> _roleManager;

        public EmployeeController(BookstoreContext context,
            UserManager<AppUser> userManager,
            RoleManager<AppRole> roleManager)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
        }


        [HttpGet]
        public async Task<IActionResult> Index(string searchString, bool? isActive, string? roleName, int pageNumber = 1, int pageSize = 10)
        {
            ViewData["CurrentFilter"] = searchString;
            ViewBag.IsActiveParam = isActive;

            // Truy vấn từ bảng Employees (không phải Users)
            var query = _context.Employees.Include(e => e.AppUser).AsQueryable();

            if (!string.IsNullOrEmpty(searchString))
            {
                query = query.Where(e => e.FullName.Contains(searchString)
                                      || e.PhoneNumber.Contains(searchString)
                                      || (e.Email != null && e.Email.Contains(searchString)));
            }

            if (isActive.HasValue)
            {
                query = query.Where(e => e.IsActive == isActive.Value);
            }

            var totalItems = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            var employees = await query
                .OrderByDescending(e => e.HireDate)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(e => new EmployeeViewModel
                {
                    Id = e.EmployeeId,
                    FullName = e.FullName,
                    PhoneNumber = e.PhoneNumber,
                    Email = e.Email,
                    Address = e.Address,
                    Salary = e.Salary,
                    HireDate = e.HireDate,
                    IsActive = e.IsActive,
                    AccountId = e.AccountId, // Để hiển thị trạng thái tài khoản
                    AccountUsername = e.AppUser != null ? e.AppUser.UserName : null
                })
                .ToListAsync();

            ViewBag.PageNumber = pageNumber;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalItems = totalItems;

            return View(employees);
        }


        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(EmployeeViewModel model)
        {
            if (ModelState.IsValid)
            {
                // Kiểm tra trùng SĐT (vì SĐT là unique trong cấu hình DB mới)
                if (await _context.Employees.AnyAsync(e => e.PhoneNumber == model.PhoneNumber))
                {
                    ModelState.AddModelError("PhoneNumber", "Số điện thoại này đã được sử dụng bởi nhân viên khác.");
                    return View(model);
                }

                var employee = new Employee
                {
                    FullName = model.FullName,
                    PhoneNumber = model.PhoneNumber,
                    Email = model.Email,
                    Address = model.Address,
                    HireDate = model.HireDate,
                    Salary = model.Salary,
                    IsActive = true
                };

                _context.Employees.Add(employee);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Thêm hồ sơ nhân viên thành công!";
                return RedirectToAction(nameof(Index));
            }
            return View(model);
        }

        // 3. CHỈNH SỬA HỒ SƠ
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var employee = await _context.Employees.FindAsync(id);
            if (employee == null) return NotFound();

            var viewModel = new EmployeeViewModel
            {
                Id = employee.EmployeeId,
                FullName = employee.FullName,
                PhoneNumber = employee.PhoneNumber,
                Email = employee.Email,
                Address = employee.Address,
                HireDate = employee.HireDate,
                Salary = employee.Salary,
                IsActive = employee.IsActive
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, EmployeeViewModel model)
        {
            if (id != model.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    var employee = await _context.Employees.FindAsync(id);
                    if (employee == null) return NotFound();

                    // Kiểm tra trùng SĐT nếu có thay đổi
                    if (employee.PhoneNumber != model.PhoneNumber &&
                        await _context.Employees.AnyAsync(e => e.PhoneNumber == model.PhoneNumber))
                    {
                        ModelState.AddModelError("PhoneNumber", "Số điện thoại này đã tồn tại.");
                        return View(model);
                    }

                    employee.FullName = model.FullName;
                    employee.PhoneNumber = model.PhoneNumber;
                    employee.Email = model.Email;
                    employee.Address = model.Address;
                    employee.HireDate = model.HireDate;
                    employee.Salary = model.Salary;
                    employee.IsActive = model.IsActive;

                    _context.Update(employee);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Employees.Any(e => e.EmployeeId == id)) return NotFound();
                    else throw;
                }

                TempData["SuccessMessage"] = "Cập nhật hồ sơ thành công!";
                return RedirectToAction(nameof(Index));
            }
            return View(model);
        }

        // 4. KHÓA/XÓA NHÂN VIÊN
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var employee = await _context.Employees.FindAsync(id);
            if (employee != null)
            {
                // Soft delete: Chỉ set IsActive = false
                employee.IsActive = false;

                // Tùy chọn: Nếu muốn khóa luôn tài khoản đăng nhập (nếu có)
                if (!string.IsNullOrEmpty(employee.AccountId))
                {
                    var user = await _userManager.FindByIdAsync(employee.AccountId);
                    if (user != null)
                    {
                        user.IsActive = false;
                        await _userManager.UpdateAsync(user);
                    }
                }

                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Đã khóa hồ sơ nhân viên.";
            }
            return RedirectToAction(nameof(Index));
        }

        // 5. XEM CHI TIẾT
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var employee = await _context.Employees
                .Include(e => e.AppUser) // Include để lấy thông tin tài khoản nếu có
                .FirstOrDefaultAsync(m => m.EmployeeId == id);

            if (employee == null) return NotFound();

            var viewModel = new EmployeeViewModel
            {
                Id = employee.EmployeeId,
                FullName = employee.FullName,
                PhoneNumber = employee.PhoneNumber,
                Email = employee.Email,
                Address = employee.Address,
                HireDate = employee.HireDate,
                Salary = employee.Salary,
                IsActive = employee.IsActive,
                AccountId = employee.AccountId,
                AccountUsername = employee.AppUser?.UserName
            };

            return View(viewModel);
        }


        // EmployeeController.cs

        // ... (Các code cũ giữ nguyên)

        // ============================================================
        // CẤP TÀI KHOẢN CHO NHÂN VIÊN
        // ============================================================

        [HttpGet]
        public async Task<IActionResult> GrantAccount(int id)
        {
            var employee = await _context.Employees.FindAsync(id);
            if (employee == null) return NotFound();

            // Kiểm tra nếu đã có tài khoản rồi thì chặn lại
            if (!string.IsNullOrEmpty(employee.AccountId))
            {
                TempData["ErrorMessage"] = "Nhân viên này đã có tài khoản rồi.";
                return RedirectToAction(nameof(Index));
            }

            var model = new GrantAccountViewModel
            {
                EmployeeId = employee.EmployeeId,
                EmployeeName = employee.FullName,
                // Gợi ý email từ hồ sơ nhân viên (nếu có), nếu không thì để trống
                Email = employee.Email ?? ""
            };

            // Lấy danh sách Role để admin chọn
            ViewBag.Roles = new SelectList(_roleManager.Roles.Select(r => r.Name).ToList());

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GrantAccount(GrantAccountViewModel model)
        {
            if (ModelState.IsValid)
            {
                var employee = await _context.Employees.FindAsync(model.EmployeeId);
                if (employee == null) return NotFound();

                // 1. Kiểm tra Email đã tồn tại trong bảng Users chưa
                var existingUser = await _userManager.FindByEmailAsync(model.Email);
                if (existingUser != null)
                {
                    ModelState.AddModelError("Email", "Email này đã được sử dụng bởi tài khoản khác.");
                    ViewBag.Roles = new SelectList(_roleManager.Roles.Select(r => r.Name).ToList());
                    return View(model);
                }

                // 2. Tạo AppUser
                var user = new AppUser
                {
                    UserName = model.Email,
                    Email = model.Email,
                    FullName = employee.FullName,
                    PhoneNumber = employee.PhoneNumber,
                    Address = employee.Address,
                    IsActive = true,
                    IsDefaultPassword = true, // Đánh dấu là mật khẩu do admin cấp (để sau này bắt đổi)
                    CreatedAt = DateTime.Now
                };

                var result = await _userManager.CreateAsync(user, model.Password);

                if (result.Succeeded)
                {
                    // 3. Gán Role
                    if (!string.IsNullOrEmpty(model.RoleName))
                    {
                        await _userManager.AddToRoleAsync(user, model.RoleName);
                    }

                    // 4. Liên kết ngược lại bảng Employee
                    employee.AccountId = user.Id;

                    // Cập nhật lại email trong hồ sơ nhân viên cho khớp với tài khoản (nếu muốn đồng bộ)
                    employee.Email = model.Email;

                    _context.Update(employee);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = $"Đã cấp tài khoản '{user.UserName}' thành công!";
                    return RedirectToAction(nameof(Index));
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            ViewBag.Roles = new SelectList(_roleManager.Roles.Select(r => r.Name).ToList());
            return View(model);
        }

        // ============================================================
        // ADMIN RESET MẬT KHẨU (Force Reset)
        // ============================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AdminResetPassword(int employeeId, string newPassword)
        {
            // 1. Tìm nhân viên
            var employee = await _context.Employees.FindAsync(employeeId);
            if (employee == null || string.IsNullOrEmpty(employee.AccountId))
            {
                TempData["ErrorMessage"] = "Nhân viên không tồn tại hoặc chưa có tài khoản.";
                return RedirectToAction(nameof(Index));
            }

            // 2. Tìm tài khoản AppUser tương ứng
            var user = await _userManager.FindByIdAsync(employee.AccountId);
            if (user == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy tài khoản liên kết.";
                return RedirectToAction(nameof(Index));
            }

            // 3. Thực hiện Reset mật khẩu
            // Cách làm: Tạo token reset -> Gọi hàm ResetPassword
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var result = await _userManager.ResetPasswordAsync(user, token, newPassword);

            if (result.Succeeded)
            {
                // (Tùy chọn) Đánh dấu là mật khẩu mặc định để bắt user đổi lần sau
                user.IsDefaultPassword = true;
                await _userManager.UpdateAsync(user);

                TempData["SuccessMessage"] = $"Đã đặt lại mật khẩu cho {employee.FullName} thành công!";
            }
            else
            {
                TempData["ErrorMessage"] = "Lỗi: " + string.Join(", ", result.Errors.Select(e => e.Description));
            }

            return RedirectToAction(nameof(Index));
        }
    }
}