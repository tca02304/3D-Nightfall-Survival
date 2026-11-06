using GameBackend.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GameBackend.Data
{
    public static class ItemDataSeeder
    {
        private static readonly List<Items> ItemData = new()
        {
            //1. WOOD

            new Items
            {
                ItemCode = "wood",
                Name = "Wood",
                Description = "Basic wood used for crafting and building.",
                Type = "Material",
                MaxStack = 64,
                BuyPrice = null,
                SellPrice = 2,
                IsActive = true,
                IconKey = "wood"
            },

            new Items
            {
                ItemCode = "wood_birch",
                Name = "Birch",
                Description = "White wood used for crafting and building.",
                Type = "Material",
                MaxStack = 64,
                BuyPrice = null,
                SellPrice = 3,
                IsActive = true,
                IconKey = "wood_birch"
            },

            new Items
            {
                ItemCode = "wood_fir",
                Name = "Fir",
                Description = "Dark brown wood used for crafting and building.",
                Type = "Material",
                MaxStack = 64,
                BuyPrice = null,
                SellPrice = 4,
                IsActive = true,
                IconKey = "wood_fir"
            },

            new Items
            {
                ItemCode = "wood_oak",
                Name = "Oak",
                Description = "Golden wood used for crafting and building.",
                Type = "Material",
                MaxStack = 64,
                BuyPrice = null,
                SellPrice = 5,
                IsActive = true,
                IconKey = "wood_oak"
            },

            new Items
            {
                ItemCode = "wood_dark_oak",
                Name = "Dark Oak",
                Description = "Rare dark wood used for advanced crafting.",
                Type = "Material",
                MaxStack = 64,
                BuyPrice = null,
                SellPrice = 8,
                IsActive = true,
                IconKey = "wood"
            },

            //2. ORE

            new Items
            {
                ItemCode = "stone",
                Name = "Stone",
                Description = "Common stone used for crafting and construction.",
                Type = "Material",
                MaxStack = 64,
                BuyPrice = null,
                SellPrice = 1,
                IsActive = true,
                IconKey = "stone"
            },

            new Items
            {
                ItemCode = "coal",
                Name = "Coal",
                Description = "Fuel used for smelting ores in a furnace.",
                Type = "Material",
                MaxStack = 64,
                BuyPrice = null,
                SellPrice = 2,
                IsActive = true,
                IconKey = "coal"
            },

            new Items
            {
                ItemCode = "iron_ore",
                Name = "Iron Ore",
                Description = "Raw iron one that can be smelted into Iron Bar.",
                Type = "Material",
                MaxStack = 64,
                BuyPrice = null,
                SellPrice = 3,
                IsActive = true,
                IconKey = "iron_ore"
            },

            new Items
            {
                ItemCode = "gold_ore",
                Name = "Gold Ore",
                Description = "Raw gold one that can be smelted into Gold Bar.",
                Type = "Material",
                MaxStack = 64,
                BuyPrice = null,
                SellPrice = 6,
                IsActive = true,
                IconKey = "gold_ore"
            },

            new Items
            {
                ItemCode = "mithril_ore",
                Name = "Mithril Ore",
                Description = "Rare cyan ore used for advanced equipment.",
                Type = "Material",
                MaxStack = 64,
                BuyPrice = null,
                SellPrice = 9,
                IsActive = true,
                IconKey = "mithril_ore"
            },

            new Items
            {
                ItemCode = "adamantite_ore",
                Name = "Adamantite Ore",
                Description = "Rare green ore used for powerful equipment.",
                Type = "Material",
                MaxStack = 64,
                BuyPrice = null,
                SellPrice = 12,
                IsActive = true,
                IconKey = "adamantite_ore"
            },

            new Items
            {
                ItemCode = "obanium_ore",
                Name = "Obanium Ore",
                Description = "Extremely rare purple ore used for legendary equipment.",
                Type = "Material",
                MaxStack = 64,
                BuyPrice = null,
                SellPrice = 15,
                IsActive = true,
                IconKey = "obanium_ore"
            },

            new Items
            {
                ItemCode = "chunkium_ore",
                Name = "Chunkium Ore",
                Description = "Rare pink ore dropped by the Big Chunk boss.",
                Type = "Material",
                MaxStack = 64,
                BuyPrice = null,
                SellPrice = 15,
                IsActive = true,
                IconKey = "chunkium_ore"
            },

            //3. BARS

            new Items
            {
                ItemCode = "iron_bar",
                Name = "Iron Bar",
                Description = "Smelted iron used for crafting equipment.",
                Type = "Material",
                MaxStack = 64,
                BuyPrice = null,
                SellPrice = 5,
                IsActive = true,
                IconKey = "iron_bar"
            },

            new Items
            {
                ItemCode = "gold_bar",
                Name = "Gold Bar",
                Description = "Smelted gold used for advanced crafting.",
                Type = "Material",
                MaxStack = 64,
                BuyPrice = null,
                SellPrice = 9,
                IsActive = true,
                IconKey = "gold_bar"
            },

            new Items
            {
                ItemCode = "mithril_bar",
                Name = "Mithril Bar",
                Description = "Refined mithril used for high-tier equipment.",
                Type = "Material",
                MaxStack = 64,
                BuyPrice = null,
                SellPrice = 12,
                IsActive = true,
                IconKey = "mithril_bar"
            },

            new Items
            {
                ItemCode = "adamantite_bar",
                Name = "Adamantite Bar",
                Description = "Refined adamantite used for powerful equipment.",
                Type = "Material",
                MaxStack = 64,
                BuyPrice = null,
                SellPrice = 15,
                IsActive = true,
                IconKey = "adamantite_bar"
            },

            new Items
            {
                ItemCode = "obanium_bar",
                Name = "Obanium Bar",
                Description = "Refined obanium used for legendary equipment.",
                Type = "Material",
                MaxStack = 64,
                BuyPrice = null,
                SellPrice = 15,
                IsActive = true,
                IconKey = "obanium_bar"
            },

            new Items
            {
                ItemCode = "chunkium_bar",
                Name = "Chunkium Bar",
                Description = "Refined chunkium used for special equipment.",
                Type = "Material",
                MaxStack = 64,
                BuyPrice = null,
                SellPrice = 15,
                IsActive = true,
                IconKey = "chunkium_bar"
            },

            //4. MONSTER MATERIALS

            new Items
            {
                ItemCode = "bone",
                Name = "Bone",
                Description = "A bone dropped by Goblin, used for early crafting.",
                Type = "Material",
                MaxStack = 64,
                BuyPrice = null,
                SellPrice = 2,
                IsActive = true,
                IconKey = "bone"
            },

            new Items
            {
                ItemCode = "wolf_claw",
                Name = "Wolf Claw",
                Description = "A sharp claw dropped by Wolves.",
                Type = "Material",
                MaxStack = 64,
                BuyPrice = null,
                SellPrice = 6,
                IsActive = true,
                IconKey = "wolf_claw"
            },

            new Items
            {
                ItemCode = "wyvern_claw",
                Name = "Wyvern Claw",
                Description = "A powerful claw dropped by Wyverns.",
                Type = "Material",
                MaxStack = 64,
                BuyPrice = null,
                SellPrice = 12,
                IsActive = true,
                IconKey = "wyvern_claw"
            },

            //5. PLANS & MUSHROOMS

            new Items
            {
                ItemCode = "apple",
                Name = "Apple",
                Description = "A fresh red apple that restores health.",
                Type = "Consumble",
                MaxStack = 64,
                BuyPrice = null,
                SellPrice = 3,
                IsActive = true,
                IconKey = "apple"
            },

            new Items
            {
                ItemCode = "wheat",
                Name = "Wheat",
                Description = "A basic crop used for food and cooking.",
                Type = "Consumble",
                MaxStack = 64,
                BuyPrice = null,
                SellPrice = 2,
                IsActive = true,
                IconKey = "wheat"
            },

            new Items
            {
                ItemCode = "ligon_mushroom",
                Name = "Ligon Mushroom",
                Description = "A yellow mushroom that restores health and stamina.",
                Type = "Consumble",
                MaxStack = 64,
                BuyPrice = null,
                SellPrice = 3,
                IsActive = true,
                IconKey = "ligon_mushroom"
            },

            new Items
            {
                ItemCode = "sugon_mushroom",
                Name = "Sugon Mushroom",
                Description = "A red mushroom that restores health and stamina.",
                Type = "Consumble",
                MaxStack = 64,
                BuyPrice = null,
                SellPrice = 4,
                IsActive = true,
                IconKey = "sugon_mushroom"
            },

            new Items
            {
                ItemCode = "gulpon_mushroom",
                Name = "Gulpon Mushroom",
                Description = "A orange mushroom that restores health and stamina.",
                Type = "Consumble",
                MaxStack = 64,
                BuyPrice = null,    
                SellPrice = 5,
                IsActive = true,
                IconKey = "gulpon_mushroom"
            },

            new Items
            {
                ItemCode = "slurp_mushroom",
                Name = "Slurp Mushroom",
                Description = "A cyan mushroom that restores stamina.",
                Type = "Consumble",
                MaxStack = 64,
                BuyPrice = null,
                SellPrice = 6,
                IsActive = true,
                IconKey = "slurp_mushroom"
            },

            //6. COOKED FOOD

            new Items
            {
                ItemCode = "raw_meat",
                Name = "Raw Meat",
                Description = "Uncooked meat that can be grilled into Cooked Meat.",
                Type = "Consumble",
                MaxStack = 64,
                BuyPrice = null,
                SellPrice = 3,
                IsActive = true,
                IconKey = "raw_meat"
            },

            new Items
            {
                ItemCode = "meat_soup",
                Name = "Meat Soup",
                Description = "A nutritious soup cooked from meat.",
                Type = "Consumble",
                MaxStack = 64,
                BuyPrice = null,
                SellPrice = 10,
                IsActive = true,
                IconKey = "meat_soup"
            },

            new Items
            {
                ItemCode = "cooked_meat",
                Name = "Cooked Meat",
                Description = "Grilled meat that restores a large amount of health.",
                Type = "Consumble",
                MaxStack = 64,
                BuyPrice = null,
                SellPrice = 12,
                IsActive = true,
                IconKey = "cooked_meat"
            },

            // 7. NEW CRAFTING MATERIALS

            new Items
            {
                ItemCode = "bark",
                Name = "Bark",
                Description = "Tree bark used for basic crafting and recipes.",
                Type = "Material",
                MaxStack = 64,
                BuyPrice = null,
                SellPrice = 2,
                IsActive = true,
                IconKey = "bark"
            },

            new Items
            {
                ItemCode = "blade",
                Name = "Blade",
                Description = "A blade component used in weapon crafting.",
                Type = "Material",
                MaxStack = 64,
                BuyPrice = null,
                SellPrice = null,
                IsActive = true,
                IconKey = "blade"
            },

            new Items
            {
                ItemCode = "rope",
                Name = "Rope",
                Description = "Crafting material made from wheat and bark, used in bows and tools.",
                Type = "Material",
                MaxStack = 64,
                BuyPrice = null,
                SellPrice = null,
                IsActive = true,
                IconKey = "rope"
            },

            new Items
            {
                ItemCode = "black_shard",
                Name = "Black Shard",
                Description = "Rare dark shard used to craft the Night Blade.",
                Type = "Material",
                MaxStack = 64,
                BuyPrice = null,
                SellPrice = null,
                IsActive = true,
                IconKey = "black_shard"
            },

            // 8. TOOL

            new Items
            {
                ItemCode = "wood_axe",
                Name = "Wood Axe",
                Description = "Starter axe for gathering wood.",
                Type = "Tool",
                MaxStack = 1,
                BuyPrice = null,
                SellPrice = null,
                IsActive = true,
                IconKey = "wood_axe"
            },

            new Items
            {
                ItemCode = "steel_axe",
                Name = "Steel Axe",
                Description = "Steel axe for harvesting wood.",
                Type = "Tool",
                MaxStack = 1,
                BuyPrice = null,
                SellPrice = null,
                IsActive = true,
                IconKey = "steel_axe"
            },

            new Items
            {
                ItemCode = "gold_axe",
                Name = "Gold Axe",
                Description = "Golden axe for gathering wood.",
                Type = "Tool",
                MaxStack = 1,
                BuyPrice = null,
                SellPrice = null,
                IsActive = true,
                IconKey = "gold_axe"
            },

            new Items
            {
                ItemCode = "mithril_axe",
                Name = "Mithril Axe",
                Description = "Mithril axe for advanced wood gathering.",
                Type = "Tool",
                MaxStack = 1,
                BuyPrice = null,
                SellPrice = null,
                IsActive = true,
                IconKey = "mithril_axe"
            },

            new Items
            {
                ItemCode = "adamantite_axe",
                Name = "Adamantite Axe",
                Description = "High-tier axe for demanding gathering tasks.",
                Type = "Tool",
                MaxStack = 1,
                BuyPrice = null,
                SellPrice = null,
                IsActive = true,
                IconKey = "adamantite_axe"
            },

            new Items
            {
                ItemCode = "wood_pickaxe",
                Name = "Wood Pickaxe",
                Description = "Starter pickaxe for mining.",
                Type = "Tool",
                MaxStack = 1,
                BuyPrice = null,
                SellPrice = null,
                IsActive = true,
                IconKey = "wood_pickaxe"
            },

            new Items
            {
                ItemCode = "steel_pickaxe",
                Name = "Steel Pickaxe",
                Description = "Steel pickaxe for mining.",
                Type = "Tool",
                MaxStack = 1,
                BuyPrice = null,
                SellPrice = null,
                IsActive = true,
                IconKey = "steel_pickaxe"
            },

            new Items
            {
                ItemCode = "gold_pickaxe",
                Name = "Gold Pickaxe",
                Description = "Golden pickaxe for mining.",
                Type = "Tool",
                MaxStack = 1,
                BuyPrice = null,
                SellPrice = null,
                IsActive = true,
                IconKey = "gold_pickaxe"
            },

            new Items
            {
                ItemCode = "mithril_pickaxe",
                Name = "Mithril Pickaxe",
                Description = "Mithril pickaxe for advanced mining.",
                Type = "Tool",
                MaxStack = 1,
                BuyPrice = null,
                SellPrice = null,
                IsActive = true,
                IconKey = "mithril_pickaxe"
            },

            new Items
            {
                ItemCode = "adamantite_pickaxe",
                Name = "Adamantite Pickaxe",
                Description = "High-tier pickaxe for demanding mining tasks.",
                Type = "Tool",
                MaxStack = 1,
                BuyPrice = null,
                SellPrice = null,
                IsActive = true,
                IconKey = "adamantite_pickaxe"
            },

            // 9. WEAPON

            new Items
            {
                ItemCode = "steel_sword",
                Name = "Steel Sword",
                Description = "Steel melee weapon.",
                Type = "Weapon",
                MaxStack = 1,
                BuyPrice = null,
                SellPrice = null,
                IsActive = true,
                IconKey = "steel_melee"
            },

            new Items
            {
                ItemCode = "gold_sword",
                Name = "Gold Sword",
                Description = "Golden melee weapon.",
                Type = "Weapon",
                MaxStack = 1,
                BuyPrice = null,
                SellPrice = null,
                IsActive = true,
                IconKey = "gold_melee"
            },

            new Items
            {
                ItemCode = "mithril_sword",
                Name = "Mithril Sword",
                Description = "Mithril melee weapon.",
                Type = "Weapon",
                MaxStack = 1,
                BuyPrice = null,
                SellPrice = null,
                IsActive = true,
                IconKey = "mithril_melee"
            },

            new Items
            {
                ItemCode = "adamantite_sword",
                Name = "Adamantite Sword",
                Description = "High-tier adamantite melee weapon.",
                Type = "Weapon",
                MaxStack = 1,
                BuyPrice = null,
                SellPrice = null,
                IsActive = true,
                IconKey = "adamantite_melee"
            },

            new Items
            {
                ItemCode = "obanium_sword",
                Name = "Obanium Sword",
                Description = "Rare obanium melee weapon.",
                Type = "Weapon",
                MaxStack = 1,
                BuyPrice = null,
                SellPrice = null,
                IsActive = true,
                IconKey = "obanium_melee"
            },

            new Items
            {
                ItemCode = "night_blade",
                Name = "Night Blade",
                Description = "Rare blade with dark-themed crafting progression.",
                Type = "Weapon",
                MaxStack = 1,
                BuyPrice = null,
                SellPrice = null,
                IsActive = true,
                IconKey = "night_blade"
            },

            new Items
            {
                ItemCode = "wood_bow",
                Name = "Wood Bow",
                Description = "Starter ranged weapon made of wood.",
                Type = "Weapon",
                MaxStack = 1,
                BuyPrice = null,
                SellPrice = null,
                IsActive = true,
                IconKey = "wood_bow"
            },
        };

        private static readonly Dictionary<string, (int healHP, int restoreStamina)>
            ConsumableData = new()
            {
                ["apple"] = (15, 0),
                ["wheat"] = (0, 10),

                ["ligon_mushroom"] = (10, 5),
                ["sugon_mushroom"] = (15, 5),
                ["gulpon_mushroom"] = (15, 10),
                ["slurp_mushroom"] = (10, 15),

                ["raw_meat"] = (5, 0),
                ["meat_soup"] = (35, 25),
                ["cooked_meat"] = (50, 15)
            };

        public static async Task SeedAsync(GameDbContext context)
        {
            var duplicateCodes = ItemData
                .GroupBy(x => x.ItemCode, StringComparer.OrdinalIgnoreCase)
                .Where(group => group.Count() > 1)
                .Select(group => group.Key)
                .ToArray();

            if(duplicateCodes.Length > 0)
            {
                throw new InvalidOperationException(
                    "Dublicate ItemCode in ItemDataSeeder: " +
                    string.Join(", ", duplicateCodes));
            }

            if (ItemData.Any(x =>
            string.IsNullOrWhiteSpace(x.ItemCode) ||
            string.IsNullOrWhiteSpace(x.Name) ||
            x.MaxStack <= 0))
                throw new InvalidOperationException(
                    "ItemDataSeeder contains an invalid ItemCode, Name, or MaxStack");

            await using var transaction = await context.Database.BeginTransactionAsync();

            var existingItems = await context.Items.ToListAsync();
            var existingByCode = new Dictionary<string, Items>(
                StringComparer.OrdinalIgnoreCase);

            foreach(Items existing in existingItems)
            {
                if (string.IsNullOrWhiteSpace(existing.ItemCode))
                    continue;

                if (!existingByCode.TryAdd(existing.ItemCode, existing))
                    throw new InvalidOperationException(
                        "Duplicate ItemCode in PostgreSQL: " + existing.ItemCode);
            }

            int addedCount = 0;
            int updatedCount = 0;

            foreach(Items seed in ItemData)
            {
                if(!existingByCode.TryGetValue(seed.ItemCode, out Items? stored))
                {
                    context.Items.Add(seed);
                    existingByCode.Add(seed.ItemCode, seed);
                    addedCount++;
                    continue;
                }

                bool changed = false;

                if(stored.Name != seed.Name)
                {
                    stored.Name = seed.Name;
                    changed = true;
                }

                if (stored.Description != seed.Description)
                {
                    stored.Description = seed.Description;
                    changed = true;
                }

                if (stored.Type != seed.Type)
                {
                    stored.Type = seed.Type;
                    changed = true;
                }

                if (stored.MaxStack != seed.MaxStack)
                {
                    stored.MaxStack = seed.MaxStack;
                    changed = true;
                }

                if (stored.BuyPrice != seed.BuyPrice)
                {
                    stored.BuyPrice = seed.BuyPrice;
                    changed = true;
                }

                if (stored.SellPrice != seed.SellPrice)
                {
                    stored.SellPrice = seed.SellPrice;
                    changed = true;
                }

                if (stored.IsActive != seed.IsActive)
                {
                    stored.IsActive = seed.IsActive;
                    changed = true;
                }

                if (stored.IconKey != seed.IconKey)
                {
                    stored.IconKey = seed.IconKey;
                    changed = true;
                }

                if (changed)
                    updatedCount++;
            }

            await context.SaveChangesAsync();

            var consumableIds = await context.Items
                .Where(x => ConsumableData.Keys.Contains(x.ItemCode))
                .ToDictionaryAsync(x => x.ItemCode, x => x.Id);

            int[] consumableItemIds = consumableIds.Values.ToArray();

            var existingStats = await context.ConsumableStats
                .Where(x => consumableItemIds.Contains(x.ItemId))
                .ToDictionaryAsync(x => x.ItemId);

            int addedStats = 0;
            int updatedStats = 0;

            foreach(var entry in ConsumableData)
            {
                if (!consumableIds.TryGetValue(entry.Key, out int itemId))
                    continue;

                if (!existingStats.TryGetValue(itemId, out ConsumableStat? stat))
                {
                    context.ConsumableStats.Add(new ConsumableStat
                    {
                        ItemId = itemId,
                        HealHP = entry.Value.healHP,
                        RestoreStamina = entry.Value.restoreStamina
                    });
                    addedStats++;
                    continue;
                }

                if(stat.HealHP != entry.Value.healHP ||
                    stat.RestoreStamina != entry.Value.restoreStamina)
                {
                    stat.HealHP = entry.Value.healHP;
                    stat.RestoreStamina = entry.Value.restoreStamina;
                    updatedStats++;
                }
            }

            await context.SaveChangesAsync();
            await transaction.CommitAsync();

            Console.WriteLine(
                $"[ItemDataSeeder] Items: added={addedCount}, updated={updatedCount}; " +
                $"ConsumableStats: added={addedStats}, updated={updatedStats}");
        }
    }
}
