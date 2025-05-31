using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using first_asp_app.Models;
using first_asp_app.Data;
using System.Security.Claims;

namespace first_asp_app.Controllers;
public class ProductController : Controller
{
    private readonly MvcMovieContext _context;

    public ProductController(MvcMovieContext context)
    {
        _context = context;
    }

    // 商品一覧表示
    public async Task<IActionResult> Index()
    {
        try
        {
            // 全商品を取得
            var products = await _context.Product
                .Where(p => p.DeleteFlg == 0) // 論理削除された商品を除外
                .ToListAsync();
            // 全商品を取得
            // var products = await _context.Product.ToListAsync();

            // 商品が存在しない場合
            if (products == null || !products.Any())
            {
                ViewBag.ErrorMessage = "表示可能な商品がありません。";
                return View(new List<Product>());
            }

            // 在庫切れ商品を確認
            var inStockProducts = products.Where(p => p.StockQuantity > 0).ToList();

            // 全商品が在庫切れの場合
            if (!inStockProducts.Any())
            {
                ViewBag.ErrorMessage = "この商品は在庫切れです。";
                return View(products);
            }

            return View(products);
        }
            catch (Exception ex)
        {
            Console.WriteLine($"エラー: {ex.Message}");
            ViewBag.ErrorMessage = "商品情報の取得中にエラーが発生しました。";
            return View(new List<Product>());
        }

    }

    // 商品詳細表示
    public async Task<IActionResult> Details(int? id)
    {
        // if (id == null)
        // {
        //     return NotFound();
        // }

        // var product = await _context.Product.FirstOrDefaultAsync(m => m.Id == id);
        // if (product == null)
        // {
        //     return NotFound();
        // }

        // return View(product);
        // 商品詳細表示

    // 2.1.1 バリデーション処理
        if (id == null || id <= 0)
        {
            // ログ出力
            LogError("リクエストの内容が不正です。", id);
            return BadRequest("リクエストの内容が不正です。");
        }

        try
        {
            // 2.2.1 データ抽出
            var products = await _context.Product
                .Where(p => p.Id == id)
                .ToListAsync();

            // 2.2.2 結果が0件の場合
            if (!products.Any())
            {
                // ログ出力
                LogError("idに対して商品データがみつからなかった。", id);
                ViewBag.ErrorMessage = "商品情報が見つかりませんでした。";
                return View("Error");
            }

            // 2.2.3 結果が2件以上の場合
            if (products.Count > 1)
            {
                // ログ出力
                LogError("idに対して商品データが複数件抽出された。", id);
                ViewBag.ErrorMessage = "商品情報の取得に失敗しました。";
                return View("Error");
            }

            // 2.2.4 結果が1件の場合、データをビューに渡す
            var product = products.First();
            LogSuccess("商品詳細取得成功", id);
            return View(product);
        }
        catch (Exception ex)
        {
            // 例外発生時
            LogError($"例外が発生しました: {ex.Message}", id);
            ViewBag.ErrorMessage = "商品情報の取得に失敗しました。";
            return View("Error");
        }
    }

    // ログ出力メソッド
    private void LogSuccess(string message, int? id)
    {
        Console.WriteLine($"成功 - {DateTime.Now} - ID:{id} - {message}");
    }

    private void LogError(string message, int? id)
    {
        Console.WriteLine($"失敗 - {DateTime.Now} - ID:{id} - {message}");
    }

    // カートに商品を追加（新規追加）
    [HttpPost]
    public async Task<IActionResult> AddToCart(int productId, int quantity = 1)
    {
        try
        {
            // ログインチェック
            if (!User.Identity.IsAuthenticated)
            {
                TempData["ErrorMessage"] = "ログインが必要です。";
                return RedirectToAction("Login", "Account");
            }

            // バリデーション
            if (productId <= 0)
            {
                TempData["ErrorMessage"] = "不正な商品IDです";
                return RedirectToAction("Index");
            }

            if (quantity <= 0)
            {
                TempData["ErrorMessage"] = "数量は1以上を指定してください";
                return RedirectToAction("Index");
            }

            // ログインユーザーのIDを取得
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

            // 商品の存在確認
            var product = await _context.Product.FindAsync(productId);
            if (product == null || product.DeleteFlg == 1)
            {
                TempData["ErrorMessage"] = "指定された商品が見つかりません";
                return RedirectToAction("Index");
            }

            // 在庫確認
            if (product.StockQuantity < quantity)
            {
                TempData["ErrorMessage"] = "在庫が不足しています";
                return RedirectToAction("Index");
            }

            // 既存のカートアイテムをチェック
            var existingCartItem = await _context.CartItems
                .FirstOrDefaultAsync(c => c.UserId == userId && c.ProductId == productId);

            if (existingCartItem != null)
            {
                // 既存のアイテムがある場合は数量を更新
                var newQuantity = existingCartItem.Quantity + quantity;

                // 在庫数を超えないかチェック
                if (newQuantity > product.StockQuantity)
                {
                    TempData["ErrorMessage"] = "カート内の数量と合わせて在庫数を超えています";
                    return RedirectToAction("Index");
                }

                existingCartItem.Quantity = newQuantity;
                _context.Update(existingCartItem);
            }
            else
            {
                // 新しいカートアイテムを作成
                var cartItem = new CartItem
                {
                    UserId = userId,
                    ProductId = productId,
                    Quantity = quantity
                };

                _context.CartItems.Add(cartItem);
            }

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"{product.Name}をカートに追加しました";
            LogSuccess($"商品ID:{productId}、数量:{quantity}をカートに追加", productId);

            // カート画面にリダイレクト
            return RedirectToAction("Index", "Cart");
        }
        catch (Exception ex)
        {
            LogError($"カートへの追加でエラー: {ex.Message}", productId);
            TempData["ErrorMessage"] = "カートへの追加に失敗しました";
            return RedirectToAction("Index");
        }
    }

    // 購入処理
    [HttpPost]
    public async Task<IActionResult> Purchase(int id, int quantity)
    {
        var product = await _context.Product.FindAsync(id);
        if (product == null || product.StockQuantity < quantity)
        {
            return BadRequest("商品が存在しないか、在庫が不足しています。");
        }

        // ユーザーIDを取得（ログイン中のユーザー）
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

        // 注文を作成
        var order = new Order
        {
            UserId = userId,
            OrderDate = DateTime.Now,
            TotalAmount = product.Price * quantity
        };

        var orderItem = new OrderItem
        {
            Order = order,
            ProductId = id,
            Quantity = quantity,
            Price = product.Price
        };

        order.OrderItems.Add(orderItem);

        // 在庫を減らす
        product.StockQuantity -= quantity;

        _context.Order.Add(order);
        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(OrderComplete), new { id = order.Id });
    }

   // 購入画面表示（詳細な購入情報入力用）
   public async Task<IActionResult> Purchase(int? id)
   {
    //    var product = await _context.Product.FindAsync(id);
    //    if (product == null)
    //    {
    //        return NotFound();
    //    }
    //    return View(product);
    // 購入画面表示（詳細な購入情報入力用）

        // 2.1.1 バリデーション処理
        if (id == null || id <= 0)
        {
            LogError("商品IDは数値で入力してください", id);
            ViewBag.ErrorMessage = "商品IDは数値で入力してください";
            return View("Error");
        }

        try
        {
            // 2.2.1 データ抽出 - 仕様に基づく条件
            var product = await _context.Product
                .Where(p => p.Id == id && p.StockQuantity > 0)
                .FirstOrDefaultAsync();

            // 2.2.2 結果が0件の場合
            if (product == null)
            {
                LogError("指定された商品は購入できません", id);
                ViewBag.ErrorMessage = "指定された商品は購入できません";
                return View("Error");
            }

            // 2.3.1 取得結果がある場合、商品購入画面情報を返却
            LogSuccess("商品購入画面表示", id);
            return View(product);
        }
        catch (Exception ex)
        {
            LogError($"例外が発生しました: {ex.Message}", id);
            ViewBag.ErrorMessage = "商品情報の取得に失敗しました";
            return View("Error");
        }
   }
   // 購入確定処理（配送先・支払方法込み）
    // [HttpPost]
    // public async Task<IActionResult> ConfirmPurchase(int id, int quantity, string address, string paymentMethod)
    // {
    //     // バリデーション処理
    //     if (id <= 0)
    //     {
    //         ModelState.AddModelError("id", "不正な商品IDです");
    //         return View("Error");
    //     }

    //     if (quantity <= 0)
    //     {
    //         ModelState.AddModelError("quantity", "購入数量は1以上を指定してください");
    //         return View("Error");
    //     }

    //     if (string.IsNullOrWhiteSpace(address))
    //     {
    //         ModelState.AddModelError("address", "配送先住所を入力してください");
    //         return View("Error");
    //     }

    //     if (address.Length > 200)
    //     {
    //         ModelState.AddModelError("address", "配送先住所は200文字以内で入力してください");
    //         return View("Error");
    //     }

    //     if (string.IsNullOrWhiteSpace(paymentMethod))
    //     {
    //         ModelState.AddModelError("paymentMethod", "支払方法を入力してください");
    //         return View("Error");
    //     }

    //     // 商品の存在と在庫確認
    //     var product = await _context.Product.FindAsync(id);
    //     if (product == null || product.StockQuantity < quantity)
    //     {
    //         ModelState.AddModelError("", "在庫数が不足しています");
    //         return View("Error");
    //     }

    //     // ログインユーザーのID取得
    //     var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

    //     // 注文データの作成
    //     var order = new Order
    //     {
    //         UserId = userId,
    //         OrderDate = DateTime.Now,
    //         TotalAmount = product.Price * quantity + 500  // 送料込みの金額
    //     };

    //     // 注文詳細データの作成
    //     var orderItem = new OrderItem
    //     {
    //         Order = order,
    //         ProductId = id,
    //         Quantity = quantity,
    //         Price = product.Price
    //     };

    //     // 注文に商品を追加
    //     order.OrderItems.Add(orderItem);

    //     // 在庫数の更新
    //     product.StockQuantity -= quantity;

    //     // データベースに注文を保存
    //     _context.Order.Add(order);
    //     await _context.SaveChangesAsync();

    //     // 注文完了画面へリダイレクト
    //     return RedirectToAction(nameof(OrderComplete), new { id = order.Id });
    // }

    [HttpPost]
    public async Task<IActionResult> ConfirmPurchase(int id, int quantity, string address, string paymentMethod)
    {

        // 商品の存在確認
        var product = await _context.Product.FindAsync(id);

        // バリデーション処理
        if (id <= 0)
        {
            ModelState.AddModelError("id", "不正な商品IDです");
        }

        if (quantity <= 0)
        {
            ModelState.AddModelError("quantity", "購入数量は1以上を指定してください");
        }

        if (string.IsNullOrWhiteSpace(address))
        {
            ModelState.AddModelError("address", "配送先住所を入力してください");
        }

        if (address?.Length > 200)
        {
            ModelState.AddModelError("address", "配送先住所は200文字以内で入力してください");
        }

        if (string.IsNullOrWhiteSpace(paymentMethod))
        {
            ModelState.AddModelError("paymentMethod", "支払方法を入力してください");
        }

        if (product == null)
        {
            ModelState.AddModelError("id", "商品が見つかりません");
        }
        else if (product.StockQuantity < quantity)
        {
            ModelState.AddModelError("quantity", "在庫数が不足しています");
        }

        // ModelStateの検証
        if (!ModelState.IsValid)
        {
            // エラーメッセージを明示的にTempDataに保存
            TempData["ErrorMessages"] = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .ToList();

            // 元のビューに戻り、エラーメッセージを表示
            return View("Purchase", product);
        }

        // ログインユーザーのID取得
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

        // 注文データの作成
        var order = new Order
        {
            UserId = userId,
            OrderDate = DateTime.Now,
            TotalAmount = product.Price * quantity + 500  // 送料込みの金額
        };

        // 注文詳細データの作成
        var orderItem = new OrderItem
        {
            Order = order,
            ProductId = id,
            Quantity = quantity,
            Price = product.Price
        };

        // 注文に商品を追加
        order.OrderItems.Add(orderItem);

        // 在庫数の更新
        product.StockQuantity -= quantity;

        // データベースに注文を保存
        _context.Order.Add(order);
        await _context.SaveChangesAsync();

        // 注文完了画面へリダイレクト
        return RedirectToAction(nameof(OrderComplete), new { id = order.Id });
    }

    // 注文完了画面表示
    public IActionResult OrderComplete(int id)
    {
        return View(id);
    }

    public IActionResult Error()
    {
        return View(new ErrorViewModel
        {
            RequestId = HttpContext.TraceIdentifier
        });
    }
}
