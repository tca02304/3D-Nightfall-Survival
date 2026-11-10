using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace GameBackend.Migrations
{
    /// <inheritdoc />
    public partial class AddCraftingAndCursorPersistence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PlayerCraftingSlots",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PlayerId = table.Column<int>(type: "integer", nullable: false),
                    SlotIndex = table.Column<int>(type: "integer", nullable: false),
                    ItemId = table.Column<int>(type: "integer", nullable: false),
                    Amount = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayerCraftingSlots", x => x.Id);
                    table.CheckConstraint("CK_PlayerCraftingSlots_Amount", "\"Amount\" > 0");
                    table.CheckConstraint("CK_PlayerCraftingSlots_SlotIndex", "\"SlotIndex\" >= 0 AND \"SlotIndex\" < 9");
                    table.ForeignKey(
                        name: "FK_PlayerCraftingSlots_Items_ItemId",
                        column: x => x.ItemId,
                        principalTable: "Items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PlayerCraftingSlots_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PlayerCursorSlots",
                columns: table => new
                {
                    PlayerId = table.Column<int>(type: "integer", nullable: false),
                    ItemId = table.Column<int>(type: "integer", nullable: false),
                    Amount = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayerCursorSlots", x => x.PlayerId);
                    table.CheckConstraint("CK_PlayerCursorSlots_Amount", "\"Amount\" > 0");
                    table.ForeignKey(
                        name: "FK_PlayerCursorSlots_Items_ItemId",
                        column: x => x.ItemId,
                        principalTable: "Items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PlayerCursorSlots_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PlayerCraftingSlots_ItemId",
                table: "PlayerCraftingSlots",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_PlayerCraftingSlots_PlayerId_SlotIndex",
                table: "PlayerCraftingSlots",
                columns: new[] { "PlayerId", "SlotIndex" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PlayerCursorSlots_ItemId",
                table: "PlayerCursorSlots",
                column: "ItemId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PlayerCraftingSlots");

            migrationBuilder.DropTable(
                name: "PlayerCursorSlots");
        }
    }
}
