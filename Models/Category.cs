using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace BookstoreManagement.Models;

public partial class Category
{
    public int CategoryId { get; set; }

    [Column(TypeName = "nvarchar(100)")]
    public string Name { get; set; } = null!;

    [Column(TypeName = "nvarchar(100)")]
    public string? Description { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }
}
