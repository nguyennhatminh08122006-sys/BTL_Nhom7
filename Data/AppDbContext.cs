using CanifaShop.Models;
using Microsoft.EntityFrameworkCore;

namespace CanifaShop.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<Cart> Carts => Set<Cart>();
    public DbSet<CartItem> CartItems => Set<CartItem>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<User>().HasIndex(u => u.Email).IsUnique();
        b.Entity<Category>().HasIndex(c => c.Slug).IsUnique();
        b.Entity<Cart>().HasIndex(c => c.UserId).IsUnique();
        b.Entity<CartItem>().HasIndex(c => new { c.CartId, c.ProductId, c.Size }).IsUnique();

        b.Entity<Product>()
            .HasOne(p => p.Category).WithMany(c => c.Products)
            .HasForeignKey(p => p.CategoryId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<Order>()
            .HasOne(o => o.User).WithMany()
            .HasForeignKey(o => o.UserId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<OrderItem>()
            .HasOne<Product>().WithMany()
            .HasForeignKey(i => i.ProductId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<Cart>()
            .HasOne<User>().WithOne()
            .HasForeignKey<Cart>(c => c.UserId).OnDelete(DeleteBehavior.Cascade);
        b.Entity<CartItem>()
            .HasOne<Cart>().WithMany(c => c.Items)
            .HasForeignKey(i => i.CartId).OnDelete(DeleteBehavior.Cascade);
        b.Entity<CartItem>()
            .HasOne<Product>().WithMany()
            .HasForeignKey(i => i.ProductId).OnDelete(DeleteBehavior.Restrict);
    }
}
