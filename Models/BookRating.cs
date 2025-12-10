using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BookstoreManagement.Models;

public partial class BookRating
{
    [Key]
    public int RatingId { get; set; }

    public int BookId { get; set; }

    public string UserId { get; set; } = null!;

    [Range(1, 5)]
    public int RatingValue { get; set; } // 1 đến 5 sao

    [Column(TypeName = "nvarchar(500)")]
    public string? Comment { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    // Navigation properties
    public virtual Book Book { get; set; } = null!;
    public virtual AppUser User { get; set; } = null!;
}