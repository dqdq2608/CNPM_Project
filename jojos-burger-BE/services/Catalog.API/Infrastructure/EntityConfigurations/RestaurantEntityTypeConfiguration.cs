using eShop.Catalog.API.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NetTopologySuite.Geometries;

namespace eShop.Catalog.API.Infrastructure.EntityConfigurations;

public class RestaurantEntityTypeConfiguration : IEntityTypeConfiguration<Restaurant>
{
    public void Configure(EntityTypeBuilder<Restaurant> builder)
    {
        builder.ToTable("Restaurants");

        // PK là Guid
        builder.HasKey(r => r.RestaurantId);

        builder.Property(r => r.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(r => r.Address)
            .HasMaxLength(300);

        // PostGIS: geometry(Point,4326)
        builder.Property(r => r.Location)
            .HasColumnType("geometry(Point,4326)")
            .IsRequired(false);

        // 1-n: Restaurant -> CatalogItems
        builder.HasMany(r => r.Items)
               .WithOne(i => i.Restaurant)
               .HasForeignKey(i => i.RestaurantId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.Property(r => r.Status)
          .HasConversion<int>()         // lưu enum dưới dạng int
          .HasDefaultValue(RestaurantStatus.Active);

        // Index phục vụ tìm kiếm
        builder.HasIndex(r => r.Name);

        // Giả sử bạn muốn query theo vị trí -> GIST index cho Location
        builder.HasIndex(r => r.Location).HasMethod("GIST");
    }
}
