using Microsoft.AspNetCore.Mvc;
using first_asp_app.Models;
using first_asp_app.Data;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

namespace first_asp_app.Controllers;

public class AccountController : Controller
{
    private readonly MvcMovieContext _context;
    private readonly ILogger<AccountController> _logger;

    public AccountController(MvcMovieContext context, ILogger<AccountController> logger)
    {
        _context = context;
        _logger = logger;
    }


    [HttpGet]
    public IActionResult Login(string returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        return View();
    }

    [HttpGet]
    public IActionResult Register()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Register(User user)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return View(user);
            }

            // メールアドレスの重複チェック
            if (await _context.User.AnyAsync(u => u.Email == user.Email))
            {
                ModelState.AddModelError("Email", "このメールアドレスは既に登録されています。");
                return View(user);
            }

            // パスワードの複雑さチェック
            if (string.IsNullOrEmpty(user.PasswordHash) || user.PasswordHash.Length < 6)
            {
                ModelState.AddModelError("PasswordHash", "パスワードは6文字以上必要です。");
                return View(user);
            }

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(user.PasswordHash);
            user.CreatedAt = DateTime.UtcNow;

            _context.User.Add(user);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"新規ユーザーが登録されました: {user.Email}");

            TempData["SuccessMessage"] = "登録が完了しました。ログインしてください。";

            return RedirectToAction("Login");
        }
        catch (Exception ex)
        {
            _logger.LogError($"ユーザー登録中にエラーが発生しました: {ex.Message}");
            ModelState.AddModelError("", "登録中にエラーが発生しました。もう一度お試しください。");
            return View(user);
        }
    }

    [HttpPost]
    public async Task<IActionResult> Login(string email, string password, string returnUrl = null)
    {
        try
        {
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                ModelState.AddModelError("", "メールアドレスとパスワードを入力してください。");
                return View();
            }

            var user = await _context.User.FirstOrDefaultAsync(u => u.Email == email);

            if (user == null)
            {
                ModelState.AddModelError("", "メールアドレスまたはパスワードが正しくありません。");
                return View();
            }

            if (!BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
            {
                _logger.LogWarning($"ログイン失敗: {email}");
                ModelState.AddModelError("", "メールアドレスまたはパスワードが正しくありません。");
                return View();
            }

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.Email),
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email)
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme, 
                principal,
                new AuthenticationProperties
                {
                    IsPersistent = true, // Remember meの機能
                    ExpiresUtc = DateTime.UtcNow.AddDays(30) // 30日間有効
                }
            );

            _logger.LogInformation($"ログイン成功: {email}");

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return RedirectToAction("Index", "Home");
        }
        catch (Exception ex)
        {
            _logger.LogError($"ログイン処理中にエラーが発生しました: {ex.Message}");
            ModelState.AddModelError("", "ログイン中にエラーが発生しました。もう一度お試しください。");
            return View();
        }
    }

    [HttpPost]
    public async Task<IActionResult> Logout()
    {
        try
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            _logger.LogInformation($"ユーザーがログアウトしました: {User.Identity?.Name}");
            return RedirectToAction("Index", "Home");
        }
        catch (Exception ex)
        {
            _logger.LogError($"ログアウト中にエラーが発生しました: {ex.Message}");
            return RedirectToAction("Index", "Home");
        }
    }

    // アクセス拒否処理
    public IActionResult AccessDenied(string returnUrl = null)
    {
        ViewBag.ReturnUrl = returnUrl;
        return View();
    }
}
