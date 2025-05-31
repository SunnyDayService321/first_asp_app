using System.ComponentModel.DataAnnotations;

namespace first_asp_app.Models;

public class CartItem
{
    public int Id { get; set; }

    [Required]
    public int UserId { get; set; }

    [Required]
    public int ProductId { get; set; }

    public Product? Product { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "数量は1以上である必要があります")]
    public int Quantity { get; set; }
}
