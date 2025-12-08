using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BookstoreManagement.Models;

public partial class Author
{
    public int AuthorId { get; set; }

    [Column(TypeName = "nvarchar(100)")]
    public string Name { get; set; } = null!;

    [Display(Name = "Bút danh")]
    [Column(TypeName = "nvarchar(100)")]
    public string? Pseudonym { get; set; }

    [Display(Name = "Ngày sinh")]
    public DateTime? DateOfBirth { get; set; }

    [Display(Name = "Quốc tịch")]
    [Column(TypeName = "nvarchar(50)")]
    public string? Nationality { get; set; }

    [Display(Name = "Ảnh đại diện")]
    public string? ImageUrl { get; set; }

    [Display(Name = "Website/Wiki")]
    [Column(TypeName = "nvarchar(255)")]
    public string? Website { get; set; }
    // ---------------------------

    [Column(TypeName = "nvarchar(max)")]
    public string? Bio { get; set; }

    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<Book> Books { get; set; } = new List<Book>();
}
