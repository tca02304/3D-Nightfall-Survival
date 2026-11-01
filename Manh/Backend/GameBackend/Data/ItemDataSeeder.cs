using GameBackend.Models;
using Microsoft.EntityFrameworkCore;

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
                ItemCode = "slime",
                Name = "Slime",
                Description = "A sticky substance dropped by Slimes.",
                Type = "Material",
                MaxStack = 64,
                BuyPrice = null,
                SellPrice = 3,
                IsActive = true,
                IconKey = "slime"
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
            var existingCodes = await context.Items
                .Select(x => x.ItemCode)
                .ToHashSetAsync();

            var newItems = ItemData
                .Where(x => !existingCodes.Contains(x.ItemCode))
                .ToList();

            if(newItems.Count > 0)
            {
                context.Items.AddRange(newItems);
                await context.SaveChangesAsync();
            }

            var itemIds = await context.Items
                .Where(x => ConsumableData.Keys.Contains(x.ItemCode))
                .ToDictionaryAsync(x => x.ItemCode, x => x.Id);

            var existingStatIds = await context.ConsumableStats
                .Select(x => x.ItemId)
                .ToHashSetAsync();

            var newStats = new List<ConsumableStat>();

            foreach(var data in ConsumableData)
            {
                if (!itemIds.TryGetValue(data.Key, out int itemId))
                    continue;

                if (existingStatIds.Contains(itemId))
                    continue;

                newStats.Add(new ConsumableStat
                {
                    ItemId = itemId,
                    HealHP = data.Value.healHP,
                    RestoreStamina = data.Value.restoreStamina
                });
            }

            if(newStats.Count > 0)
            {
                context.ConsumableStats.AddRange(newStats);
                await context.SaveChangesAsync();
            }
        }
    }
}
