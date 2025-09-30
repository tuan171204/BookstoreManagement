using System;
using System.Collections.Generic;

namespace BookstoreManagement.Models;

public partial class BookPromotion
{
    public int BookId { get; set; }

    public int PromotionId { get; set; }

    public virtual Book Book { get; set; } = null!;

    public virtual Promotion Promotion { get; set; } = null!;
}
