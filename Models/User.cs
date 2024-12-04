using System.ComponentModel.DataAnnotations;

namespace first_asp_app.Models;

public class User
{
    public int Id { get; set; }

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string PasswordHash { get; set; } = string.Empty;

    [Display(Name = "名前")]
    public string? Name { get; set; }

    public DateTime CreatedAt { get; set; }

    // ナビゲーションプロパティ
    public List<Order> Orders { get; set; } = new();
}
