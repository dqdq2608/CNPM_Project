using eShop.Ordering.Domain.AggregatesModel.OrderAggregate;
using eShop.Ordering.Domain.AggregatesModel.BuyerAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace eShop.Ordering.Infrastructure.EntityConfigurations;

class OrderEntityTypeConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> orderConfiguration)
    {
        orderConfiguration.ToTable("orders");

        orderConfiguration.Ignore(b => b.DomainEvents);

        orderConfiguration.Property(o => o.Id)
            .UseHiLo("orderseq");

        // Address value object persisted as owned entity type
        orderConfiguration
            .OwnsOne(o => o.Address);

        orderConfiguration
            .Property(o => o.OrderStatus)
            .HasConversion<string>()
            .HasMaxLength(30);

        // 🔧 QUAN TRỌNG: OrderDate là DateTime, map sang timestamp without time zone cho PostgreSQL
        orderConfiguration
            .Property(o => o.OrderDate)
            .HasColumnType("timestamp without time zone");

        orderConfiguration
            .Property(o => o.PaymentId)
            .HasColumnName("PaymentMethodId");

        orderConfiguration
            .Property(o => o.DeliveryFee)
            .HasColumnType("decimal(18,2)")
            .HasDefaultValue(0m);

        orderConfiguration.HasOne<PaymentMethod>()
            .WithMany()
            .HasForeignKey(o => o.PaymentId)
            .OnDelete(DeleteBehavior.Restrict);

        orderConfiguration.HasOne(o => o.Buyer)
            .WithMany()
            .HasForeignKey(o => o.BuyerId);
    }
}
