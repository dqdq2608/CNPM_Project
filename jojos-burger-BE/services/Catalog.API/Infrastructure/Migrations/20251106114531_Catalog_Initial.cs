using System;
using Microsoft.EntityFrameworkCore.Migrations;
using NetTopologySuite.Geometries;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace eShop.Catalog.API.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Catalog_Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Catalog_CatalogBrand_CatalogBrandId",
                table: "Catalog");

            migrationBuilder.DropForeignKey(
                name: "FK_Catalog_CatalogType_CatalogTypeId",
                table: "Catalog");

            migrationBuilder.DropTable(
                name: "CatalogBrand");

            migrationBuilder.DropPrimaryKey(
                name: "PK_CatalogType",
                table: "CatalogType");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Catalog",
                table: "Catalog");

            migrationBuilder.DropIndex(
                name: "IX_Catalog_CatalogBrandId",
                table: "Catalog");

            migrationBuilder.DropIndex(
                name: "IX_Catalog_Name",
                table: "Catalog");

            migrationBuilder.DropColumn(
                name: "CatalogBrandId",
                table: "Catalog");

            migrationBuilder.RenameTable(
                name: "CatalogType",
                newName: "CatalogTypes");

            migrationBuilder.RenameTable(
                name: "Catalog",
                newName: "CatalogItems");

            migrationBuilder.RenameIndex(
                name: "IX_Catalog_CatalogTypeId",
                table: "CatalogItems",
                newName: "IX_CatalogItems_CatalogTypeId");

            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:postgis", ",,")
                .Annotation("Npgsql:PostgresExtension:vector", ",,")
                .OldAnnotation("Npgsql:PostgresExtension:vector", ",,");

            migrationBuilder.AlterColumn<decimal>(
                name: "Price",
                table: "CatalogItems",
                type: "numeric(18,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AlterColumn<string>(
                name: "PictureFileName",
                table: "CatalogItems",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "CatalogItems",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "CatalogItems",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "EstimatedPrepTime",
                table: "CatalogItems",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsAvailable",
                table: "CatalogItems",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<Guid>(
                name: "RestaurantId",
                table: "CatalogItems",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddPrimaryKey(
                name: "PK_CatalogTypes",
                table: "CatalogTypes",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_CatalogItems",
                table: "CatalogItems",
                column: "Id");

            migrationBuilder.CreateTable(
                name: "Restaurants",
                columns: table => new
                {
                    RestaurantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Address = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    Location = table.Column<Point>(type: "geometry(Point,4326)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Restaurants", x => x.RestaurantId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CatalogTypes_Type",
                table: "CatalogTypes",
                column: "Type",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CatalogItems_Name_CatalogTypeId",
                table: "CatalogItems",
                columns: new[] { "Name", "CatalogTypeId" });

            migrationBuilder.CreateIndex(
                name: "IX_CatalogItems_RestaurantId",
                table: "CatalogItems",
                column: "RestaurantId");

            migrationBuilder.CreateIndex(
                name: "IX_Restaurants_Location",
                table: "Restaurants",
                column: "Location")
                .Annotation("Npgsql:IndexMethod", "GIST");

            migrationBuilder.CreateIndex(
                name: "IX_Restaurants_Name",
                table: "Restaurants",
                column: "Name");

            migrationBuilder.AddForeignKey(
                name: "FK_CatalogItems_CatalogTypes_CatalogTypeId",
                table: "CatalogItems",
                column: "CatalogTypeId",
                principalTable: "CatalogTypes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CatalogItems_Restaurants_RestaurantId",
                table: "CatalogItems",
                column: "RestaurantId",
                principalTable: "Restaurants",
                principalColumn: "RestaurantId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CatalogItems_CatalogTypes_CatalogTypeId",
                table: "CatalogItems");

            migrationBuilder.DropForeignKey(
                name: "FK_CatalogItems_Restaurants_RestaurantId",
                table: "CatalogItems");

            migrationBuilder.DropTable(
                name: "Restaurants");

            migrationBuilder.DropPrimaryKey(
                name: "PK_CatalogTypes",
                table: "CatalogTypes");

            migrationBuilder.DropIndex(
                name: "IX_CatalogTypes_Type",
                table: "CatalogTypes");

            migrationBuilder.DropPrimaryKey(
                name: "PK_CatalogItems",
                table: "CatalogItems");

            migrationBuilder.DropIndex(
                name: "IX_CatalogItems_Name_CatalogTypeId",
                table: "CatalogItems");

            migrationBuilder.DropIndex(
                name: "IX_CatalogItems_RestaurantId",
                table: "CatalogItems");

            migrationBuilder.DropColumn(
                name: "EstimatedPrepTime",
                table: "CatalogItems");

            migrationBuilder.DropColumn(
                name: "IsAvailable",
                table: "CatalogItems");

            migrationBuilder.DropColumn(
                name: "RestaurantId",
                table: "CatalogItems");

            migrationBuilder.RenameTable(
                name: "CatalogTypes",
                newName: "CatalogType");

            migrationBuilder.RenameTable(
                name: "CatalogItems",
                newName: "Catalog");

            migrationBuilder.RenameIndex(
                name: "IX_CatalogItems_CatalogTypeId",
                table: "Catalog",
                newName: "IX_Catalog_CatalogTypeId");

            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:vector", ",,")
                .OldAnnotation("Npgsql:PostgresExtension:postgis", ",,")
                .OldAnnotation("Npgsql:PostgresExtension:vector", ",,");

            migrationBuilder.AlterColumn<decimal>(
                name: "Price",
                table: "Catalog",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)");

            migrationBuilder.AlterColumn<string>(
                name: "PictureFileName",
                table: "Catalog",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Catalog",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "Catalog",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(1000)",
                oldMaxLength: 1000,
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CatalogBrandId",
                table: "Catalog",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddPrimaryKey(
                name: "PK_CatalogType",
                table: "CatalogType",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Catalog",
                table: "Catalog",
                column: "Id");

            migrationBuilder.CreateTable(
                name: "CatalogBrand",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Brand = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CatalogBrand", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Catalog_CatalogBrandId",
                table: "Catalog",
                column: "CatalogBrandId");

            migrationBuilder.CreateIndex(
                name: "IX_Catalog_Name",
                table: "Catalog",
                column: "Name");

            migrationBuilder.AddForeignKey(
                name: "FK_Catalog_CatalogBrand_CatalogBrandId",
                table: "Catalog",
                column: "CatalogBrandId",
                principalTable: "CatalogBrand",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Catalog_CatalogType_CatalogTypeId",
                table: "Catalog",
                column: "CatalogTypeId",
                principalTable: "CatalogType",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
