using EAV_Draft.Models;
using Microsoft.EntityFrameworkCore;

namespace EAV_Draft
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Product> Products { get; set; }
        public DbSet<Models.Attribute> Attributes { get; set; }
        public DbSet<AttributeValue> AttributeValues { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Product>(entity =>
            {
                entity.HasKey(p => p.ProductId);
                entity.Property(p => p.ProductName).IsRequired().HasMaxLength(100);
                entity.Property(p => p.Price).HasColumnType("decimal(10,2)");
                entity.Property(p => p.CreatedAt).HasDefaultValueSql("GETDATE()");
            });

            modelBuilder.Entity<Models.Attribute>(entity =>
            {
                entity.HasKey(a => a.AttributeId);
                entity.Property(a => a.Name).IsRequired().HasMaxLength(100);
                entity.Property(a => a.DataType).IsRequired().HasMaxLength(50);
            });

            modelBuilder.Entity<AttributeValue>(entity =>
            {
                entity.HasKey(av => av.ValueId);

                entity.HasOne<Product>()
                      .WithMany()
                      .HasForeignKey(av => av.ProductId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne<Models.Attribute>()
                      .WithMany()
                      .HasForeignKey(av => av.AttributeId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.Property(av => av.Value).IsRequired();
            });
        }
    }
}
