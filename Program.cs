using Microsoft.EntityFrameworkCore;
using first_asp_app.Data;
using first_asp_app.Models;
using Microsoft.AspNetCore.Authentication.Cookies;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// ロギングの設定
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// DBContextの登録
builder.Services.AddDbContext<MvcMovieContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// 認証の設定を追加
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
    });

var app = builder.Build();

// データベースの初期化とシードデータ追加
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var context = services.GetRequiredService<MvcMovieContext>();

    // データベースを作成
    context.Database.EnsureCreated();

    // テストデータがない場合は追加
    if (!context.Product.Any())
    {
        context.Product.AddRange(
            new Product
            {
                Name = "テスト商品1",
                Description = "これはテスト商品1です",
                Price = 1000,
                StockQuantity = 10
            },
               new Product
            {
                Name = "テスト商品2",
                Description = "これはテスト商品2です",
                Price = 2000,
                StockQuantity = 20
            }
        );
        context.SaveChanges();
    }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// 認証・認可の順序は重要
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
