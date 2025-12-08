using System.ComponentModel.DataAnnotations;

namespace BookstoreManagement.ViewModels.Book
{
    public class BookViewModel
    {
        public int BookId { get; set; }

        [Display(Name = "Tên sách")]
        public string Title { get; set; } = null!;

        [Display(Name = "Hình ảnh")]
        public string? ImageUrl { get; set; }

        [Display(Name = "Tác giả")]
        public string AuthorName { get; set; } = null!;

        public int AuthorId { get; set; }

        [Display(Name = "Nhà xuất bản")]
        public string PublisherName { get; set; } = null!;

        public int PublisherId { get; set; }

        [Display(Name = "Năm xuất bản")]
        public int? PublicationYear { get; set; }

        [Display(Name = "Giá bán")]
        [DisplayFormat(DataFormatString = "{0:N0} ₫")]
        public decimal Price { get; set; }

        [Display(Name = "Tồn kho")]
        public int? StockQuantity { get; set; }

        [Display(Name = "Mô tả")]
        public string? Description { get; set; }

        [Display(Name = "Nhà cung cấp")]
        public string? SupplierName { get; set; }

        [Display(Name = "Giá nhập")]
        public decimal DefaultCostPrice { get; set; }

        [Display(Name = "Ngưỡng cảnh báo")]
        public int? LowStockThreshold { get; set; }

        [Display(Name = "Ngày tạo")]
        [DisplayFormat(DataFormatString = "{0:dd/MM/yyyy HH:mm}")]
        public DateTime? CreatedAt { get; set; }

        [Display(Name = "Ngày cập nhật")]
        [DisplayFormat(DataFormatString = "{0:dd/MM/yyyy HH:mm}")]
        public DateTime? UpdatedAt { get; set; }

        public bool? IsDeleted { get; set; }

        [Display(Name = "Trạng thái")]
        public string Status => IsDeleted == true ? "Đã xóa" : "Hoạt động";

        public List<string> CategoryNames { get; set; } = new List<string>();
    }
}
