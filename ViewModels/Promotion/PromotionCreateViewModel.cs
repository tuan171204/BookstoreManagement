using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace BookstoreManagement.ViewModels.Promotion
{
    public class PromotionCreateViewModel
    {
        [Required(ErrorMessage = "Tên chương trình là bắt buộc")]
        [StringLength(100, ErrorMessage = "Tên chương trình không được vượt quá 100 ký tự")]
        [Display(Name = "Tên chương trình khuyến mãi")]
        public string Name { get; set; } = null!;

        [Required(ErrorMessage = "Vui lòng chọn loại khuyến mãi")]
        [Display(Name = "Loại khuyến mãi")]
        public int TypeId { get; set; }

        [Range(0, 100, ErrorMessage = "Phần trăm giảm giá phải từ 0 đến 100")]
        [Display(Name = "Phần trăm giảm giá (%)")]
        public decimal? DiscountPercent { get; set; }

        [Display(Name = "Ngày bắt đầu")]
        [DataType(DataType.Date)]
        public DateTime? StartDate { get; set; }

        [Display(Name = "Ngày kết thúc")]
        [DataType(DataType.Date)]
        public DateTime? EndDate { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Giá trị đơn hàng tối thiểu phải lớn hơn 0")]
        [Display(Name = "Giá trị đơn hàng tối thiểu (₫)")]
        public decimal? MinPurchaseAmount { get; set; }

        [Display(Name = "Sách quà tặng (tùy chọn)")]
        public int? GiftBookId { get; set; }

        [Display(Name = "Kích hoạt ngay")]
        public bool IsActive { get; set; } = true;

        // For dropdowns
        public List<SelectListItem>? PromotionTypes { get; set; }
        public List<SelectListItem>? Books { get; set; }

        // For multi-select books
        [Display(Name = "Chọn sách áp dụng khuyến mãi")]
        public List<int>? SelectedBookIds { get; set; }

        // Dùng để đổ dữ liệu vào dropdown chọn nhiều
        public SelectList? AvailableBooks { get; set; }

        [Display(Name = "Kênh áp dụng")]
        public string ApplyChannel { get; set; } = "All";

        [Display(Name = "Phạm vi áp dụng")]
        public string ApplyType { get; set; } = "Specific";
    }
}
