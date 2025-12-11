using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace BookstoreManagement.ViewModels.Book
{
    public class BookEditViewModel
    {
        public int BookId { get; set; }

        [Required(ErrorMessage = "Tên sách là bắt buộc")]
        [StringLength(100, ErrorMessage = "Tên sách không được vượt quá 100 ký tự")]
        [Display(Name = "Tên sách")]
        public string Title { get; set; } = null!;

        [Display(Name = "Hình ảnh hiện tại")]
        public string? ImageUrl { get; set; }

        [Display(Name = "Chọn ảnh mới (nếu có)")]
        public IFormFile? ImageFile { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn tác giả")]
        [Display(Name = "Tác giả")]
        public int AuthorId { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn nhà xuất bản")]
        [Display(Name = "Nhà xuất bản")]
        public int PublisherId { get; set; }

        [Range(1900, 2100, ErrorMessage = "Năm xuất bản phải từ 1900 đến 2100")]
        [Display(Name = "Năm xuất bản")]
        public int? PublicationYear { get; set; }

        [Required(ErrorMessage = "Giá bán là bắt buộc")]
        [Range(0, double.MaxValue, ErrorMessage = "Giá bán phải lớn hơn 0")]
        [Display(Name = "Giá bán (₫)")]
        public decimal Price { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Số lượng tồn kho phải lớn hơn hoặc bằng 0")]
        [Display(Name = "Số lượng tồn kho")]
        public int? StockQuantity { get; set; }

        [StringLength(500, ErrorMessage = "Mô tả không được vượt quá 500 ký tự")]
        [Display(Name = "Mô tả")]
        [DataType(DataType.MultilineText)]
        public string? Description { get; set; }

        [Display(Name = "Nhà cung cấp")]
        [Required(ErrorMessage = "Vui lòng chọn nhà cung cấp")]
        public int SupplierId { get; set; }

        [Display(Name = "Giá nhập")]
        [Required(ErrorMessage = "Giá nhập là bắt buộc")]
        [Range(0, double.MaxValue, ErrorMessage = "Giá nhập phải lớn hơn 0")]
        public decimal DefaultCostPrice { get; set; }

        [Display(Name = "Giá nhập (Giá vốn)")]
        [Required(ErrorMessage = "Vui lòng nhập giá vốn")]
        [Range(0, double.MaxValue, ErrorMessage = "Giá vốn phải lớn hơn 0")]
        public decimal CostPrice { get; set; }

        [Display(Name = "% Lợi nhuận")]
        [Required(ErrorMessage = "Vui lòng nhập % lợi nhuận")]
        [Range(0, 500, ErrorMessage = "% Lợi nhuận không hợp lệ")]
        public double ProfitMargin { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Ngưỡng cảnh báo phải lớn hơn hoặc bằng 0")]
        [Display(Name = "Ngưỡng cảnh báo tồn kho thấp")]
        public int? LowStockThreshold { get; set; }

        public bool? IsDeleted { get; set; }

        // Dropdown lists
        public List<SelectListItem>? Authors { get; set; }
        public List<SelectListItem>? Publishers { get; set; }
        public IEnumerable<SelectListItem>? SupplierList { get; set; }
        public List<SelectListItem>? Categories { get; set; }
        public List<int>? SelectedCategoryIds { get; set; }
    }
}
