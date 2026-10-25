using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameBackend.Migrations
{
    /// <inheritdoc />
    public partial class AddWeaponAndToolStats : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "IconKey",
                table: "Items",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ToolStats",
                columns: table => new
                {
                    ItemId = table.Column<int>(type: "integer", nullable: false),
                    ToolType = table.Column<string>(type: "text", nullable: false),
                    ToolPower = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ToolStats", x => x.ItemId);
                    table.ForeignKey(
                        name: "FK_ToolStats_Items_ItemId",
                        column: x => x.ItemId,
                        principalTable: "Items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WeaponStats",
                columns: table => new
                {
                    ItemId = table.Column<int>(type: "integer", nullable: false),
                    Damage = table.Column<int>(type: "integer", nullable: false),
                    AttackSpeed = table.Column<float>(type: "real", nullable: false),
                    AttackRange = table.Column<float>(type: "real", nullable: false),
                    Durability = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WeaponStats", x => x.ItemId);
                    table.ForeignKey(
                        name: "FK_WeaponStats_Items_ItemId",
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
                name: "ToolStats");

            migrationBuilder.DropTable(
                name: "WeaponStats");

            migrationBuilder.DropColumn(
                name: "IconKey",
                table: "Items");
        }
    }
}
