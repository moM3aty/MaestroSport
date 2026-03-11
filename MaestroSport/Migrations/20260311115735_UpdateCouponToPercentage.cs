using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MaestroSport.Migrations
{
    /// <inheritdoc />
    public partial class UpdateCouponToPercentage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "DiscountAmount",
                table: "Coupons",
                newName: "DiscountPercentage");

            migrationBuilder.AddColumn<bool>(
                name: "IsFreePiece",
                table: "Coupons",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsFreePiece",
                table: "Coupons");

            migrationBuilder.RenameColumn(
                name: "DiscountPercentage",
                table: "Coupons",
                newName: "DiscountAmount");
        }
    }
}
