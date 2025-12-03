using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Delivery.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDroneLatLonToDeliveryOrder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "DroneLat",
                schema: "delivery",
                table: "deliveryorders",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "DroneLon",
                schema: "delivery",
                table: "deliveryorders",
                type: "double precision",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DroneLat",
                schema: "delivery",
                table: "deliveryorders");

            migrationBuilder.DropColumn(
                name: "DroneLon",
                schema: "delivery",
                table: "deliveryorders");
        }
    }
}
