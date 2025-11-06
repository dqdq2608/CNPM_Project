using eShop.Catalog.API.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace eShop.Catalog.API.Infrastructure.EntityConfigurations;

public class CatalogTypeEntityTypeConfiguration : IEntityTypeConfiguration<CatalogType>
{
    public void Configure(EntityTypeBuilder<CatalogType> builder)
    {
        builder.ToTable("CatalogTypes");

        // Khóa chính
        builder.HasKey(ct => ct.Id);

        // Tên loại món ăn (ví dụ: Burger, Drink, Combo, Side Dish)
        builder.Property(ct => ct.Type)
            .HasMaxLength(100)
            .IsRequired();

        // Không cho phép trùng tên loại
        builder.HasIndex(ct => ct.Type)
            .IsUnique();
    }
}
