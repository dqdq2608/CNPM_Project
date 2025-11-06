using eShop.Catalog.API.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace eShop.Catalog.API.Infrastructure.EntityConfigurations;

public class CatalogItemEntityTypeConfiguration : IEntityTypeConfiguration<CatalogItem>
{
    public void Configure(EntityTypeBuilder<CatalogItem> builder)
    {
        builder.ToTable("CatalogItems");

        // Khóa chính
        builder.HasKey(ci => ci.Id);

        // Tên món
        builder.Property(ci => ci.Name)
            .HasMaxLength(200)
            .IsRequired();

        // Mô tả món ăn
        builder.Property(ci => ci.Description)
            .HasMaxLength(1000);

        // Giá
        builder.Property(ci => ci.Price)
            .HasColumnType("numeric(18,2)");

        // File ảnh
        builder.Property(ci => ci.PictureFileName)
            .HasMaxLength(100);

        // Trạng thái món ăn
        builder.Property(ci => ci.IsAvailable)
            .HasDefaultValue(true);

        // Embedding (pgvector)
        builder.Property(ci => ci.Embedding)
            .HasColumnType("vector(384)")
            .IsRequired(false);

        // Quan hệ với CatalogType
        builder.HasOne(ci => ci.CatalogType)
            .WithMany()
            .HasForeignKey(ci => ci.CatalogTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        // Quan hệ với Restaurant
        builder.HasOne(ci => ci.Restaurant)
            .WithMany(r => r.Items)
            .HasForeignKey(ci => ci.RestaurantId)
            .OnDelete(DeleteBehavior.Restrict);

        // Các trường tồn kho
        builder.Property(ci => ci.AvailableStock);
        builder.Property(ci => ci.RestockThreshold);
        builder.Property(ci => ci.MaxStockThreshold);
        builder.Property(ci => ci.OnReorder);

        // Index cho tìm kiếm nhanh theo tên và loại
        builder.HasIndex(ci => new { ci.Name, ci.CatalogTypeId });
    }
}
