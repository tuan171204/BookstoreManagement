using System.ComponentModel.DataAnnotations;

namespace BookstoreManagement.ViewModels.Promotion
{
    public class PromotionViewModel
    {
        public int PromotionId { get; set; }

        [Display(Name = "Tên chương trình")]
        public string Name { get; set; } = null!;

        [Display(Name = "Loại khuyến mãi")]
        public string TypeName { get; set; } = null!;

        public int TypeId { get; set; }

        [Display(Name = "% Giảm giá")]
        [DisplayFormat(DataFormatString = "{0}%")]
        public decimal? DiscountPercent { get; set; }

        [Display(Name = "Ngày bắt đầu")]
        [DisplayFormat(DataFormatString = "{0:dd/MM/yyyy}")]
        public DateTime? StartDate { get; set; }

        [Display(Name = "Ngày kết thúc")]
        [DisplayFormat(DataFormatString = "{0:dd/MM/yyyy}")]
        public DateTime? EndDate { get; set; }

        [Display(Name = "Giá trị đơn hàng tối thiểu")]
        [DisplayFormat(DataFormatString = "{0:N0} ₫")]
        public decimal? MinPurchaseAmount { get; set; }

        [Display(Name = "Sách quà tặng")]
        public string? GiftBookName { get; set; }

        public int? GiftBookId { get; set; }

        [Display(Name = "Trạng thái")]
        public bool? IsActive { get; set; }

        [Display(Name = "Số sách áp dụng")]
        public int AppliedBooksCount { get; set; }

        [Display(Name = "Ngày tạo")]
        [DisplayFormat(DataFormatString = "{0:dd/MM/yyyy HH:mm}")]
        public DateTime? CreatedAt { get; set; }

        [Display(Name = "Ngày cập nhật")]
        [DisplayFormat(DataFormatString = "{0:dd/MM/yyyy HH:mm}")]
        public DateTime? UpdatedAt { get; set; }

        public string Status => IsActive == true ? "Đang hoạt động" : "Không hoạt động";

        public string StatusBadgeClass => IsActive == true ? "bg-success" : "bg-secondary";
    }
}
