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
            // Debug: Log parameters
            Console.WriteLine($"Employee Index - searchString: {searchString}, isActive: {isActive}, roleName: {roleName}, pageNumber: {pageNumber}");
            
            ViewData["CurrentFilter"] = searchString;
            if (isActive.HasValue)
            {
                ViewData["IsActiveFilter"] = isActive.Value.ToString();
            }
            ViewData["RoleFilter"] = roleName;

            // Query để lấy danh sách nhân viên (không phải Admin)
            var query = from u in _context.Users
                        join ur in _context.UserRoles on u.Id equals ur.UserId
                        join r in _context.Roles on ur.RoleId equals r.Id
                        where r.Name != "Admin"
                        select new { User = u, Role = r.Name };

            // Search filter
            if (!string.IsNullOrEmpty(searchString))
            {
                query = query.Where(x => x.User.FullName.Contains(searchString)
                                      || x.User.Email.Contains(searchString)
                                      || x.User.PhoneNumber.Contains(searchString));
            }

            // Status filter
            if (isActive.HasValue)
            {
                query = query.Where(x => x.User.IsActive == isActive.Value);
            }

            // Role filter
            if (!string.IsNullOrEmpty(roleName))
            {
                query = query.Where(x => x.Role == roleName);
            }

            // Get distinct users
            var distinctQuery = query.Select(x => x.User).Distinct();

            // Calculate pagination
            var totalItems = await distinctQuery.CountAsync();
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            // Apply pagination
            var employees = await distinctQuery
                .OrderByDescending(u => u.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.PageNumber = pageNumber;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalItems = totalItems;
            ViewBag.IsActiveParam = isActive;
            ViewBag.RoleParam = roleName;

            // Load roles for filter dropdown
            var availableRoles = await _roleManager.Roles
                .Where(r => r.Name != "Admin")
                .Select(r => r.Name)
                .OrderBy(r => r)
                .ToListAsync();
            
            // Debug: Log available roles and parameter
            Console.WriteLine($"Available roles: {string.Join(", ", availableRoles)}");
            Console.WriteLine($"Selected roleName parameter: '{roleName}'");
            Console.WriteLine($"Request QueryString: {Request.QueryString}");
            
            // Create SelectList with explicit dataValueField and dataTextField
            ViewBag.Roles = new SelectList(availableRoles.Select(r => new { Value = r, Text = r }), "Value", "Text", roleName);

            return View(employees);
        }


        [HttpGet]
        public IActionResult Create()
        {
            var roles = _roleManager.Roles
                .Where(r => r.Name != "Admin")
                .Select(r => new { r.Name })
                .ToList();

            ViewBag.Roles = new SelectList(_roleManager.Roles.Where(r => r.Name != "Admin"), "Name", "Name");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(EmployeeViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = new AppUser
                {
                    UserName = model.Email,
                    Email = model.Email,
                    FullName = model.FullName,
                    PhoneNumber = model.PhoneNumber,
                    Address = model.Address,
                    IsActive = true,
                    IsDefaultPassword = true,
                    CreatedAt = DateTime.Now
                };

                var result = await _userManager.CreateAsync(user, "123");

                if (result.Succeeded)
                {
                    if (!string.IsNullOrEmpty(model.RoleName))
                    {
                        await _userManager.AddToRoleAsync(user, model.RoleName);
                    }
                    TempData["SuccessMessage"] = "Thêm nhân viên thành công!";
                    return RedirectToAction(nameof(Index));
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }
            }

            var rolesList = _roleManager.Roles
                .Where(r => r.Name != "Admin")
                .ToList();
            ViewBag.Roles = new SelectList(rolesList, "Name", "Name");
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(string id)
        {
            if (id == null) return NotFound();

            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            var roles = await _userManager.GetRolesAsync(user);

            var viewModel = new EmployeeViewModel
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                Address = user.Address,
                RoleName = roles.FirstOrDefault(),
                IsActive = user.IsActive
            };

            ViewBag.Roles = new SelectList(_roleManager.Roles.Where(r => r.Name != "Admin"), "Name", "Name");
            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, EmployeeViewModel model)
        {
            if (id != model.Id) return NotFound();

            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByIdAsync(id);
                if (user == null) return NotFound();

                user.FullName = model.FullName;
                user.PhoneNumber = model.PhoneNumber;
                user.Address = model.Address;
                user.IsActive = model.IsActive;
                user.UpdatedAt = DateTime.Now;

                // Nếu cho sửa Email thì bỏ comment dòng dưới 
                // user.Email = model.Email; 
                // user.UserName = model.Email;

                var result = await _userManager.UpdateAsync(user);

                if (result.Succeeded)
                {
                    var currentRoles = await _userManager.GetRolesAsync(user);
                    if (!currentRoles.Contains(model.RoleName))
                    {
                        await _userManager.RemoveFromRolesAsync(user, currentRoles);
                        await _userManager.AddToRoleAsync(user, model.RoleName);
                    }

                    TempData["SuccessMessage"] = "Cập nhật nhân viên thành công!";
                    return RedirectToAction(nameof(Index));
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }
            }

            ViewBag.Roles = new SelectList(_roleManager.Roles.Where(r => r.Name != "Admin"), "Name", "Name");
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user != null)
            {
                user.IsActive = false;
                user.UpdatedAt = DateTime.Now;
                await _userManager.UpdateAsync(user);

                TempData["SuccessMessage"] = "Đã khóa tài khoản nhân viên.";
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Details(string id)
        {
            if (id == null) return NotFound();

            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            var roles = await _userManager.GetRolesAsync(user);

            var viewModel = new EmployeeViewModel
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                Address = user.Address,
                RoleName = roles.FirstOrDefault() ?? "Chưa có",
                IsActive = user.IsActive
            };

            return View(viewModel);
        }

    }
}