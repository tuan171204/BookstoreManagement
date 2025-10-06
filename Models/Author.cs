using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace BookstoreManagement.Models;

public partial class Author
{
    public int AuthorId { get; set; }

    [Column(TypeName = "nvarchar(100)")]
    public string Name { get; set; } = null!;
    [Column(TypeName = "nvarchar(255)")]
    public string? Bio { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<Book> Books { get; set; } = new List<Book>();
}
