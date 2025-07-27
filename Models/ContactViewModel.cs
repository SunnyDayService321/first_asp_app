using System.ComponentModel.DataAnnotations;

namespace first_asp_app.Models;

public class ContactViewModel
{
    [Required(ErrorMessage = "お名前は必須です")]
    [StringLength(50, ErrorMessage = "お名前は50文字以内で入力してください")]
    [Display(Name = "お名前")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "メールアドレスは必須です")]
    [EmailAddress(ErrorMessage = "正しいメールアドレスの形式で入力してください")]
    [StringLength(100, ErrorMessage = "メールアドレスは100文字以内で入力してください")]
    [Display(Name = "メールアドレス")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "件名は必須です")]
    [StringLength(100, ErrorMessage = "件名は100文字以内で入力してください")]
    [Display(Name = "件名")]
    public string Subject { get; set; } = string.Empty;

    [Required(ErrorMessage = "お問い合わせ内容は必須です")]
    [StringLength(1000, ErrorMessage = "お問い合わせ内容は1000文字以内で入力してください")]
    [Display(Name = "お問い合わせ内容")]
    public string Message { get; set; } = string.Empty;

    [Display(Name = "お問い合わせ種別")]
    public string Category { get; set; } = "一般";

    [Display(Name = "電話番号")]
    [Phone(ErrorMessage = "正しい電話番号の形式で入力してください")]
    [StringLength(20, ErrorMessage = "電話番号は20文字以内で入力してください")]
    public string? PhoneNumber { get; set; }
}
