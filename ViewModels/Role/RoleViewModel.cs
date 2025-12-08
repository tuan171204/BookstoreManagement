using System.ComponentModel.DataAnnotations;
using BookstoreManagement.Models;

namespace BookstoreManagement.ViewModels.Role
{
    public class RoleViewModel
    {
        public string? Id { get; set; }

        [Required(ErrorMessage = "Tên chức vụ không được bỏ trống")]
        [Display(Name = "Tên chức vụ")]
        public string Name { get; set; } = null!;

        [Display(Name = "Mô tả")]
        public string? Description { get; set; }

        [Display(Name = "Lương cơ bản")]
        [Range(0, double.MaxValue, ErrorMessage = "Lương phải lớn hơn hoặc bằng 0")]
        public decimal Salary { get; set; }

        public List<Permission>? AllPermissions { get; set; }

        // Danh sách ID các quyền đã được chọn cho Role này
        public List<int> SelectedPermissionIds { get; set; } = new List<int>();
    }
}