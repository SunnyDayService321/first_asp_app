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

    // IsValidEmail メソッドを AccountController クラス内に追加
    private bool IsValidEmail(string email)
    {
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
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
            // モデル検証
            if (!ModelState.IsValid)
            {
                return View(user);
            }

            // 必須項目チェック
            if (string.IsNullOrEmpty(user.Email) ||
                string.IsNullOrEmpty(user.PasswordHash))
            {
                ModelState.AddModelError("", "リクエストの内容が不正です。");
                return View(user);
            }

            // user_id（メールアドレス）のチェック
            if (string.IsNullOrEmpty(user.Email))
            {
                ModelState.AddModelError(nameof(user.Email), "メールアドレスとパスワードを入力してください。");
                return View(user);
            }

            // メールアドレスの入力チェック
            if (string.IsNullOrEmpty(user.Email))
            {
                ModelState.AddModelError("Email", "メールアドレスとパスワードを入力してください。");
                return View(user);
            }

            // メールアドレス形式チェック
            if (!IsValidEmail(user.Email))
            {
                ModelState.AddModelError("Email", "正しいメールアドレスの形式で入力してください。");
                return View(user);
            }

            // メールアドレス桁数チェック
            if (user.Email.Length > 50)
            {
                ModelState.AddModelError("Email", "メールアドレスは50桁以内で入力してください。");
                return View(user);
            }

            // パスワードの必須チェック
            if (string.IsNullOrEmpty(user.PasswordHash))
            {
                ModelState.AddModelError("PasswordHash", "パスワードは必須です。");
                return View(user);
            }

            // パスワードの桁数チェック
            if (user.PasswordHash.Length > 20)
            {
                ModelState.AddModelError("PasswordHash", "パスワードは20桁以内で入力してください。");
                return View(user);
            }

            // メールアドレスの重複チェック
            if (await _context.User.AnyAsync(u => u.Email == user.Email))
            {
                ModelState.AddModelError("Email", "このメールアドレスは既に登録されています。");
                return View(user);
            }

            // パスワードの複雑さチェックÏ
            if (string.IsNullOrEmpty(user.PasswordHash) || user.PasswordHash.Length < 6)
            {
                ModelState.AddModelError("PasswordHash", "パスワードは6文字以上必要です。");
                return View(user);
            }

            // パスワードのハッシュ化とユーザー情報の設定
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(user.PasswordHash);
            user.CreatedAt = DateTime.UtcNow;

            // データベースに登録
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
    public async Task<IActionResult> Login(string? email, string? password, string? returnUrl = null)
    {
        try
        {
            // メールアドレス桁数チェック（50桁以内）
            if (string.IsNullOrEmpty(email) || email.Length > 50)
            {
                ModelState.AddModelError(nameof(email), "メールアドレスは50桁以内で入力してください。");
                return View();
            }

            // パスワード桁数チェック（20桁以内）
            if (string.IsNullOrEmpty(password) || password.Length > 20)
            {
                ModelState.AddModelError(nameof(password), "パスワードは20桁以内で入力してください。");
                return View();
            }

            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                ModelState.AddModelError(string.Empty, "メールアドレスとパスワードを入力してください。");
                return View();
            }

            // 先に変数を宣言
            User? user = null;

            // メールアドレスの存在チェック
            user = await _context.User.FirstOrDefaultAsync(u => u.Email == email);

            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "正しいメールアドレスの形式で入力してください。");
                return View();
            }

            // パスワード検証
            if (!BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
            {
                _logger.LogWarning($"ログイン失敗: {email}");
                ModelState.AddModelError(string.Empty, "メールアドレスまたはパスワードが正しくありません。");
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
            ModelState.AddModelError(string.Empty, "ログイン中にエラーが発生しました。もう一度お試しください。");
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
