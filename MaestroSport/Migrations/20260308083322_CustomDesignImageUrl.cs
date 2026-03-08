using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MaestroSport.Migrations
{
    /// <inheritdoc />
    public partial class CustomDesignImageUrl : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CustomDesignImageUrl",
                table: "Orders",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CustomDesignImageUrl",
                table: "Orders");
        }
    }
}
