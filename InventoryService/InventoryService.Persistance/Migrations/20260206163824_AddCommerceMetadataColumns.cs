using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InventoryService.Persistance.Migrations
{
    /// <inheritdoc />
    public partial class AddCommerceMetadataColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                IF COL_LENGTH('Products','AttributesJson') IS NULL
                    ALTER TABLE [Products] ADD [AttributesJson] nvarchar(max) NOT NULL DEFAULT N'{}';
                IF COL_LENGTH('Products','Brand') IS NULL
                    ALTER TABLE [Products] ADD [Brand] nvarchar(max) NOT NULL DEFAULT N'';
                IF COL_LENGTH('Products','Category') IS NULL
                    ALTER TABLE [Products] ADD [Category] nvarchar(max) NOT NULL DEFAULT N'';
                IF COL_LENGTH('Products','CurrencyCode') IS NULL
                    ALTER TABLE [Products] ADD [CurrencyCode] nvarchar(max) NOT NULL DEFAULT N'USD';
                IF COL_LENGTH('Products','Description') IS NULL
                    ALTER TABLE [Products] ADD [Description] nvarchar(max) NOT NULL DEFAULT N'';
                IF COL_LENGTH('Products','IsActive') IS NULL
                    ALTER TABLE [Products] ADD [IsActive] bit NOT NULL DEFAULT CAST(1 as bit);
                IF COL_LENGTH('Products','Sku') IS NULL
                    ALTER TABLE [Products] ADD [Sku] nvarchar(max) NOT NULL DEFAULT N'';
                IF COL_LENGTH('Products','Subcategory') IS NULL
                    ALTER TABLE [Products] ADD [Subcategory] nvarchar(max) NOT NULL DEFAULT N'';
                IF COL_LENGTH('Products','Tags') IS NULL
                    ALTER TABLE [Products] ADD [Tags] nvarchar(max) NOT NULL DEFAULT N'';
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AttributesJson",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "Brand",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "Category",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "CurrencyCode",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "Sku",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "Subcategory",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "Tags",
                table: "Products");
        }
    }
}
