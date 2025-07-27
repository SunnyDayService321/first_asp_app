using Microsoft.AspNetCore.Mvc;
using first_asp_app.Models;

namespace first_asp_app.Controllers;

public class ContactController : Controller
{
    private readonly ILogger<ContactController> _logger;

    public ContactController(ILogger<ContactController> logger)
    {
        _logger = logger;
    }

    // お問い合わせページ表示
    [HttpGet]
    public IActionResult Index()
    {
        return View(new ContactViewModel());
    }

    // お問い合わせ送信処理
    [HttpPost]
    public IActionResult Index(ContactViewModel model)
    {
        try
        {
            // バリデーション
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // ここで実際のメール送信処理を行う
            // 現在はログ出力のみ
            _logger.LogInformation($"お問い合わせを受信: {model.Name} ({model.Email}) - {model.Subject}");
            _logger.LogInformation($"内容: {model.Message}");

            // 成功メッセージ
            TempData["SuccessMessage"] = "お問い合わせありがとうございます。内容を確認次第、ご連絡いたします。";

            // フォームをクリア
            return View(new ContactViewModel());
        }
        catch (Exception ex)
        {
            _logger.LogError($"お問い合わせ送信でエラー: {ex.Message}");
            TempData["ErrorMessage"] = "送信中にエラーが発生しました。もう一度お試しください。";
            return View(model);
        }
    }
}
