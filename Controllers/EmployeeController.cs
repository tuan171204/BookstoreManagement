using BookstoreManagement.Models;
using BookstoreManagement.ViewModels.Employee;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace BookstoreManagement.Controllers
{

    [Authorize(Roles = "Admin,Manager")] // chỉ role Admin hoặc Manager được truy cập
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
        [Authorize]
        public async Task<IActionResult> Index()
        {
            TempData["CurrentFeature"] = "Employee";
            var employees = await (from u in _context.Users
                                   join ur in _context.UserRoles on u.Id equals ur.UserId
                                   join r in _context.Roles on ur.RoleId equals r.Id
                                   where r.Name != "Admin"
                                   select u)
                      .Distinct()
                      .ToListAsync();
            ViewBag.Roles = _roleManager.Roles
                            .Select(r => new SelectListItem
                            {
                                Value = r.Name,
                                Text = r.Name == "Employee" ? "Nhân viên" : r.Name == "Manager" ? "Quản lý" : r.Name
                            })
                            .ToList();
            return View(employees);
        }


        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(AppUser model, string selectedRole)
        {
            // Nếu sai validate thì trả về form hiện tại
            if (!ModelState.IsValid)
            {
                return PartialView("_CreateForm", model);
            }

            // Mặc định, nếu không chọn role thì gán Employee
            if (string.IsNullOrEmpty(selectedRole))
                selectedRole = "Employee";

            // Nếu chưa có role
            if (!await _roleManager.RoleExistsAsync(selectedRole))
            {
                await _roleManager.CreateAsync(new AppRole
                {
                    Name = selectedRole,
                    Description = selectedRole == "Manager" ? "Quản lý" : "Nhân viên",
                    CreatedAt = DateTime.Now
                });
            }

            string userEmail = model.Email ?? string.Empty;
            var employee = new AppUser
            {
                UserName = model.Email,
                Email = model.Email,
                FullName = model.FullName,
                PhoneNumber = model.PhoneNumber,
                Address = model.Address,
                IsActive = true,
                CreatedAt = DateTime.Now
            };

            var result = await _userManager.CreateAsync(employee, "123");
            if (!result.Succeeded)
                return Json(new { success = false, message = string.Join(", ", result.Errors.Select(e => e.Description)) });

            await _userManager.AddToRoleAsync(employee, selectedRole);

            return Json(new { success = true, message = "Thêm nhân viên thành công!" });
        }

        [HttpPost]
        [Authorize(Roles = "Admin,Manager")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
                return Json(new { success = false, message = "Không tìm thấy nhân viên!" });

            if (user.UserName == User.Identity?.Name)
                return Json(new { success = false, message = "Bạn không thể tự xóa chính mình!" });

            user.IsActive = false;
            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
                return Json(new { success = false, message = "Xóa thất bại: " + string.Join(", ", result.Errors.Select(e => e.Description)) });

            return Json(new { success = true, message = "Xóa nhân viên thành công!" });
        }

        public async Task<IActionResult> GetUpdateForm(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            var roles = await _userManager.GetRolesAsync(user);

            var vm = new EmployeeViewModel
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                Address = user.Address,
                RoleName = roles.FirstOrDefault() ?? "Employee",
                IsActive = user.IsActive
            };

            ViewBag.Roles = _roleManager.Roles
                            .Select(r => new SelectListItem
                            {
                                Value = r.Name,
                                Text = r.Name == "Employee" ? "Nhân viên" : r.Name == "Manager" ? "Quản lý" : r.Name
                            })
                            .ToList();

            return PartialView("~/Views/Employee/_UpdateForm.cshtml", vm);
        }

        [HttpPost]
        [Authorize(Roles = "Admin,Manager")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update(EmployeeViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return PartialView("_UpdateForm", model);
            }

            var user = await _userManager.FindByIdAsync(model.Id);
            if (user == null) return NotFound();

            user.FullName = model.FullName;
            user.Email = model.Email;
            user.PhoneNumber = model.PhoneNumber;
            user.Address = model.Address;
            user.IsActive = model.IsActive;
            user.UpdatedAt = DateTime.Now;

            await _userManager.UpdateAsync(user);

            // cập nhật vai trò
            var currentRoles = await _userManager.GetRolesAsync(user);
            await _userManager.RemoveFromRolesAsync(user, currentRoles);
            await _userManager.AddToRoleAsync(user, model.RoleName);

            return Json(new { success = true, message = "Cập nhật nhân viên thành công!" });
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GetList(string inputTxt, string selectedRole, string selectedStatus)
        {

            TempData["CurrentFeature"] = "Employee";

            var query = from u in _context.Users
                        join ur in _context.UserRoles on u.Id equals ur.UserId
                        join r in _context.Roles on ur.RoleId equals r.Id
                        where r.Name != "Admin"
                        select u;

            // Lọc theo input
            if (!string.IsNullOrEmpty(inputTxt))
            {
                query = query.Where(u => (u.Email.Contains("@") && u.Email.Substring(0, u.Email.IndexOf("@")).Contains(inputTxt))
                                       || u.FullName.Contains(inputTxt)
                                       || u.Address.Contains(inputTxt));
            }

            // Lọc theo role
            if (!string.IsNullOrEmpty(selectedRole))
            {
                query = query.Where(u => _context.UserRoles
                                .Where(ur => ur.UserId == u.Id)
                                .Join(_context.Roles, ur => ur.RoleId, r => r.Id, (ur, r) => r.Name)
                                .Any(rn => rn == selectedRole));
            }

            // Lọc theo trạng thái
            if (!string.IsNullOrEmpty(selectedStatus))
            {
                bool isActive = selectedStatus == "1";
                query = query.Where(u => u.IsActive == isActive);
            }

            var employees = await query.Distinct().ToListAsync();

            ViewBag.Roles = _roleManager.Roles
                            .Select(r => new SelectListItem
                            {
                                Value = r.Name,
                                Text = r.Name == "Employee" ? "Nhân viên" :
                                       r.Name == "Manager" ? "Quản lý" : r.Name
                            })
                            .ToList();

            return View("Index", employees);
        }

    }
}