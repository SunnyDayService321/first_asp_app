using Microsoft.AspNetCore.Mvc;

namespace first_asp_app.Controllers;

public class CompanyController : Controller
{
    private readonly ILogger<CompanyController> _logger;

    public CompanyController(ILogger<CompanyController> logger)
    {
        _logger = logger;
    }

    // 会社概要ページ
    public IActionResult About()
    {
        _logger.LogInformation("会社概要ページにアクセス");
        return View();
    }

    // プライバシーポリシーページ
    public IActionResult Privacy()
    {
        _logger.LogInformation("プライバシーポリシーページにアクセス");
        return View();
    }
}
