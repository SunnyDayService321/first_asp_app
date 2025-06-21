
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

    // 注文完了画面表示
    public IActionResult OrderComplete(int id)
    {
        return View(id);
    }

    // 注文確定処理（重要：トランザクション必須）
    [HttpPost]
    public async Task<IActionResult> ConfirmOrder(string address, string paymentMethod)
    {
        // バリデーション処理（トランザクション外）
        // 入力値のバリデーション
        if (string.IsNullOrWhiteSpace(address))
        {
            TempData["ErrorMessage"] = "配送先住所を入力してください";
            return RedirectToAction("Checkout");
        }

        if (string.IsNullOrWhiteSpace(paymentMethod))
        {
            TempData["ErrorMessage"] = "支払方法を選択してください";
            return RedirectToAction("Checkout");
        }

        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

        // カート内の商品を取得
        var cartItems = await _context.CartItems
            .Include(c => c.Product)
            .Where(c => c.UserId == userId)
            .ToListAsync();

        if (!cartItems.Any())
        {
            TempData["ErrorMessage"] = "カートに商品がありません";
            return RedirectToAction("Index");
        }

        // 最新の在庫情報を取得
        var productIds = cartItems.Select(c => c.ProductId).ToList();
        var products = await _context.Product
            .Where(p => productIds.Contains(p.Id))
            .ToListAsync();

        // 在庫チェックと合計金額計算
        decimal totalAmount = 0;
        foreach (var item in cartItems)
        {
            var product = products.FirstOrDefault(p => p.Id == item.ProductId);
            if (product == null)
            {
                TempData["ErrorMessage"] = "商品情報が見つかりません";
                return RedirectToAction("Checkout");
            }

            // 在庫チェック
            if (product.StockQuantity < item.Quantity)
            {
                TempData["ErrorMessage"] = $"{product.Name}の在庫が不足しています（現在の在庫: {product.StockQuantity}個）";
                return RedirectToAction("Checkout");
            }

            totalAmount += product.Price * item.Quantity;
        }

        // データベース更新処理（トランザクション内）
        using (var transaction = _context.Database.BeginTransaction())
        {
            try
            {
                // 注文作成
                var order = new Order
                {
                    UserId = userId,
                    OrderDate = DateTime.Now,
                    TotalAmount = totalAmount + 500
                };

                _context.Order.Add(order);
                _context.SaveChanges(); // Orderを保存してIDを取得

                // 注文詳細作成と在庫更新
                foreach (var item in cartItems)
                {
                    var product = products.FirstOrDefault(p => p.Id == item.ProductId);

                    // 注文詳細作成
                    var orderItem = new OrderItem
                    {
                        OrderId = order.Id,
                        ProductId = item.ProductId,
                        Quantity = item.Quantity,
                        Price = product.Price
                    };

                    _context.OrderItem.Add(orderItem);

                    // 在庫更新
                    product.StockQuantity -= item.Quantity;
                    _context.Update(product);
                }

                // カートをクリア
                _context.CartItems.RemoveRange(cartItems);

                // 全ての変更を保存
                _context.SaveChanges();

                // トランザクションをコミット
                transaction.Commit();

                TempData["SuccessMessage"] = "注文が完了しました";
                _logger.LogInformation($"注文ID:{order.Id}が完了 - ユーザーID:{userId}");

                return RedirectToAction("OrderComplete", new { id = order.Id });
            }
            catch (Exception ex)
            {
                // エラー時はロールバック
                transaction.Rollback();
                _logger.LogError($"データベース更新でエラー: {ex.Message}");
                TempData["ErrorMessage"] = "注文の処理に失敗しました。もう一度お試しください。";
                return RedirectToAction("Checkout");
            }
        }
    }

    // 注文手続きページ表示
    public async Task<IActionResult> Checkout()
    {
        try
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

            // カート内の商品を取得
            var cartItems = await _context.CartItems
                .Include(c => c.Product)
                .Where(c => c.UserId == userId)
                .ToListAsync();

            // カートが空の場合
            if (!cartItems.Any())
            {
                TempData["ErrorMessage"] = "カートに商品がありません";
                return RedirectToAction("Index");
            }

            // 在庫チェック
            foreach (var item in cartItems)
            {
                if (item.Product.StockQuantity < item.Quantity)
                {
                    TempData["ErrorMessage"] = $"{item.Product.Name}の在庫が不足しています";
                    return RedirectToAction("Index");
                }
            }

            return View(cartItems);
        }
        catch (Exception ex)
        {
            _logger.LogError($"注文手続きでエラー: {ex.Message}");
            TempData["ErrorMessage"] = "注文手続きに失敗しました";
            return RedirectToAction("Index");
        }
    }
}
