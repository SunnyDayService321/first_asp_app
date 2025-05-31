using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using first_asp_app.Data;
using first_asp_app.Models;

namespace first_asp_app.Controllers;

[Authorize]
public class CartController : Controller
{
    private readonly MvcMovieContext _context;
    private readonly ILogger<CartController> _logger;

    public CartController(MvcMovieContext context, ILogger<CartController> logger)
    {
        _context = context;
        _logger = logger;
    }

    // カート内容表示
    public async Task<IActionResult> Index()
    {
        _logger.LogInformation("Cart/Index メソッド呼び出し");

        // ログインユーザーのIDを取得
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

        // カート内の商品を取得
        var cartItems = await _context.CartItems
            .Include(c => c.Product)
            .Where(c => c.UserId == userId)
            .ToListAsync();

        _logger.LogInformation($"カート内商品数: {cartItems.Count}");

        return View(cartItems);
    }

    // カートから商品を削除
    [HttpPost]
    public async Task<IActionResult> RemoveFromCart(int cartItemId)
    {
        try
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

            var cartItem = await _context.CartItems
                .FirstOrDefaultAsync(c => c.Id == cartItemId && c.UserId == userId);

            if (cartItem == null)
            {
                TempData["ErrorMessage"] = "指定されたカートアイテムが見つかりません";
                return RedirectToAction("Index");
            }

            _context.CartItems.Remove(cartItem);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "商品をカートから削除しました";
            _logger.LogInformation($"カートアイテムID:{cartItemId}を削除");

            return RedirectToAction("Index");
        }
        catch (Exception ex)
        {
            _logger.LogError($"カートからの削除でエラー: {ex.Message}");
            TempData["ErrorMessage"] = "カートからの削除に失敗しました";
            return RedirectToAction("Index");
        }
    }

    // カート内商品の数量更新
    [HttpPost]
    public async Task<IActionResult> UpdateQuantity(int cartItemId, int quantity)
    {
        try
        {
            if (quantity <= 0)
            {
                TempData["ErrorMessage"] = "数量は1以上を指定してください";
                return RedirectToAction("Index");
            }

            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

            var cartItem = await _context.CartItems
                .Include(c => c.Product)
                .FirstOrDefaultAsync(c => c.Id == cartItemId && c.UserId == userId);

            if (cartItem == null)
            {
                TempData["ErrorMessage"] = "指定されたカートアイテムが見つかりません";
                return RedirectToAction("Index");
            }

            // 在庫確認
            if (cartItem.Product.StockQuantity < quantity)
            {
                TempData["ErrorMessage"] = "在庫が不足しています";
                return RedirectToAction("Index");
            }

            cartItem.Quantity = quantity;
            _context.Update(cartItem);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "数量を更新しました";
            _logger.LogInformation($"カートアイテムID:{cartItemId}の数量を{quantity}に更新");

            return RedirectToAction("Index");
        }
        catch (Exception ex)
        {
            _logger.LogError($"数量更新でエラー: {ex.Message}");
            TempData["ErrorMessage"] = "数量の更新に失敗しました";
            return RedirectToAction("Index");
        }
    }

    // カートをクリア
    [HttpPost]
    public async Task<IActionResult> ClearCart()
    {
        try
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

            var cartItems = await _context.CartItems
                .Where(c => c.UserId == userId)
                .ToListAsync();

            _context.CartItems.RemoveRange(cartItems);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "カートをクリアしました";
            _logger.LogInformation($"ユーザーID:{userId}のカートをクリア");

            return RedirectToAction("Index");
        }
        catch (Exception ex)
        {
            _logger.LogError($"カートクリアでエラー: {ex.Message}");
            TempData["ErrorMessage"] = "カートのクリアに失敗しました";
            return RedirectToAction("Index");
        }
    }

    // デバッグ用のアクション
    public IActionResult Debug()
    {
        return Content("Cart Controller is working");
    }
}
