using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace BookstoreManagement.Models;

public partial class Publisher
{
    public int PublisherId { get; set; }

    [Column(TypeName = "nvarchar(100)")]
    public string Name { get; set; } = null!;

    [Column(TypeName = "nvarchar(255)")]
    public string? Address { get; set; }

    [Column(TypeName = "varchar(20)")]
    public string? Phone { get; set; }

    [Column(TypeName = "varchar(100)")]
    public string? Email { get; set; }

    [Column(TypeName = "nvarchar(255)")]
    public string? Website { get; set; }

    public string? ImageUrl { get; set; }

    [Column(TypeName = "nvarchar(max)")]
    public string? Description { get; set; }

    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<Book> Books { get; set; } = new List<Book>();
}