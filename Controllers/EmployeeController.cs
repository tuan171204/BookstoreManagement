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
        public async Task<IActionResult> Index(string searchString)
        {
            ViewData["CurrentFilter"] = searchString;

            var query = from u in _context.Users
                        join ur in _context.UserRoles on u.Id equals ur.UserId
                        join r in _context.Roles on ur.RoleId equals r.Id
                        where r.Name != "Admin"
                        select u;

            if (!string.IsNullOrEmpty(searchString))
            {
                query = query.Where(u => u.FullName.Contains(searchString)
                                      || u.Email.Contains(searchString));
            }

            var employees = await query.Distinct().ToListAsync();
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