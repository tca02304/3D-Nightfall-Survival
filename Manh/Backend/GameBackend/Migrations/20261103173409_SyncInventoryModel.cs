using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameBackend.Migrations
{
    /// <inheritdoc />
    public partial class SyncInventoryModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PlayerInventorySlots_Items_itemId",
                table: "PlayerInventorySlots");

            migrationBuilder.DropForeignKey(
                name: "FK_PlayerInventorySlots_Players_playerId",
                table: "PlayerInventorySlots");

            migrationBuilder.RenameColumn(
                name: "playerId",
                table: "PlayerInventorySlots",
                newName: "PlayerId");

            migrationBuilder.RenameColumn(
                name: "itemId",
                table: "PlayerInventorySlots",
                newName: "ItemId");

            migrationBuilder.RenameIndex(
                name: "IX_PlayerInventorySlots_playerId_SlotIndex",
                table: "PlayerInventorySlots",
                newName: "IX_PlayerInventorySlots_PlayerId_SlotIndex");

            migrationBuilder.RenameIndex(
                name: "IX_PlayerInventorySlots_itemId",
                table: "PlayerInventorySlots",
                newName: "IX_PlayerInventorySlots_ItemId");

            migrationBuilder.AddForeignKey(
                name: "FK_PlayerInventorySlots_Items_ItemId",
                table: "PlayerInventorySlots",
                column: "ItemId",
                principalTable: "Items",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PlayerInventorySlots_Players_PlayerId",
                table: "PlayerInventorySlots",
                column: "PlayerId",
                principalTable: "Players",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PlayerInventorySlots_Items_ItemId",
                table: "PlayerInventorySlots");

            migrationBuilder.DropForeignKey(
                name: "FK_PlayerInventorySlots_Players_PlayerId",
                table: "PlayerInventorySlots");

            migrationBuilder.RenameColumn(
                name: "PlayerId",
                table: "PlayerInventorySlots",
                newName: "playerId");

            migrationBuilder.RenameColumn(
                name: "ItemId",
                table: "PlayerInventorySlots",
                newName: "itemId");

            migrationBuilder.RenameIndex(
                name: "IX_PlayerInventorySlots_PlayerId_SlotIndex",
                table: "PlayerInventorySlots",
                newName: "IX_PlayerInventorySlots_playerId_SlotIndex");

            migrationBuilder.RenameIndex(
                name: "IX_PlayerInventorySlots_ItemId",
                table: "PlayerInventorySlots",
                newName: "IX_PlayerInventorySlots_itemId");

            migrationBuilder.AddForeignKey(
                name: "FK_PlayerInventorySlots_Items_itemId",
                table: "PlayerInventorySlots",
                column: "itemId",
                principalTable: "Items",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PlayerInventorySlots_Players_playerId",
                table: "PlayerInventorySlots",
                column: "playerId",
                principalTable: "Players",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
