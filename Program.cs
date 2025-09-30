using BookstoreManagement.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

builder.Services.AddRazorPages();

builder.Services.AddDbContext<BookstoreContext>(
	options => options.UseSqlServer(builder.Configuration.GetConnectionString("BookstoreConnectionString") ??
	 throw new InvalidOperationException("Connection string 'BookstoreContext' not found.")));

builder.Services.AddIdentity<IdentityUser, IdentityRole>()
	.AddEntityFrameworkStores<BookstoreContext>()
	.AddDefaultTokenProviders();


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
		options.LoginPath = "/login/";
		options.LogoutPath = "/logout/";
		options.AccessDeniedPath = "/Identity/Account/AccessDenied";
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
