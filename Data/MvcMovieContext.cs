using Microsoft.EntityFrameworkCore;
using first_asp_app.Models;

namespace first_asp_app.Data;

public class MvcMovieContext : DbContext
{
    public MvcMovieContext(DbContextOptions<MvcMovieContext> options)
        : base(options)
    {
    }

    public DbSet<Movie> Movie { get; set; } = null!;
    public DbSet<User> User { get; set; } = null!;
    public DbSet<Product> Product { get; set; } = null!;
    public DbSet<Order> Order { get; set; } = null!;
    public DbSet<OrderItem> OrderItem { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // OrderItemの複合キー設定
        modelBuilder.Entity<OrderItem>()
            .HasOne(oi => oi.Order)
            .WithMany(o => o.OrderItems)
            .HasForeignKey(oi => oi.OrderId);

        modelBuilder.Entity<OrderItem>()
            .HasOne(oi => oi.Product);
    }
}
