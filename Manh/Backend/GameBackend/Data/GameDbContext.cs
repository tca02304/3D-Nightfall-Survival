using GameBackend.Models;
using Microsoft.EntityFrameworkCore;

namespace GameBackend.Data
{
    public class GameDbContext : DbContext
    {
        public GameDbContext(DbContextOptions<GameDbContext> options)
            : base(options)
        {
        }

        public DbSet<Items> Items { get; set; }
        public DbSet<WeaponStat> WeaponStats { get; set; }
        public DbSet<ToolStat> ToolStats { get; set; }
        public DbSet<ConsumableStat> ConsumableStats { get; set; }
        public DbSet<Player> Players { get; set; }
        public DbSet<PlayerInventorySlot> PlayerInventorySlots { get; set; }
        public DbSet<PlayerCraftingSlot> PlayerCraftingSlots { get; set; }
        public DbSet<PlayerCursorSlot> PlayerCursorSlots { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Player>(entity =>
            {
                entity.HasKey(p => p.Id);

                entity.Property(p => p.Username)
                .IsRequired()
                .HasMaxLength(50);

                entity.HasIndex(p => p.Username)
                .IsUnique();
            });

            modelBuilder.Entity<PlayerInventorySlot>(entity =>
            {
                entity.HasKey(s => s.Id);

                entity.HasIndex(s => new
                {
                    s.PlayerId,
                    s.SlotIndex
                }).IsUnique();

                entity.HasOne(s => s.player)
                .WithMany(p => p.InventorySlots)
                .HasForeignKey(s => s.PlayerId)
                .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(s => s.Item)
                .WithMany()
                .HasForeignKey(s => s.ItemId)
                .OnDelete(DeleteBehavior.Restrict);

                entity.ToTable(t =>
                t.HasCheckConstraint(
                    "CK_PlayerInventorySlots_Amount",
                    "\"Amount\" > 0"
                    ));

                entity.ToTable(t =>
                t.HasCheckConstraint(
                    "CK_PlayerInventorySlots_SlotIndex",
                    "\"SlotIndex\" >= 0"
                    ));
            });


            modelBuilder.Entity<PlayerCraftingSlot>(entity =>
            {
                entity.HasKey(s => s.Id);

                entity.HasIndex(s => new
                {
                    s.PlayerId,
                    s.SlotIndex
                }).IsUnique();

                entity.HasOne(s => s.Player)
                    .WithMany()
                    .HasForeignKey(s => s.PlayerId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(s => s.Item)
                    .WithMany()
                    .HasForeignKey(s => s.ItemId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.ToTable(t =>
                {
                    t.HasCheckConstraint(
                        "CK_PlayerCraftingSlots_Amount",
                        "\"Amount\" > 0");

                    t.HasCheckConstraint(
                        "CK_PlayerCraftingSlots_SlotIndex",
                        "\"SlotIndex\" >= 0 AND \"SlotIndex\" < 9");
                });
            });

            modelBuilder.Entity<PlayerCursorSlot>(entity =>
            {
                entity.HasKey(s => s.PlayerId);

                entity.HasOne(s => s.Player)
                    .WithOne()
                    .HasForeignKey<PlayerCursorSlot>(s => s.PlayerId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(s => s.Item)
                    .WithMany()
                    .HasForeignKey(s => s.ItemId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.ToTable(t =>
                {
                    t.HasCheckConstraint(
                        "CK_PlayerCursorSlots_Amount",
                        "\"Amount\" > 0");
                });
            });

        }
    }
}
