using BookstoreManagement.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
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

            var result = await _userManager.DeleteAsync(user);
            if (!result.Succeeded)
                return Json(new { success = false, message = "Xóa thất bại: " + string.Join(", ", result.Errors.Select(e => e.Description)) });

            return Json(new { success = true, message = "Xóa nhân viên thành công!" });
        }

    }
}