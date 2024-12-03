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
        var products = await _context.Product.ToListAsync();
        return View(products);
    }

    // 商品詳細表示
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var product = await _context.Product.FirstOrDefaultAsync(m => m.Id == id);
        if (product == null)
        {
            return NotFound();
        }

        return View(product);
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
   public async Task<IActionResult> Purchase(int id)
   {
       var product = await _context.Product.FindAsync(id);
       if (product == null)
       {
           return NotFound();
       }
       return View(product);
   }
   // 購入確定処理（配送先・支払方法込み）
   [HttpPost]
   public async Task<IActionResult> ConfirmPurchase(int id, int quantity, string address, string paymentMethod)
   {
       // 商品の存在と在庫確認
       var product = await _context.Product.FindAsync(id);
       if (product == null || product.StockQuantity < quantity)
       {
           return BadRequest("商品が存在しないか、在庫が不足しています。");
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
}
