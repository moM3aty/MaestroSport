using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MaestroSport.Migrations
{
    /// <inheritdoc />
    public partial class AddCouponConditions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MinQuantity",
                table: "Coupons",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TargetCategoryId",
                table: "Coupons",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TargetFabricName",
                table: "Coupons",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MinQuantity",
                table: "Coupons");

            migrationBuilder.DropColumn(
                name: "TargetCategoryId",
                table: "Coupons");

            migrationBuilder.DropColumn(
                name: "TargetFabricName",
                table: "Coupons");
        }
    }
}
