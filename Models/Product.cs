using System.ComponentModel.DataAnnotations;

namespace first_asp_app.Models;

public class Product
{
    public int Id { get; set; }

    [Required]
    [Display(Name = "商品名")]
    public string Name { get; set; } = string.Empty;

    [Display(Name = "説明")]
    public string? Description { get; set; }

    [Required]
    [Range(0, 1000000)]
    [Display(Name = "価格")]
    public decimal Price { get; set; }

    [Display(Name = "在庫数")]
    public int StockQuantity { get; set; }

    // DeleteFlgプロパティを追加
    [Display(Name = "削除フラグ")]
    public int DeleteFlg { get; set; } = 0; // デフォルト値は0（有効）
}
