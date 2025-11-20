using System;
using eShop.Ordering.Domain.AggregatesModel.BuyerAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace eShop.Ordering.Infrastructure.EntityConfigurations;

class PaymentMethodEntityTypeConfiguration
    : IEntityTypeConfiguration<PaymentMethod>
{
    public void Configure(EntityTypeBuilder<PaymentMethod> paymentConfiguration)
    {
        paymentConfiguration.ToTable("paymentmethods");

        paymentConfiguration.Ignore(b => b.DomainEvents);

        paymentConfiguration.Property(b => b.Id)
            .UseHiLo("paymentseq");

        paymentConfiguration.Property<int>("BuyerId");

        paymentConfiguration
            .Property<string>("_cardHolderName")
            .HasColumnName("CardHolderName")
            .HasMaxLength(200);

        paymentConfiguration
            .Property<string>("_alias")
            .HasColumnName("Alias")
            .HasMaxLength(200);

        paymentConfiguration
            .Property<string>("_cardNumber")
            .HasColumnName("CardNumber")
            .HasMaxLength(25)
            .IsRequired();

        // 🔧 QUAN TRỌNG: _expiration là DateTime, map sang timestamp without time zone
        paymentConfiguration
            .Property<DateTime>("_expiration")
            .HasColumnName("Expiration")
            .HasColumnType("timestamp without time zone");

        paymentConfiguration
            .Property<int>("_cardTypeId")
            .HasColumnName("CardTypeId");

        paymentConfiguration.HasOne(p => p.CardType)
            .WithMany()
            .HasForeignKey("_cardTypeId");
    }
}
