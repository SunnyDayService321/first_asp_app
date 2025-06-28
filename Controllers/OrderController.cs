using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using first_asp_app.Data;
using first_asp_app.Models;

namespace first_asp_app.Controllers;

[Authorize]
public class OrderController : Controller
{
    private readonly MvcMovieContext _context;
    private readonly ILogger<OrderController> _logger;

    public OrderController(MvcMovieContext context, ILogger<OrderController> logger)
    {
        _context = context;
        _logger = logger;
    }

    // 注文履歴一覧表示
    public async Task<IActionResult> Index()
    {
        try
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

            // ユーザーの注文履歴を取得（新しい順）
            var orders = await _context.Order
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .Where(o => o.UserId == userId)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            _logger.LogInformation($"ユーザーID:{userId}の注文履歴を{orders.Count}件取得");

            return View(orders);
        }
        catch (Exception ex)
        {
            _logger.LogError($"注文履歴取得でエラー: {ex.Message}");
            TempData["ErrorMessage"] = "注文履歴の取得に失敗しました";
            return RedirectToAction("Index", "Product");
        }
    }

    // 注文詳細表示
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
        {
            TempData["ErrorMessage"] = "注文IDが指定されていません";
            return RedirectToAction("Index");
        }

        try
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

            // 指定された注文の詳細を取得（自分の注文のみ）
            var order = await _context.Order
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .FirstOrDefaultAsync(o => o.Id == id && o.UserId == userId);

            if (order == null)
            {
                TempData["ErrorMessage"] = "指定された注文が見つかりません";
                return RedirectToAction("Index");
            }

            _logger.LogInformation($"注文ID:{id}の詳細を表示");

            return View(order);
        }
        catch (Exception ex)
        {
            _logger.LogError($"注文詳細取得でエラー: {ex.Message}");
            TempData["ErrorMessage"] = "注文詳細の取得に失敗しました";
            return RedirectToAction("Index");
        }
    }
}
