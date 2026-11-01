using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameBackend.Migrations
{
    /// <inheritdoc />
    public partial class AddConsumableStatsAndTradePrices : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Price",
                table: "Items");

            migrationBuilder.AddColumn<int>(
                name: "BuyPrice",
                table: "Items",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SellPrice",
                table: "Items",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ConsumableStats",
                columns: table => new
                {
                    ItemId = table.Column<int>(type: "integer", nullable: false),
                    HealHP = table.Column<int>(type: "integer", nullable: false),
                    RestoreStamina = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConsumableStats", x => x.ItemId);
                    table.ForeignKey(
                        name: "FK_ConsumableStats_Items_ItemId",
                        column: x => x.ItemId,
                        principalTable: "Items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ConsumableStats");

            migrationBuilder.DropColumn(
                name: "BuyPrice",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "SellPrice",
                table: "Items");

            migrationBuilder.AddColumn<int>(
                name: "Price",
                table: "Items",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }
    }
}
