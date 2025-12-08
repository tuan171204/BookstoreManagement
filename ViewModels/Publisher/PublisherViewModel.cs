using System.ComponentModel.DataAnnotations;

namespace BookstoreManagement.ViewModels.Publisher
{
    public class PublisherViewModel
    {
        public int PublisherId { get; set; }
        public string Name { get; set; } = null!;
        public string? Address { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string? Website { get; set; }
        public string? ImageUrl { get; set; }
        public string? Description { get; set; }

        public int BooksCount { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}