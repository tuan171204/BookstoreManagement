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
	.AddDefaultTokenProviders()
	.AddClaimsPrincipalFactory<CustomClaimsPrincipalFactory>();

builder.Services.AddTransient<IEmailSender, EmailSender>();
builder.Services.AddScoped<ReportService>();

builder.Services.AddAuthorization(options =>
{
	// Quyền truy cập quản trị
	options.AddPolicy("Admin.View", policy => policy.RequireClaim("Permission", "Admin.View"));

	// Quản lý Đơn hàng 
	options.AddPolicy("Order.View", policy => policy.RequireClaim("Permission", "Order.View"));
	options.AddPolicy("Order.Create", policy => policy.RequireClaim("Permission", "Order.Create"));
	options.AddPolicy("Order.Update", policy => policy.RequireClaim("Permission", "Order.Update"));
	options.AddPolicy("Order.Delete", policy => policy.RequireClaim("Permission", "Order.Delete"));
	options.AddPolicy("Order.Approve", policy => policy.RequireClaim("Permission", "Order.Approve"));

	// Quản lý Sách 
	options.AddPolicy("Book.View", policy => policy.RequireClaim("Permission", "Book.View"));
	options.AddPolicy("Book.Create", policy => policy.RequireClaim("Permission", "Book.Create"));
	options.AddPolicy("Book.Update", policy => policy.RequireClaim("Permission", "Book.Update"));
	options.AddPolicy("Book.Delete", policy => policy.RequireClaim("Permission", "Book.Delete"));

	// Quản lý Tác giả 
	options.AddPolicy("Author.View", policy => policy.RequireClaim("Permission", "Author.View"));
	options.AddPolicy("Author.Create", policy => policy.RequireClaim("Permission", "Author.Create"));
	options.AddPolicy("Author.Update", policy => policy.RequireClaim("Permission", "Author.Update"));
	options.AddPolicy("Author.Delete", policy => policy.RequireClaim("Permission", "Author.Delete"));

	// Quản lý Nhà xuất bản 
	options.AddPolicy("Publisher.View", policy => policy.RequireClaim("Permission", "Publisher.View"));
	options.AddPolicy("Publisher.Create", policy => policy.RequireClaim("Permission", "Publisher.Create"));
	options.AddPolicy("Publisher.Update", policy => policy.RequireClaim("Permission", "Publisher.Update"));
	options.AddPolicy("Publisher.Delete", policy => policy.RequireClaim("Permission", "Publisher.Delete"));

	// Quản lý Danh mục & Nhà cung cấp 
	options.AddPolicy("Category.Manage", policy => policy.RequireClaim("Permission", "Category.Manage"));
	options.AddPolicy("Supplier.View", policy => policy.RequireClaim("Permission", "Supplier.View"));
	options.AddPolicy("Supplier.Create", policy => policy.RequireClaim("Permission", "Supplier.Create"));
	options.AddPolicy("Supplier.Update", policy => policy.RequireClaim("Permission", "Supplier.Update"));
	options.AddPolicy("Supplier.Delete", policy => policy.RequireClaim("Permission", "Supplier.Delete"));
	options.AddPolicy("Supplier.Manage", policy => policy.RequireClaim("Permission", "Supplier.Manage"));

	// Quản lý Khách hàng (Customer) 
	options.AddPolicy("Customer.View", policy => policy.RequireClaim("Permission", "Customer.View"));
	options.AddPolicy("Customer.Create", policy => policy.RequireClaim("Permission", "Customer.Create"));
	options.AddPolicy("Customer.Update", policy => policy.RequireClaim("Permission", "Customer.Update"));
	options.AddPolicy("Customer.Delete", policy => policy.RequireClaim("Permission", "Customer.Delete"));

	// Quản lý Khuyến mãi (Promotion) 
	options.AddPolicy("Promotion.View", policy => policy.RequireClaim("Permission", "Promotion.View"));
	options.AddPolicy("Promotion.Create", policy => policy.RequireClaim("Permission", "Promotion.Create"));
	options.AddPolicy("Promotion.Update", policy => policy.RequireClaim("Permission", "Promotion.Update"));
	options.AddPolicy("Promotion.Delete", policy => policy.RequireClaim("Permission", "Promotion.Delete"));

	// Quản lý Kho (Warehouse) 
	options.AddPolicy("Warehouse.View", policy => policy.RequireClaim("Permission", "Warehouse.View"));
	options.AddPolicy("Warehouse.Import", policy => policy.RequireClaim("Permission", "Warehouse.Import"));
	options.AddPolicy("Warehouse.Export", policy => policy.RequireClaim("Permission", "Warehouse.Export"));

	// Báo cáo (Report)
	options.AddPolicy("Report.View", policy => policy.RequireClaim("Permission", "Report.View"));
	options.AddPolicy("Report.Revenue", policy => policy.RequireClaim("Permission", "Report.Revenue"));
	options.AddPolicy("Report.Inventory", policy => policy.RequireClaim("Permission", "Report.Inventory"));
	options.AddPolicy("Report.Export", policy => policy.RequireClaim("Permission", "Report.Export"));

	// Quản lý Người dùng & Quyền (User/Role)
	options.AddPolicy("User.View", policy => policy.RequireClaim("Permission", "User.View"));
	options.AddPolicy("User.Create", policy => policy.RequireClaim("Permission", "User.Create"));
	options.AddPolicy("User.Update", policy => policy.RequireClaim("Permission", "User.Update"));
	options.AddPolicy("User.Delete", policy => policy.RequireClaim("Permission", "User.Delete"));
	options.AddPolicy("Role.Manage", policy => policy.RequireClaim("Permission", "Role.Manage"));

	// Cấu hình hệ thống (Settings)
	options.AddPolicy("System.Settings", policy => policy.RequireClaim("Permission", "System.Settings"));
});

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
		options.AccessDeniedPath = "/Home/AccessDenied"; // Trang khi bị cấm truy cập
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
		options.LoginPath = "/Account/Login"; // Trang đăng nhập admin
		options.LogoutPath = "/Account/Logout";
		options.AccessDeniedPath = "/Home/AccessDenied";
		options.SlidingExpiration = true;
	});

	builder.Services.Configure<SecurityStampValidatorOptions>(options =>
	{
		options.ValidationInterval = TimeSpan.FromMinutes(30); // check lại mỗi 30 phút
	});
}


var app = builder.Build();

// ---Seed dữ liệu người dùng mặc định ---
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
				new Code { Entity = "PromotionType", Key = 3, Value = "Tặng sách", CreatedAt = DateTime.Now }
			};
			context.Codes.AddRange(promotionTypes);
			await context.SaveChangesAsync();
			Console.WriteLine("Đã seed PromotionType codes!");
		}

		if (!context.Codes.Any(c => c.Entity == "PaymentMethod"))
		{
			var promotionTypes = new[]
			{
				new Code { Entity = "PaymentMethod", Key = 1, Value = "Tiền mặt", CreatedAt = DateTime.Now },
				new Code { Entity = "PaymentMethod", Key = 2, Value = "Chuyển khoản", CreatedAt = DateTime.Now }
			};
			context.Codes.AddRange(promotionTypes);
			await context.SaveChangesAsync();
			Console.WriteLine("Đã seed PaymentMethod codes!");
		}

		// Seed Categories với DefaultProfitMargin nếu chưa có
		if (!context.Categories.Any())
		{
			var categories = new[]
			{
				new Category 
				{ 
					Name = "Tiểu thuyết", 
					Description = "Thể loại tiểu thuyết văn học",
					DefaultProfitMargin = 20.0, // 20% lợi nhuận mặc định
					CreatedAt = DateTime.Now 
				},
				new Category 
				{ 
					Name = "Tình cảm", 
					Description = "Thể loại sách tình cảm, lãng mạn",
					DefaultProfitMargin = 25.0, // 25% lợi nhuận mặc định
					CreatedAt = DateTime.Now 
				},
				new Category 
				{ 
					Name = "Kinh tế", 
					Description = "Sách về kinh tế, tài chính",
					DefaultProfitMargin = 15.0, // 15% lợi nhuận mặc định
					CreatedAt = DateTime.Now 
				},
				new Category 
				{ 
					Name = "Khoa học", 
					Description = "Sách khoa học, công nghệ",
					DefaultProfitMargin = 18.0, // 18% lợi nhuận mặc định
					CreatedAt = DateTime.Now 
				},
				new Category 
				{ 
					Name = "Thiếu nhi", 
					Description = "Sách dành cho thiếu nhi",
					DefaultProfitMargin = 30.0, // 30% lợi nhuận mặc định
					CreatedAt = DateTime.Now 
				}
			};
			context.Categories.AddRange(categories);
			await context.SaveChangesAsync();
			Console.WriteLine("Đã seed Categories với DefaultProfitMargin!");
		}
		else
		{
			// Cập nhật DefaultProfitMargin cho các category đã có (nếu chưa có giá trị)
			var categoriesWithoutProfit = await context.Categories
				.Where(c => c.DefaultProfitMargin == null || c.DefaultProfitMargin == 0)
				.ToListAsync();
			
			if (categoriesWithoutProfit.Any())
			{
				// Set mặc định 20% cho các category chưa có DefaultProfitMargin
				foreach (var category in categoriesWithoutProfit)
				{
					category.DefaultProfitMargin = 20.0;
					category.UpdatedAt = DateTime.Now;
				}
				await context.SaveChangesAsync();
				Console.WriteLine($"Đã cập nhật DefaultProfitMargin = 20% cho {categoriesWithoutProfit.Count} category!");
			}
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

app.UseStatusCodePagesWithReExecute("/Home/HandleError", "?statusCode={0}");

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();

app.UseAuthorization();

app.MapControllerRoute(
	name: "default",
	pattern: "{controller=Shopping}/{action=Index}/{id?}");

app.MapRazorPages();

app.Run();
