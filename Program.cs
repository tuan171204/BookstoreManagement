using BookstoreManagement.Models;
using BookstoreManagement.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddScoped<ImportService>();

builder.Services.AddRazorPages();

builder.Services.AddDbContext<BookstoreContext>(
	options => options.UseSqlServer(builder.Configuration.GetConnectionString("BookstoreConnectionString") ??
	 throw new InvalidOperationException("Connection string 'BookstoreContext' not found.")));

builder.Services.AddIdentity<AppUser, AppRole>()
	.AddEntityFrameworkStores<BookstoreContext>()
	.AddDefaultTokenProviders();

builder.Services.AddTransient<IEmailSender, EmailSender>();
builder.Services.AddScoped<ReportService>();

// set biến môi trường ASPNETCORE_ENVIRONMENT=Development/Production trong Properties/launchSettings.json
if (builder.Environment.IsDevelopment())
{
	builder.Services.Configure<IdentityOptions>(options =>
	{
		// Password settings (để lỏng cho tiện test)
		options.Password.RequireDigit = false;
		options.Password.RequireLowercase = false;
		options.Password.RequireNonAlphanumeric = false;
		options.Password.RequireUppercase = false;
		options.Password.RequiredLength = 1; // test nên để ngắn
		options.Password.RequiredUniqueChars = 1;

		// Lockout settings
		options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
		options.Lockout.MaxFailedAccessAttempts = 5; // sai 5 lần khóa 5 phút
		options.Lockout.AllowedForNewUsers = true;

		// User settings
		options.User.RequireUniqueEmail = false; // Người dùng xác thực email mới được login, test nên để false
	});

	builder.Services.ConfigureApplicationCookie(options =>
	{
		options.Cookie.HttpOnly = true;
		options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
		options.LoginPath = "/Account/Login"; // Trang đăng nhập
		options.LogoutPath = "/Account/Logout";
		options.AccessDeniedPath = "/Account/AccessDenied"; // Trang khi bị cấm truy cập
		options.SlidingExpiration = true;
	});

	builder.Services.Configure<SecurityStampValidatorOptions>(options =>
	{
		options.ValidationInterval = TimeSpan.FromSeconds(5); // test nhanh cứ 5s check lại một lần
	});
}


if (builder.Environment.IsProduction())
{
	builder.Services.Configure<IdentityOptions>(options =>
	{
		// Password settings
		options.Password.RequireDigit = true;
		options.Password.RequireLowercase = true;
		options.Password.RequireNonAlphanumeric = true; // bắt buộc ký tự đặc biệt
		options.Password.RequireUppercase = true;
		options.Password.RequiredLength = 8; // password dài đảm bảo an toàn
		options.Password.RequiredUniqueChars = 2;

		// Lockout settings
		options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
		options.Lockout.MaxFailedAccessAttempts = 3; // 3 lần sai là khóa 15 phút
		options.Lockout.AllowedForNewUsers = true;

		// User settings
		options.User.RequireUniqueEmail = true; // email phải unique
	});

	builder.Services.ConfigureApplicationCookie(options =>
	{
		options.Cookie.HttpOnly = true;
		options.ExpireTimeSpan = TimeSpan.FromMinutes(60); // lâu hơn chút
		options.LoginPath = "/login/";
		options.LogoutPath = "/logout/";
		options.AccessDeniedPath = "/Identity/Account/AccessDenied";
		options.SlidingExpiration = true;
	});

	builder.Services.Configure<SecurityStampValidatorOptions>(options =>
	{
		options.ValidationInterval = TimeSpan.FromMinutes(30); // check lại mỗi 30 phút
	});
}


var app = builder.Build();


// --- Seed dữ liệu người dùng mặc định ---
using (var scope = app.Services.CreateScope())
{
	var services = scope.ServiceProvider;

	try
	{
		var userManager = services.GetRequiredService<UserManager<AppUser>>();
		var roleManager = services.GetRequiredService<RoleManager<AppRole>>();
		var context = services.GetRequiredService<BookstoreContext>();

		// Tạo role "Admin" nếu chưa có
		if (!await roleManager.RoleExistsAsync("Admin"))
		{
			await roleManager.CreateAsync(new AppRole
			{
				Name = "Admin",
				Description = "Quản trị hệ thống",
				CreatedAt = DateTime.Now
			});
		}

		// Seed PromotionType codes
		if (!context.Codes.Any(c => c.Entity == "PromotionType"))
		{
			var promotionTypes = new[]
			{
				new Code { Entity = "PromotionType", Key = 1, Value = "Giảm giá theo phần trăm", CreatedAt = DateTime.Now },
				new Code { Entity = "PromotionType", Key = 2, Value = "Giảm giá cố định", CreatedAt = DateTime.Now },
				new Code { Entity = "PromotionType", Key = 3, Value = "Tặng sách", CreatedAt = DateTime.Now },
				new Code { Entity = "PromotionType", Key = 4, Value = "Mua X tặng Y", CreatedAt = DateTime.Now }
			};
			context.Codes.AddRange(promotionTypes);
			await context.SaveChangesAsync();
			Console.WriteLine("Đã seed PromotionType codes!");
		}

		// Tạo user admin nếu chưa có
		string adminEmail = "admin@bookstore.com";
		var adminUser = await userManager.FindByEmailAsync(adminEmail);
		if (adminUser == null)
		{
			var user = new AppUser
			{
				UserName = adminEmail,
				Email = adminEmail,
				FullName = "Administrator",
				IsActive = true,
				CreatedAt = DateTime.Now
			};

			var result = await userManager.CreateAsync(user, "123456");
			if (result.Succeeded)
			{
				await userManager.AddToRoleAsync(user, "Admin");
				Console.WriteLine("Đã tạo tài khoản Admin mặc định!");
			}
			else
			{
				Console.WriteLine("Lỗi khi tạo tài khoản Admin:");
				foreach (var err in result.Errors)
					Console.WriteLine($" - {err.Description}");
			}
		}
	}
	catch (Exception ex)
	{
		Console.WriteLine($"Seed dữ liệu lỗi: {ex.Message}");
	}
}


// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
	app.UseExceptionHandler("/Home/Error");
	// The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
	app.UseHsts();
}


app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();

app.UseAuthorization();

app.MapControllerRoute(
	name: "default",
	pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();

app.Run();
