using BookstoreManagement.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Identity.UI.V4.Pages.Account.Internal;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using BookstoreManagement.ViewModels.Account;
using Microsoft.EntityFrameworkCore;

namespace BookstoreManagement.Controllers
{
    public class AccountController : Controller
    {
        // Xử lý đăng nhập của Identity
        private readonly SignInManager<AppUser> _signInManager;

        // Quản lý người dùng của Identity
        private readonly UserManager<AppUser> _userManager;

        private readonly IEmailSender _emailSender;

        private readonly BookstoreContext _context;

        public AccountController(SignInManager<AppUser> signInManager, UserManager<AppUser> userManager, IEmailSender emailSender, BookstoreContext context)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _emailSender = emailSender;
            _context = context;
        }

        // ------------------ LOGIN & LOGOUT ------------------
        // Hiển thị trang đăng nhập
        [HttpGet]
        [AllowAnonymous] // Cho phép người chưa đăng nhập truy cập
        public IActionResult Login()
        {
            return View();
        }

        // Submit form đăng nhập
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken] // Bảo vệ tấn công CSRF
        public async Task<IActionResult> Login(string email, string password, bool rememberMe)
        {
            if (!ModelState.IsValid) // Các validation trong models không hợp lệ
            {
                return View();
            }

            var result = await _signInManager.PasswordSignInAsync(
                userName: email,
                password: password,
                isPersistent: rememberMe, // true = lưu cookie sau khi tắt trình duyệt
                lockoutOnFailure: false
            );

            if (result.Succeeded)
            {
                var user = await _userManager.FindByEmailAsync(email);

                if (user != null && user.IsDefaultPassword)
                {
                    return RedirectToAction("Index", "Setting");
                }

                return RedirectToAction("Index", "Book");
            }

            if (result.IsLockedOut)
            {
                ModelState.AddModelError("", "Tài khoản bị khóa tạm thời");
            }
            else
            {
                ModelState.AddModelError("", "Đăng nhập thất bại. Vui lòng kiểm tra lại");
            }

            return View();

        }

        // Đăng xuất
        [HttpPost]
        [Authorize] // Chỉ cho phép người đăng nhập thực hiện
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Login", "Account");
        }

        [HttpPost]
        [Authorize] // Chỉ cho phép người đăng nhập thực hiện
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ClientLogout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("ClientLogin", "Account");
        }


        // ------------------ FORGOT PASSWORD ------------------
        [HttpGet]
        [AllowAnonymous]
        public IActionResult ForgotPassword() => View();

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {

            if (!ModelState.IsValid)
            {
                return View(model); // Hiển thị lỗi validation
            }

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                return RedirectToAction(nameof(ForgotPasswordConfirmation));
            }


            // DEBUG LOG
            Console.WriteLine($"Email user: {user.Email}");

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var resetLink = Url.Action("ResetPassword", "Account", new { token, email = user.Email },
                                                                        protocol: Request.Scheme,
                                                                        host: Request.Host.ToString());

            await _emailSender.SendEmailAsync(user.Email, "Đặt lại mật khẩu",
            $"<p>Nhấn vào <a href='{resetLink}'>đây</a> để đặt lại mật khẩu.</p>");

            return RedirectToAction(nameof(ForgotPasswordConfirmation));
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult ForgotPasswordConfirmation() => View();


        // ------------------ RESET PASSWORD ------------------
        [HttpGet]
        [AllowAnonymous]
        public IActionResult ResetPassword(string token, string email)
        {
            if (token == null || email == null)
            {
                return RedirectToAction("Login");
            }

            var model = new ResetPasswordViewModel { Token = token, Email = email };
            return View(model);
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
                return RedirectToAction(nameof(ResetPasswordConfirmation));

            var result = await _userManager.ResetPasswordAsync(user, model.Token, model.Password);
            if (result.Succeeded)
            {
                user.IsDefaultPassword = false;
                user.UpdatedAt = DateTime.Now;

                await _userManager.UpdateAsync(user);

                return RedirectToAction(nameof(ResetPasswordConfirmation));
            }

            foreach (var error in result.Errors)
                ModelState.AddModelError("", error.Description);

            return View(model);

        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult ResetPasswordConfirmation() => View();


        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RequestChangePassword()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login");

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);

            var resetLink = Url.Action("ResetPassword", "Account",
                new { token = token, email = user.Email },
                protocol: Request.Scheme);

            await _emailSender.SendEmailAsync(user.Email, "Yêu cầu đổi mật khẩu",
                $"<p>Bạn đã yêu cầu đổi mật khẩu. Nhấn vào <a href='{resetLink}'>đây</a> để thiết lập mật khẩu mới.</p>");

            return View("ForgotPasswordConfirmation");
        }

        // ============================================================
        // PHẦN DÀNH CHO KHÁCH HÀNG (CLIENT)
        // ============================================================

        // 1. ĐĂNG NHẬP KHÁCH HÀNG (Giao diện riêng)
        [HttpGet]
        [AllowAnonymous]
        public IActionResult ClientLogin(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ClientLogin(LoginViewModel model, string? returnUrl = null)
        {
            if (ModelState.IsValid)
            {
                var result = await _signInManager.PasswordSignInAsync(model.Email, model.Password, model.RememberMe, lockoutOnFailure: false);
                if (result.Succeeded)
                {
                    // Nếu có returnUrl thì quay lại trang đó (ví dụ đang thanh toán)
                    if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                    {
                        return Redirect(returnUrl);
                    }
                    return RedirectToAction("Index", "Shopping"); // Về trang chủ
                }
                ModelState.AddModelError(string.Empty, "Đăng nhập thất bại. Sai email hoặc mật khẩu.");
            }
            return View(model);
        }

        // 2. ĐĂNG KÝ KHÁCH HÀNG
        [HttpGet]
        [AllowAnonymous]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                // A. Kiểm tra Email đã tồn tại trong hệ thống User chưa
                var existingUser = await _userManager.FindByEmailAsync(model.Email);
                if (existingUser != null)
                {
                    ModelState.AddModelError("Email", "Email này đã được đăng ký tài khoản.");
                    return View(model);
                }

                // B. Kiểm tra SĐT trong hệ thống Customer
                // Logic: Tìm xem có khách hàng nào dùng SĐT này chưa?
                var existingCustomer = await _context.Customers
                                            .FirstOrDefaultAsync(c => c.Phone == model.Phone);

                // Nếu khách hàng đã tồn tại VÀ đã có tài khoản rồi -> Báo lỗi
                if (existingCustomer != null && !string.IsNullOrEmpty(existingCustomer.AccountId))
                {
                    ModelState.AddModelError("Phone", "Số điện thoại này đã được liên kết với một tài khoản khác.");
                    return View(model);
                }

                // C. Tạo AppUser (Tài khoản đăng nhập)
                var user = new AppUser
                {
                    UserName = model.Email,
                    Email = model.Email,
                    FullName = model.FullName,
                    PhoneNumber = model.Phone,
                    Address = model.Address,
                    IsActive = true,
                    IsDefaultPassword = false,
                    CreatedAt = DateTime.Now
                };

                var result = await _userManager.CreateAsync(user, model.Password);

                if (result.Succeeded)
                {
                    // D. Xử lý liên kết Customer
                    if (existingCustomer != null)
                    {
                        // TRƯỜNG HỢP 1: Khách đã mua tại quầy (Có hồ sơ, chưa có Account)
                        // -> Cập nhật AccountId vào hồ sơ cũ để bảo toàn điểm
                        existingCustomer.AccountId = user.Id;

                        // Cập nhật thêm thông tin nếu hồ sơ cũ thiếu
                        if (string.IsNullOrEmpty(existingCustomer.Email)) existingCustomer.Email = model.Email;
                        if (string.IsNullOrEmpty(existingCustomer.Address)) existingCustomer.Address = model.Address;

                        _context.Customers.Update(existingCustomer);
                    }
                    else
                    {
                        // TRƯỜNG HỢP 2: Khách mới hoàn toàn
                        // -> Tạo hồ sơ Customer mới
                        var newCustomer = new Customer
                        {
                            CustomerId = Guid.NewGuid().ToString(),
                            FullName = model.FullName,
                            Phone = model.Phone,
                            Email = model.Email,
                            Address = model.Address,
                            AccountId = user.Id, // Liên kết ngay lập tức
                            IsActive = true,
                            Points = 0,
                            CreatedAt = DateTime.Now,
                            UpdatedAt = DateTime.Now
                        };
                        _context.Customers.Add(newCustomer);
                    }

                    await _context.SaveChangesAsync();

                    // E. Đăng nhập ngay sau khi đăng ký
                    await _signInManager.SignInAsync(user, isPersistent: false);
                    return RedirectToAction("Index", "Shopping");
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }
            return View(model);
        }
    }
}