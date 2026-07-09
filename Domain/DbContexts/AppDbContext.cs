using Microsoft.EntityFrameworkCore;
using STL.Entities.CatalogModule;
using STL.Entities.IdentityModule;
using STL.Entities.RecommendationModule;
using STL.Entities.SalesModule;

namespace STL.DbContexts
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<Category> Categories => Set<Category>();
        public DbSet<Product> Products => Set<Product>();
        public DbSet<Order> Orders => Set<Order>();
        public DbSet<OrderItem> OrderItems => Set<OrderItem>();
        public DbSet<ProductInteraction> ProductInteractions => Set<ProductInteraction>();
        public DbSet<ProductRecommendation> ProductRecommendations => Set<ProductRecommendation>();
        public DbSet<User> Users => Set<User>();
        public DbSet<UserToken> UserTokens => Set<UserToken>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Soft-delete: tu dong an ban ghi da xoa mem cho cac entity IHaveSoftDelete.
            modelBuilder.Entity<Category>()
                .HasQueryFilter(category => !category.Deleted);

            modelBuilder.Entity<Product>()
                .HasQueryFilter(product => !product.Deleted);

            modelBuilder.Entity<User>()
                .HasQueryFilter(user => !user.Deleted);

            modelBuilder.Entity<Order>()
                .HasQueryFilter(order => !order.Deleted);

            modelBuilder.Entity<OrderItem>()
                .HasQueryFilter(orderItem => !orderItem.Deleted);

            modelBuilder.Entity<ProductInteraction>()
                .HasQueryFilter(interaction => !interaction.Product!.Deleted);

            modelBuilder.Entity<ProductRecommendation>()
                .HasQueryFilter(recommendation =>
                    !recommendation.Product!.Deleted
                    && !recommendation.RecommendedProduct!.Deleted);

            modelBuilder.Entity<Category>()
                .HasIndex(category => category.Slug)
                .IsUnique();

            modelBuilder.Entity<Product>()
                .HasOne(product => product.Category)
                .WithMany(category => category.Products)
                .HasForeignKey(product => product.CategoryId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<User>()
                .HasIndex(user => user.Email)
                .IsUnique();

            modelBuilder.Entity<UserToken>()
                .HasIndex(token => token.UserId)
                .IsUnique();

            modelBuilder.Entity<UserToken>()
                .HasOne(token => token.User)
                .WithMany()
                .HasForeignKey(token => token.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Order>()
                .HasOne(order => order.User)
                .WithMany()
                .HasForeignKey(order => order.UserId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Order>()
                .HasIndex(order => order.UserId);

            modelBuilder.Entity<Order>()
                .HasIndex(order => order.OrderNumber)
                .IsUnique();

            modelBuilder.Entity<Order>()
                .Property(order => order.TotalAmount)
                .HasPrecision(18, 2);

            modelBuilder.Entity<OrderItem>()
                .HasOne(orderItem => orderItem.Order)
                .WithMany(order => order.Items)
                .HasForeignKey(orderItem => orderItem.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<OrderItem>()
                .HasOne(orderItem => orderItem.Product)
                .WithMany()
                .HasForeignKey(orderItem => orderItem.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<OrderItem>()
                .HasIndex(orderItem => new
                {
                    orderItem.OrderId,
                    orderItem.ProductId
                });

            modelBuilder.Entity<OrderItem>()
                .Property(orderItem => orderItem.UnitPrice)
                .HasPrecision(18, 2);

            modelBuilder.Entity<OrderItem>()
                .Property(orderItem => orderItem.LineTotal)
                .HasPrecision(18, 2);

            modelBuilder.Entity<ProductInteraction>()
                .HasOne(interaction => interaction.User)
                .WithMany()
                .HasForeignKey(interaction => interaction.UserId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<ProductInteraction>()
                .HasOne(interaction => interaction.Product)
                .WithMany()
                .HasForeignKey(interaction => interaction.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ProductInteraction>()
                .HasIndex(interaction => interaction.UserId);

            modelBuilder.Entity<ProductInteraction>()
                .HasIndex(interaction => interaction.SessionId);

            modelBuilder.Entity<ProductInteraction>()
                .HasIndex(interaction => new
                {
                    interaction.ProductId,
                    interaction.CreatedAt
                });

            modelBuilder.Entity<ProductInteraction>()
                .Property(interaction => interaction.Weight)
                .HasPrecision(9, 2);

            modelBuilder.Entity<ProductRecommendation>()
                .HasOne(recommendation => recommendation.Product)
                .WithMany()
                .HasForeignKey(recommendation => recommendation.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ProductRecommendation>()
                .HasOne(recommendation => recommendation.RecommendedProduct)
                .WithMany()
                .HasForeignKey(recommendation => recommendation.RecommendedProductId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ProductRecommendation>()
                .HasIndex(recommendation => new
                {
                    recommendation.ProductId,
                    recommendation.RecommendationType,
                    recommendation.ModelVersion,
                    recommendation.RecommendedProductId
                })
                .IsUnique();

            modelBuilder.Entity<ProductRecommendation>()
                .Property(recommendation => recommendation.Score)
                .HasPrecision(9, 6);
        }
    }
}
