using Delivery.Domain;
using Delivery.Domain.Drone;
using Microsoft.EntityFrameworkCore;

namespace Delivery.Infrastructure;

public class DeliveryDbContext : DbContext
{
    public DeliveryDbContext(DbContextOptions<DeliveryDbContext> options)
        : base(options)
    {
    }

    public DbSet<DeliveryOrder> DeliveryOrders => Set<DeliveryOrder>();
    public DbSet<Drone> Drones => Set<Drone>();
    public DbSet<DroneAssignment> DroneAssignments => Set<DroneAssignment>();
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("delivery");

        modelBuilder.Entity<DeliveryOrder>(b =>
        {
            b.ToTable("deliveryorders");
            b.HasKey(x => x.Id);

            b.Property(x => x.Status)
             .HasConversion<int>();
        });

        modelBuilder.Entity<Drone>(builder =>
        {
            builder.ToTable("drones", "delivery");

            builder.HasKey(d => d.Id);

            builder.Property(d => d.RestaurantId)
                .IsRequired();

            builder.Property(d => d.Code)
                .IsRequired()
                .HasMaxLength(50);

            builder.Property(d => d.Status)
                .HasConversion<int>();

            builder.Property(d => d.CurrentLatitude);
            builder.Property(d => d.CurrentLongitude);

            builder.Property(d => d.LastHeartbeatAt)
                .IsRequired();

            builder.HasIndex(d => new { d.RestaurantId, d.Code })
                .IsUnique();
        });

        modelBuilder.Entity<DroneAssignment>(builder =>
        {
            builder.ToTable("drone_assignments", "delivery");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.DroneId)
                .IsRequired();

            builder.Property(x => x.DeliveryOrderId)
                .IsRequired();

            builder.Property(x => x.Status)
                .HasConversion<int>()  // enum -> int
                .IsRequired();

            builder.Property(x => x.AssignedAt)
                .IsRequired();

            builder.Property(x => x.StartedAt);
            builder.Property(x => x.CompletedAt);

            builder.Property(x => x.FailureReason)
                .HasMaxLength(500);
        });
    }
}
