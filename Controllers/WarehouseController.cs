using BookstoreManagement.Models;
using BookstoreManagement.ViewModels.Warehouse;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

public class WarehouseController : Controller
{
    private readonly BookstoreContext _context;

    public WarehouseController(BookstoreContext context)
    {
        _context = context;
    }

    // GET: /Warehouse/Index
    [HttpGet]
    public async Task<IActionResult> Index(string searchString, string typeFilter, string statusFilter)
    {
        // Tạo SelectList cho Filter
        ViewBag.TypeFilter = new SelectList(new[] { "Import", "Export" });
        ViewBag.StatusFilter = new SelectList(new[] { "Completed", "Pending", "Cancelled" });
        ViewData["CurrentFilter"] = searchString;

        // === PHẦN SỬA LỖI BẮT ĐẦU TỪ ĐÂY ===

        // 1. Tạo câu query cho Phiếu Nhập (CHƯA THỰC THI)
        var importQuery = _context.ImportTickets
            .Include(t => t.Supplier)
            .Select(t => new WarehouseTicketViewModel
            {
                Id = t.ImportId,
                Type = "Import",
                DocumentNumber = t.DocumentNumber,
                Date = t.Date,
                Reference = t.Supplier.Name,
                TotalQuantity = t.TotalQuantity,
                TotalCost = t.TotalCost,
                Status = t.Status
            });

        // 2. Tạo câu query cho Phiếu Xuất (CHƯA THỰC THI)
        var exportQuery = _context.ExportTickets
            .Include(t => t.Reference) // Đơn hàng liên quan
            .Select(t => new WarehouseTicketViewModel
            {
                Id = t.ExportId,
                Type = "Export",
                DocumentNumber = t.DocumentNumber,
                Date = t.Date,
                Reference = t.Reason == "Sale" ? $"ĐH: {t.Reference.OrderId}" : t.Reason,
                TotalQuantity = t.TotalQuantity,
                TotalCost = null,
                Status = t.Status
            });

        // 3. Áp dụng Filter cho TỪNG câu query
        if (!String.IsNullOrEmpty(searchString))
        {
            importQuery = importQuery.Where(t => (t.DocumentNumber != null && t.DocumentNumber.Contains(searchString))
                                              || t.Reference.Contains(searchString));

            exportQuery = exportQuery.Where(t => (t.DocumentNumber != null && t.DocumentNumber.Contains(searchString))
                                              || t.Reference.Contains(searchString));
        }

        if (!String.IsNullOrEmpty(statusFilter))
        {
            importQuery = importQuery.Where(t => t.Status == statusFilter);
            exportQuery = exportQuery.Where(t => t.Status == statusFilter);
        }

        // 4. Khởi tạo danh sách kết quả
        var importList = new List<WarehouseTicketViewModel>();
        var exportList = new List<WarehouseTicketViewModel>();

        // 5. THỰC THI query (chỉ thực thi cái nào cần thiết)
        if (string.IsNullOrEmpty(typeFilter) || typeFilter == "Import")
        {
            // Thực thi query 1
            importList = await importQuery.ToListAsync();
        }

        if (string.IsNullOrEmpty(typeFilter) || typeFilter == "Export")
        {
            // Thực thi query 2
            exportList = await exportQuery.ToListAsync();
        }

        // 6. Gộp 2 danh sách (trong bộ nhớ)
        var allTickets = importList.Concat(exportList);

        // 7. Sắp xếp và trả về View
        var finalTickets = allTickets
            .OrderByDescending(t => t.Date)
            .ToList(); // Dùng ToList() vì đã ở trong memory

        return View(finalTickets);
    }


    // GET: /Warehouse/Details/5?type=Import
    [HttpGet]
    public async Task<IActionResult> Details(int id, string type)
    {
        var viewModel = new WarehouseDetailViewModel { TicketType = type };

        if (type == "Import")
        {
            viewModel.ImportTicket = await _context.ImportTickets
                .Include(t => t.Supplier)
                .Include(t => t.User)
                .Include(t => t.ImportDetails)
                    .ThenInclude(d => d.Book) // Load Sách từ Chi tiết
                .FirstOrDefaultAsync(t => t.ImportId == id);
        }
        else // type == "Export"
        {
            viewModel.ExportTicket = await _context.ExportTickets
                .Include(t => t.Reference) // Đơn hàng
                .Include(t => t.User)
                .Include(t => t.ExportDetails)
                    .ThenInclude(d => d.Book) // Load Sách từ Chi tiết
                .FirstOrDefaultAsync(t => t.ExportId == id);
        }

        if (viewModel.ImportTicket == null && viewModel.ExportTicket == null)
        {
            return NotFound();
        }

        return View(viewModel);
    }

    // (Thêm Action Create() mà chúng ta đã làm ở bước trước)

    [HttpGet]
    public IActionResult Create()
    {
        // Tải dữ liệu cho các dropdown
        var viewModel = new ImportTicketCreateViewModel
        {
            Suppliers = new SelectList(_context.Suppliers.ToList(), "SupplierId", "Name"),
            PaymentMethods = new SelectList(_context.Codes.Where(c => c.Entity == "PaymentMethod").ToList(), "CodeId", "Value"),
            Books = new SelectList(_context.Books.Where(b => b.IsDeleted == false).ToList(), "BookId", "Title")
        };
        return View(viewModel);
    }

    // ### 2. ACTION XỬ LÝ FORM (HTTP POST) ###
    // POST: /Warehouse/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ImportTicketCreateViewModel viewModel)
    {
        // Kiểm tra xem có ít nhất 1 chi tiết sách không
        if (viewModel.Details == null || !viewModel.Details.Any())
        {
            ModelState.AddModelError("Details", "Bạn phải thêm ít nhất một quyển sách.");
        }

        if (ModelState.IsValid)
        {
            // Lấy User ID của người đang đăng nhập
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // 1. Tạo đối tượng ImportTicket (Master)
            var importTicket = new ImportTicket
            {
                UserId = userId, // ID người tạo
                SupplierId = viewModel.SupplierId,
                PaymentMethodId = viewModel.PaymentMethodId,
                Note = viewModel.Note,
                Date = DateTime.Now,
                Status = "Completed", // Đặt là "Hoàn thành"
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now,
                // Tự động tạo mã phiếu, ví dụ: "PN" + timestamp
                DocumentNumber = $"PN{DateTimeOffset.Now.ToUnixTimeSeconds()}"
            };

            // 2. Tính tổng tiền, tổng số lượng
            importTicket.TotalQuantity = viewModel.Details.Sum(d => d.Quantity);
            importTicket.TotalCost = viewModel.Details.Sum(d => d.Quantity * d.CostPrice);

            // 3. Thêm các ImportDetail (Details)
            foreach (var item in viewModel.Details)
            {
                importTicket.ImportDetails.Add(new ImportDetail
                {
                    BookId = item.BookId,
                    Quantity = item.Quantity,
                    CostPrice = item.CostPrice,
                    Subtotal = item.Quantity * item.CostPrice
                });

                // 4. *** LOGIC CỐT LÕI: CẬP NHẬT TỒN KHO (CỘNG KHO) ***
                var book = await _context.Books.FindAsync(item.BookId);
                if (book != null)
                {
                    // Nếu tồn kho ban đầu là null, coi như là 0
                    if (book.StockQuantity == null)
                    {
                        book.StockQuantity = 0;
                    }
                    book.StockQuantity += item.Quantity; // Cộng kho
                    book.UpdatedAt = DateTime.Now;
                }
            }

            // 5. Lưu tất cả vào DB (dùng Transaction để đảm bảo an toàn)
            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    _context.ImportTickets.Add(importTicket);
                    await _context.SaveChangesAsync(); // Lưu phiếu nhập, chi tiết, và sách đã cập nhật

                    await transaction.CommitAsync(); // Xác nhận tất cả thay đổi
                }
                catch (Exception)
                {
                    await transaction.RollbackAsync(); // Hoàn tác nếu có lỗi
                    ModelState.AddModelError("", "Đã xảy ra lỗi khi lưu phiếu nhập. Vui lòng thử lại.");

                    // Tải lại dropdowns nếu thất bại
                    await LoadDropdownsForCreateView(viewModel);
                    return View(viewModel);
                }
            }

            TempData["SuccessMessage"] = "Tạo phiếu nhập thành công!";
            return RedirectToAction("Index"); // Chuyển về trang danh sách
        }

        // Nếu model không hợp lệ (ModelState.IsValid == false)
        // Tải lại dropdown và trả về view
        await LoadDropdownsForCreateView(viewModel);
        return View(viewModel);
    }

    // Hàm private để tránh lặp code tải dropdown
    private async Task LoadDropdownsForCreateView(ImportTicketCreateViewModel viewModel)
    {
        viewModel.Suppliers = new SelectList(await _context.Suppliers.ToListAsync(), "SupplierId", "Name", viewModel.SupplierId);
        viewModel.PaymentMethods = new SelectList(await _context.Codes.Where(c => c.Entity == "PaymentMethod").ToListAsync(), "CodeId", "Value", viewModel.PaymentMethodId);
        viewModel.Books = new SelectList(await _context.Books.Where(b => b.IsDeleted == false).ToListAsync(), "BookId", "Title");
    }
}