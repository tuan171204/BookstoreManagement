using BookstoreManagement.Models;
using BookstoreManagement.Services;
using BookstoreManagement.ViewModels.Warehouse;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using OfficeOpenXml;
using OfficeOpenXml.Style;


public class WarehouseController : Controller
{
    private readonly BookstoreContext _context;
    private readonly ImportService _importService;


    public WarehouseController(BookstoreContext context, ImportService importService)
    {
        _context = context;
        _importService = importService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string searchString, string typeFilter, string statusFilter, string sortBy = "Date", string sortOrder = "desc", int pageNumber = 1, int pageSize = 10)
    {
        ViewBag.TypeFilter = new SelectList(new[] { "Phiếu Nhập", "Phiếu Xuất" }, typeFilter);
        ViewBag.StatusFilter = new SelectList(new[] { "Completed", "Pending", "Cancelled" }, statusFilter);
        ViewData["CurrentFilter"] = searchString;
        ViewData["TypeFilter"] = typeFilter;
        ViewData["StatusFilter"] = statusFilter;
        ViewData["SortBy"] = sortBy;
        ViewData["SortOrder"] = sortOrder;


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


        var exportQuery = _context.ExportTickets
            .Include(t => t.Reference)
            .Select(t => new WarehouseTicketViewModel
            {
                Id = t.ExportId,
                Type = "Export",
                DocumentNumber = t.DocumentNumber,
                Date = t.Date,

                Reference = t.Reason == "Sale" && t.Reference != null ? "ĐH: " + t.Reference.OrderId : t.Reason,
                TotalQuantity = t.TotalQuantity,
                TotalCost = null,
                Status = t.Status
            });


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

        var importList = new List<WarehouseTicketViewModel>();
        var exportList = new List<WarehouseTicketViewModel>();

        // Get all tickets first to calculate total
        if (string.IsNullOrEmpty(typeFilter) || typeFilter == "Phiếu Nhập")
        {
            importList = await importQuery.OrderByDescending(t => t.Date).ToListAsync();
        }

        if (string.IsNullOrEmpty(typeFilter) || typeFilter == "Phiếu Xuất")
        {
            exportList = await exportQuery.OrderByDescending(t => t.Date).ToListAsync();
        }

        var allTickets = importList.Concat(exportList).ToList();

        // Apply sorting
        var sortedTickets = sortBy?.ToLower() switch
        {
            "documentnumber" => sortOrder == "asc" 
                ? allTickets.OrderBy(t => t.DocumentNumber ?? "").ToList()
                : allTickets.OrderByDescending(t => t.DocumentNumber ?? "").ToList(),
            "type" => sortOrder == "asc" 
                ? allTickets.OrderBy(t => t.Type).ToList()
                : allTickets.OrderByDescending(t => t.Type).ToList(),
            "date" => sortOrder == "asc" 
                ? allTickets.OrderBy(t => t.Date).ToList()
                : allTickets.OrderByDescending(t => t.Date).ToList(),
            "totalquantity" => sortOrder == "asc" 
                ? allTickets.OrderBy(t => t.TotalQuantity).ToList()
                : allTickets.OrderByDescending(t => t.TotalQuantity).ToList(),
            "totalcost" => sortOrder == "asc" 
                ? allTickets.OrderBy(t => t.TotalCost ?? 0).ToList()
                : allTickets.OrderByDescending(t => t.TotalCost ?? 0).ToList(),
            "status" => sortOrder == "asc" 
                ? allTickets.OrderBy(t => t.Status).ToList()
                : allTickets.OrderByDescending(t => t.Status).ToList(),
            _ => allTickets.OrderByDescending(t => t.Date).ToList()
        };

        // Calculate pagination
        var totalItems = sortedTickets.Count;
        var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

        // Apply pagination
        var finalTickets = sortedTickets
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        ViewBag.PageNumber = pageNumber;
        ViewBag.PageSize = pageSize;
        ViewBag.TotalPages = totalPages;
        ViewBag.TotalItems = totalItems;
        ViewBag.TypeFilterParam = typeFilter;
        ViewBag.StatusFilterParam = statusFilter;

        return View(finalTickets);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id, string type)
    {
        var viewModel = new WarehouseDetailViewModel { TicketType = type };

        if (type == "Import")
        {
            viewModel.ImportTicket = await _context.ImportTickets
                .Include(t => t.Supplier)
                .Include(t => t.User)
                .Include(t => t.PaymentMethod)
                .Include(t => t.ImportDetails)
                    .ThenInclude(d => d.Book)
                .FirstOrDefaultAsync(t => t.ImportId == id);
        }
        else
        {
            viewModel.ExportTicket = await _context.ExportTickets
                .Include(t => t.Reference)
                .Include(t => t.User)
                .Include(t => t.ExportDetails)
                    .ThenInclude(d => d.Book)
                .FirstOrDefaultAsync(t => t.ExportId == id);
        }

        if (viewModel.ImportTicket == null && viewModel.ExportTicket == null)
        {
            return NotFound();
        }

        return View(viewModel);
    }

    [HttpGet]
    public IActionResult GetPaymentMethods()
    {
        var paymentMethods = _context.Codes
            .Where(c => c.Entity == "PaymentMethod")
            .Select(c => new { id = c.CodeId, name = c.Value }).ToList();
        return Json(paymentMethods);
    }

    [HttpGet]
    public IActionResult Create()
    {
        var viewModel = new ImportTicketCreateViewModel
        {
            Suppliers = new SelectList(
                _context.Suppliers.Where(s => s.IsActive).ToList(),
                "SupplierId",
                "Name"),
            PaymentMethods = new SelectList(_context.Codes.Where(c => c.Entity == "PaymentMethod").ToList(), "CodeId", "Value"),
            Books = new SelectList(_context.Books.Where(b => b.IsDeleted == false).ToList(), "BookId", "Title")
        };
        return View(viewModel);
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ImportTicketCreateViewModel viewModel)
    {

        if (viewModel.Details == null || !viewModel.Details.Any())
        {
            ModelState.AddModelError("Details", "Bạn phải thêm ít nhất một quyển sách.");
        }

        if (ModelState.IsValid)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Account");
            }

            var importTicket = new ImportTicket
            {
                UserId = userId,
                SupplierId = viewModel.SupplierId,
                PaymentMethodId = viewModel.PaymentMethodId,
                Note = viewModel.Note,
                Date = DateTime.Now,
                Status = "Pending",
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now,
                DocumentNumber = $"PN{DateTimeOffset.Now.ToUnixTimeSeconds()}"
            };


            foreach (var item in viewModel.Details)
            {
                importTicket.ImportDetails.Add(new ImportDetail
                {
                    BookId = item.BookId,
                    Quantity = item.Quantity,
                    CostPrice = item.CostPrice,
                    Subtotal = item.Quantity * item.CostPrice
                });
            }

            try
            {
                await _importService.CreateImportTicketAsync(importTicket);

                TempData["SuccessMessage"] = "Tạo phiếu nhập thành công!";
                return RedirectToAction("Index");
            }
            catch (Exception)
            {

                ModelState.AddModelError("", "Đã xảy ra lỗi khi lưu phiếu nhập. Vui lòng thử lại.");
                await LoadDropdownsForCreateView(viewModel);
                return View(viewModel);
            }
        }


        await LoadDropdownsForCreateView(viewModel);
        return View(viewModel);
    }

    [HttpGet]
    public async Task<IActionResult> GetBooksBySupplier(int supplierId)
    {

        var supplierBooks = await _context.SupplierBooks
            .Where(sb => sb.SupplierId == supplierId)
            .Include(sb => sb.Book)
            .Select(sb => new
            {
                BookId = sb.BookId,
                Title = sb.Book.Title,

                CostPrice = sb.DefaultCostPrice
            })
            .ToListAsync();


        return Json(supplierBooks);
    }


    private async Task LoadDropdownsForCreateView(ImportTicketCreateViewModel viewModel)
    {
        viewModel.Suppliers = new SelectList(await _context.Suppliers.ToListAsync(), "SupplierId", "Name", viewModel.SupplierId);
        viewModel.PaymentMethods = new SelectList(await _context.Codes.Where(c => c.Entity == "PaymentMethod").ToListAsync(), "CodeId", "Value", viewModel.PaymentMethodId);
        viewModel.Books = new SelectList(await _context.Books.Where(b => b.IsDeleted == false).ToListAsync(), "BookId", "Title");
    }


    [Authorize]
    [HttpGet]
    public IActionResult CreateExport()
    {
        var viewModel = new ExportTicketCreateViewModel
        {

            Books = new SelectList(_context.Books.Where(b => b.IsDeleted != true && b.StockQuantity > 0).ToList(), "BookId", "Title")
        };
        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateExport(ExportTicketCreateViewModel viewModel)
    {
        if (viewModel.Details == null || !viewModel.Details.Any())
        {
            ModelState.AddModelError("Details", "Bạn phải chọn ít nhất một quyển sách.");
        }

        if (ModelState.IsValid)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {

                var exportTicket = new ExportTicket
                {
                    UserId = userId ?? "1",
                    Date = DateTime.Now,
                    Status = "Completed",
                    Reason = viewModel.Reason,
                    Note = viewModel.Note,
                    DocumentNumber = $"PX{DateTimeOffset.Now.ToUnixTimeSeconds()}",
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                };


                int totalQty = 0;

                foreach (var item in viewModel.Details)
                {
                    var book = await _context.Books.FindAsync(item.BookId);


                    if (book == null)
                    {
                        throw new Exception($"Sách ID {item.BookId} không tồn tại.");
                    }


                    if ((book.StockQuantity ?? 0) < item.Quantity)
                    {
                        throw new Exception($"Sách '{book.Title}' không đủ số lượng để xuất (Tồn: {book.StockQuantity}).");
                    }


                    book.StockQuantity -= item.Quantity;
                    book.UpdatedAt = DateTime.Now;


                    exportTicket.ExportDetails.Add(new ExportDetail
                    {
                        BookId = item.BookId,
                        Quantity = item.Quantity,
                        UnitPrice = book.Price,
                        Subtotal = book.Price * item.Quantity
                    });

                    totalQty += item.Quantity;
                }

                exportTicket.TotalQuantity = totalQty;


                _context.ExportTickets.Add(exportTicket);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                TempData["SuccessMessage"] = "Tạo phiếu xuất kho thành công!";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                ModelState.AddModelError("", "Lỗi: " + ex.Message);
            }
        }


        viewModel.Books = new SelectList(await _context.Books.Where(b => b.IsDeleted != true).ToListAsync(), "BookId", "Title");
        return View(viewModel);
    }

    // =========================================================================
    // PHẦN 4: IMPORT EXCEL (LOGIC MỚI)
    // =========================================================================

    // ************ 4.1. Xử lý Upload cho Phiếu Nhập ************
    [Authorize(Policy = "Warehouse.Import")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UploadImportData(ImportExcelViewModel model)
    {
        if (model.ExcelFile == null || model.ExcelFile.Length == 0 || !model.ExcelFile.FileName.EndsWith(".xlsx"))
        {
            TempData["ErrorMessage"] = "Vui lòng chọn file Excel (.xlsx) hợp lệ.";
            return RedirectToAction("Create");
        }

        try
        {
            // 1. Đọc và xử lý file Excel
            var importDetails = await ProcessImportExcelFile(model.ExcelFile);

            // 2. Chuyển kết quả về JSON để JavaScript xử lý
            TempData["SuccessMessage"] = "Đã Import thành công chi tiết sách. Vui lòng kiểm tra và Lưu Phiếu Nhập.";
            return Json(new { success = true, details = importDetails });
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = "Lỗi xử lý file: " + ex.Message;
            return RedirectToAction("Create");
        }
    }

    // Hàm đọc chi tiết Phiếu Nhập từ Excel (Cần BookId, Quantity, CostPrice)
    private async Task<List<ImportDetailViewModel>> ProcessImportExcelFile(IFormFile file)
    {
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

        var details = new List<ImportDetailViewModel>();

        using (var stream = new MemoryStream())
        {
            await file.CopyToAsync(stream);
            using (var package = new ExcelPackage(stream))
            {
                var worksheet = package.Workbook.Worksheets[0];
                if (worksheet == null) throw new Exception("File Excel không có sheet.");

                int rowCount = worksheet.Dimension?.Rows ?? 0;
                if (rowCount <= 1) throw new Exception("File Excel không có dữ liệu (chỉ có Header?).");

                for (int row = 2; row <= rowCount; row++)
                {
                    try
                    {
                        // Đọc cột A: BookId (int)
                        int bookId = worksheet.Cells[row, 1].GetValue<int>();
                        // Đọc cột B: Quantity (int)
                        int quantity = worksheet.Cells[row, 2].GetValue<int>();
                        // Đọc cột C: CostPrice (decimal)
                        decimal costPrice = worksheet.Cells[row, 3].GetValue<decimal>();

                        var book = await _context.Books.FindAsync(bookId);

                        if (book == null)
                        {
                            throw new Exception($"Hàng {row}: Sách có ID '{bookId}' không tồn tại.");
                        }

                        if (quantity <= 0)
                        {
                            throw new Exception($"Hàng {row}: Số lượng phải lớn hơn 0.");
                        }

                        details.Add(new ImportDetailViewModel
                        {
                            BookId = bookId,
                            Quantity = quantity,
                            CostPrice = costPrice,
                            Title = book.Title,
                        });
                    }
                    catch (Exception ex)
                    {
                        // Ném ngoại lệ để bắt ở tầng trên (UploadImportData) và báo lỗi chi tiết
                        throw new Exception(ex.Message);
                    }
                }
            }
        }
        return details;
    }

    // ************ 4.2. Xử lý Upload cho Phiếu Xuất ************
    [Authorize(Policy = "Warehouse.Export")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UploadExportData(ImportExcelViewModel model) // Dùng chung ViewModel cho file
    {
        if (model.ExcelFile == null || model.ExcelFile.Length == 0 || !model.ExcelFile.FileName.EndsWith(".xlsx"))
        {
            TempData["ErrorMessage"] = "Vui lòng chọn file Excel (.xlsx) hợp lệ.";
            return RedirectToAction("CreateExport");
        }

        try
        {
            // Đọc và xử lý file Excel Export
            var exportDetails = await ProcessExportExcelFile(model.ExcelFile);

            // Trả về JSON để JavaScript xử lý
            TempData["SuccessMessage"] = "Đã Import thành công chi tiết sách. Vui lòng kiểm tra và Lưu Phiếu Xuất.";
            return Json(new { success = true, details = exportDetails });
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = "Lỗi xử lý file: " + ex.Message;
            return RedirectToAction("CreateExport");
        }
    }

    // Hàm đọc chi tiết Phiếu Xuất từ Excel (Cần BookId, Quantity)
    private async Task<List<ExportDetailViewModel>> ProcessExportExcelFile(IFormFile file)
    {
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

        var details = new List<ExportDetailViewModel>();

        using (var stream = new MemoryStream())
        {
            await file.CopyToAsync(stream);
            using (var package = new ExcelPackage(stream))
            {
                var worksheet = package.Workbook.Worksheets[0];
                if (worksheet == null) throw new Exception("File Excel không có sheet.");

                // Giả định Header ở hàng 1: BookId | Quantity
                int rowCount = worksheet.Dimension?.Rows ?? 0;
                if (rowCount <= 1) throw new Exception("File Excel không có dữ liệu (chỉ có Header?).");

                for (int row = 2; row <= rowCount; row++)
                {
                    try
                    {
                        // Đọc cột A: BookId (int)
                        int bookId = worksheet.Cells[row, 1].GetValue<int>();
                        // Đọc cột B: Quantity (int)
                        int quantity = worksheet.Cells[row, 2].GetValue<int>();

                        var book = await _context.Books.FindAsync(bookId);

                        if (book == null)
                        {
                            throw new Exception($"Hàng {row}: Sách có ID '{bookId}' không tồn tại.");
                        }

                        // Xác thực tồn kho
                        if (quantity <= 0 || quantity > book.StockQuantity)
                        {
                            throw new Exception($"Hàng {row}: Số lượng xuất ({quantity}) không hợp lệ hoặc vượt quá tồn kho ({book.StockQuantity}).");
                        }

                        details.Add(new ExportDetailViewModel
                        {
                            BookId = bookId,
                            Quantity = quantity,
                            Title = book.Title,
                            UnitPrice = book.Price
                        });
                    }
                    catch (Exception ex)
                    {
                        // Ném ngoại lệ để bắt ở tầng trên (UploadExportData) và báo lỗi chi tiết
                        throw new Exception(ex.Message);
                    }
                }
            }
        }
        return details;
    }


    // =========================================================================
    // PHẦN 6: XUẤT EXCEL (GIỮ NGUYÊN)
    // =========================================================================

    [Authorize(Policy = "Warehouse.View")] // Có thể dùng Warehouse.View hoặc Report.Export tùy Policy bạn muốn
    [HttpGet]
    public async Task<IActionResult> ExportWarehouseData()
    {
        // Lấy tất cả phiếu Nhập (kèm chi tiết và thông tin Supplier)
        var importTickets = await _context.ImportTickets
            .Include(t => t.ImportDetails)
            .ThenInclude(d => d.Book)
            .Include(t => t.Supplier)
            .ToListAsync();

        // Lấy tất cả phiếu Xuất (kèm chi tiết và thông tin tham chiếu)
        var exportTickets = await _context.ExportTickets
            .Include(t => t.ExportDetails)
            .ThenInclude(d => d.Book)
            .Include(t => t.Reference) // Reference là Order
            .ToListAsync();

        // Gọi hàm tạo file Excel và nhận về mảng byte
        var excelBytes = GenerateExcel(importTickets, exportTickets);

        // Trả về file Excel cho trình duyệt
        string fileName = $"Warehouse_Data_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
        return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
    }

    // Hàm logic tạo file Excel
    private byte[] GenerateExcel(List<ImportTicket> imports, List<ExportTicket> exports)
    {
        // Thiết lập giấy phép (EPPlus 5 trở lên yêu cầu)
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

        using (var package = new ExcelPackage())
        {
            // ----------------------------------------------------
            // SHEET 1: Phiếu Nhập Kho (Tóm tắt)
            // ----------------------------------------------------
            var wsImport = package.Workbook.Worksheets.Add("Phieu_Nhap_Kho");

            // Header
            wsImport.Cells["A1"].Value = "Mã Phiếu";
            wsImport.Cells["B1"].Value = "Ngày Nhập";
            wsImport.Cells["C1"].Value = "Nhà Cung Cấp";
            wsImport.Cells["D1"].Value = "Tổng Số Lượng";
            wsImport.Cells["E1"].Value = "Tổng Chi Phí";
            wsImport.Cells["F1"].Value = "Trạng Thái";

            // Format Header
            using (var range = wsImport.Cells["A1:F1"])
            {
                range.Style.Font.Bold = true;
                range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
            }

            // Dữ liệu
            int row = 2;
            foreach (var import in imports)
            {
                wsImport.Cells[row, 1].Value = import.DocumentNumber;
                wsImport.Cells[row, 2].Value = import.Date?.ToString("dd/MM/yyyy") ?? string.Empty;
                wsImport.Cells[row, 3].Value = import.Supplier?.Name ?? "N/A";
                wsImport.Cells[row, 4].Value = import.TotalQuantity;
                wsImport.Cells[row, 5].Value = import.TotalCost;
                wsImport.Cells[row, 5].Style.Numberformat.Format = "#,##0"; // Format tiền tệ
                wsImport.Cells[row, 6].Value = import.Status;
                row++;
            }

            // ----------------------------------------------------
            // SHEET 2: Phiếu Xuất Kho (Tóm tắt)
            // ----------------------------------------------------
            var wsExport = package.Workbook.Worksheets.Add("Phieu_Xuat_Kho");

            // Header
            wsExport.Cells["A1"].Value = "Mã Phiếu";
            wsExport.Cells["B1"].Value = "Ngày Xuất";
            wsExport.Cells["C1"].Value = "Tham Chiếu (Đơn hàng/Lý do)";
            wsExport.Cells["D1"].Value = "Tổng Số Lượng";
            wsExport.Cells["E1"].Value = "Lý do (Chi tiết)";
            wsExport.Cells["F1"].Value = "Trạng Thái";

            // Format Header
            using (var range = wsExport.Cells["A1:F1"])
            {
                range.Style.Font.Bold = true;
                range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightBlue);
            }

            // Dữ liệu
            row = 2;
            foreach (var export in exports)
            {
                wsExport.Cells[row, 1].Value = export.DocumentNumber;
                wsExport.Cells[row, 2].Value = export.Date?.ToString("dd/MM/yyyy") ?? string.Empty;

                // Hiển thị tham chiếu (Order ID) nếu là phiếu bán hàng
                string reference = export.ReferenceId.HasValue ? $"ĐH: {export.ReferenceId}" : export.Reason;

                wsExport.Cells[row, 3].Value = reference;
                wsExport.Cells[row, 4].Value = export.TotalQuantity;
                wsExport.Cells[row, 5].Value = export.Reason;
                wsExport.Cells[row, 6].Value = export.Status;
                row++;
            }

            // Tự động căn chỉnh độ rộng cột cho cả hai sheet
            wsImport.Cells[wsImport.Dimension.Address].AutoFitColumns();
            wsExport.Cells[wsExport.Dimension.Address].AutoFitColumns();

            // Trả về mảng byte của file Excel
            return package.GetAsByteArray();
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ApproveImport(int id)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var ticket = await _context.ImportTickets
                .Include(t => t.ImportDetails)
                .FirstOrDefaultAsync(t => t.ImportId == id);

            if (ticket == null) return NotFound();

            if (ticket.Status != "Pending")
            {
                TempData["ErrorMessage"] = "Phiếu này đã được xử lý trước đó.";
                return RedirectToAction("Details", new { id = id, type = "Import" });
            }

            foreach (var detail in ticket.ImportDetails)
            {
                var book = await _context.Books.FindAsync(detail.BookId);
                if (book != null)
                {
                    book.StockQuantity = (book.StockQuantity ?? 0) + detail.Quantity;
                    book.UpdatedAt = DateTime.Now;
                    _context.Books.Update(book);
                }
            }

            ticket.Status = "Completed";
            ticket.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            TempData["SuccessMessage"] = "Đã xác nhận nhập kho thành công!";
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            TempData["ErrorMessage"] = "Lỗi khi xác nhận: " + ex.Message;
        }

        return RedirectToAction("Details", new { id = id, type = "Import" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CancelImport(int id)
    {
        var ticket = await _context.ImportTickets.FindAsync(id);
        if (ticket == null) return NotFound();

        if (ticket.Status != "Pending")
        {
            TempData["ErrorMessage"] = "Chỉ có thể hủy phiếu đang ở trạng thái chờ.";
            return RedirectToAction("Details", new { id = id, type = "Import" });
        }

        ticket.Status = "Cancelled";
        ticket.UpdatedAt = DateTime.Now;

        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = "Đã hủy phiếu nhập.";

        return RedirectToAction("Details", new { id = id, type = "Import" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateImportQuantity(int importId, Dictionary<int, ImportDetailUpdateModel> details)
    {
        var ticket = await _context.ImportTickets
            .Include(t => t.ImportDetails)
            .FirstOrDefaultAsync(t => t.ImportId == importId);

        if (ticket == null) return NotFound();

        if (ticket.Status != "Pending")
        {
            TempData["ErrorMessage"] = "Không thể chỉnh sửa phiếu đã hoàn thành hoặc đã hủy.";
            return RedirectToAction("Details", new { id = importId, type = "Import" });
        }

        try
        {
            decimal newTotalCost = 0;
            int newTotalQuantity = 0;

            foreach (var item in details.Values)
            {
                var detailEntity = ticket.ImportDetails.FirstOrDefault(d => d.ImportDetailId == item.DetailId);
                if (detailEntity != null)
                {
                    detailEntity.Quantity = item.Quantity;
                    detailEntity.Subtotal = detailEntity.Quantity * detailEntity.CostPrice;

                    newTotalQuantity += detailEntity.Quantity;
                    newTotalCost += detailEntity.Subtotal ?? 0;
                }
            }

            ticket.TotalQuantity = newTotalQuantity;
            ticket.TotalCost = newTotalCost;
            ticket.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Cập nhật số lượng thành công!";
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = "Lỗi khi cập nhật: " + ex.Message;
        }

        return RedirectToAction("Details", new { id = importId, type = "Import" });
    }

    [Authorize(Policy = "Warehouse.View")] // Hoặc policy phù hợp của bạn
    [HttpGet]
    public async Task<IActionResult> ExportTicketDetail(int id, string type)
    {
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

        using (var package = new ExcelPackage())
        {
            var worksheet = package.Workbook.Worksheets.Add("ChiTietPhieu");

            // Biến để lưu thông tin chung
            string docNumber = "";
            DateTime? date = DateTime.Now;
            string supplierOrReason = "";
            string ticketTitle = "";
            string createdBy = "";

            // Danh sách chi tiết để duyệt vòng lặp (dùng object để linh hoạt)
            var detailsData = new List<dynamic>();
            decimal? totalAmount = 0;

            if (type == "Import")
            {
                var ticket = await _context.ImportTickets
                    .Include(t => t.Supplier)
                    .Include(t => t.User)
                    .Include(t => t.ImportDetails).ThenInclude(d => d.Book)
                    .FirstOrDefaultAsync(t => t.ImportId == id);

                if (ticket == null) return NotFound();

                ticketTitle = "PHIẾU NHẬP KHO";
                docNumber = ticket.DocumentNumber;
                date = ticket.Date;
                supplierOrReason = "Nhà cung cấp: " + ticket.Supplier.Name;
                createdBy = ticket.User.FullName;
                totalAmount = ticket.TotalCost;

                foreach (var d in ticket.ImportDetails)
                {
                    detailsData.Add(new
                    {
                        Title = d.Book.Title,
                        Quantity = d.Quantity,
                        Price = d.CostPrice,
                        Subtotal = d.Subtotal
                    });
                }
            }
            else // Export
            {
                var ticket = await _context.ExportTickets
                    .Include(t => t.Reference)
                    .Include(t => t.User)
                    .Include(t => t.ExportDetails).ThenInclude(d => d.Book)
                    .FirstOrDefaultAsync(t => t.ExportId == id);

                if (ticket == null) return NotFound();

                ticketTitle = "PHIẾU XUẤT KHO";
                docNumber = ticket.DocumentNumber;
                date = ticket.Date;

                // Xử lý hiển thị lý do hoặc đơn hàng tham chiếu
                string refStr = ticket.Reason == "Sale" && ticket.Reference != null
                    ? $"Đơn hàng #{ticket.Reference.OrderId}"
                    : ticket.Reason;
                supplierOrReason = "Lý do/Tham chiếu: " + refStr;

                createdBy = ticket.User.FullName;

                // Phiếu xuất có thể không lưu tổng tiền trong header, ta tính tổng từ detail
                totalAmount = ticket.ExportDetails.Sum(x => x.Subtotal);

                foreach (var d in ticket.ExportDetails)
                {
                    detailsData.Add(new
                    {
                        Title = d.Book.Title,
                        Quantity = d.Quantity,
                        Price = d.UnitPrice,
                        Subtotal = d.Subtotal
                    });
                }
            }

            // --- BẮT ĐẦU VẼ EXCEL ---

            // 1. Tiêu đề lớn
            worksheet.Cells["A1:E1"].Merge = true;
            worksheet.Cells["A1"].Value = ticketTitle;
            worksheet.Cells["A1"].Style.Font.Size = 16;
            worksheet.Cells["A1"].Style.Font.Bold = true;
            worksheet.Cells["A1"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

            // 2. Thông tin chung (Header)
            worksheet.Cells["A3"].Value = "Mã phiếu:";
            worksheet.Cells["B3"].Value = docNumber;
            worksheet.Cells["B3"].Style.Font.Bold = true;

            worksheet.Cells["D3"].Value = "Ngày tạo:";
            worksheet.Cells["E3"].Value = date?.ToString("dd/MM/yyyy HH:mm");

            worksheet.Cells["A4"].Value = "Thông tin:";
            worksheet.Cells["B4"].Value = supplierOrReason;

            worksheet.Cells["A5"].Value = "Người tạo:";
            worksheet.Cells["B5"].Value = createdBy;

            // 3. Header bảng chi tiết (Dòng 7)
            int tableRow = 7;
            worksheet.Cells[tableRow, 1].Value = "STT";
            worksheet.Cells[tableRow, 2].Value = "Tên Sách";
            worksheet.Cells[tableRow, 3].Value = "Số Lượng";
            worksheet.Cells[tableRow, 4].Value = "Đơn Giá (VNĐ)";
            worksheet.Cells[tableRow, 5].Value = "Thành Tiền (VNĐ)";

            // Style cho header bảng
            using (var range = worksheet.Cells[tableRow, 1, tableRow, 5])
            {
                range.Style.Font.Bold = true;
                range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
                range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
            }

            // 4. Đổ dữ liệu
            int stt = 1;
            tableRow++; // Bắt đầu từ dòng 8
            foreach (var item in detailsData)
            {
                worksheet.Cells[tableRow, 1].Value = stt++;
                worksheet.Cells[tableRow, 2].Value = item.Title;
                worksheet.Cells[tableRow, 3].Value = item.Quantity;
                worksheet.Cells[tableRow, 4].Value = item.Price;
                worksheet.Cells[tableRow, 5].Value = item.Subtotal;

                // Format số
                worksheet.Cells[tableRow, 4].Style.Numberformat.Format = "#,##0";
                worksheet.Cells[tableRow, 5].Style.Numberformat.Format = "#,##0";

                tableRow++;
            }

            // 5. Tổng cộng (Footer)
            worksheet.Cells[tableRow, 4].Value = "TỔNG CỘNG:";
            worksheet.Cells[tableRow, 4].Style.Font.Bold = true;

            worksheet.Cells[tableRow, 5].Value = totalAmount;
            worksheet.Cells[tableRow, 5].Style.Font.Bold = true;
            worksheet.Cells[tableRow, 5].Style.Numberformat.Format = "#,##0";

            // Tự động chỉnh độ rộng cột
            worksheet.Cells.AutoFitColumns();

            // Trả về file
            string fileName = $"{ticketTitle}_{docNumber}_{DateTime.Now:yyyyMMdd}.xlsx";
            return File(package.GetAsByteArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }
    }

    public class ImportDetailUpdateModel
    {
        public int DetailId { get; set; }
        public int Quantity { get; set; }
    }

    // =========================================================================
    // CÁC VIEWMODEL/HELPER CLASS MỚI DÀNH CHO IMPORT
    // =========================================================================

    // ViewModel để nhận file được upload
    public class ImportExcelViewModel
    {
        public IFormFile ExcelFile { get; set; }
    }

    // Class chi tiết phiếu nhập đọc từ Excel (dùng để trả về JSON)
    public class ImportDetailViewModel
    {
        public int BookId { get; set; }
        public string Title { get; set; }
        public int Quantity { get; set; }
        public decimal CostPrice { get; set; }
    }

    // Class chi tiết phiếu xuất đọc từ Excel (dùng để trả về JSON)
    public class ExportDetailViewModel
    {
        public int BookId { get; set; }
        public string Title { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; } // Giá bán (dùng để tính toán/hiển thị)
    }

    // Class để lưu kết quả và lỗi Import (nếu cần xử lý phức tạp hơn)
    public class ImportResult
    {
        public int SuccessCount { get; set; }
        public List<string> Errors { get; set; } = new List<string>();
        public bool HasError => Errors.Any();

        public void AddError(string error) => Errors.Add(error);
    }
}