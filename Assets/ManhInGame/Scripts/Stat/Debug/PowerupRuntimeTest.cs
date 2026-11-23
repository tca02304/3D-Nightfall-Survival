using System;
using System.Collections.Generic;
using UnityEngine;

namespace Nightfall.Stats
{
    public sealed class PowerupRuntimeTest : MonoBehaviour
    {
        [SerializeField] private PowerupCatalogSO catalog;
        [SerializeField] private ChestDropTableSO chestTable;
        [SerializeField, Min(100)] private int trials = 100000;
        [SerializeField] private ChestDropTableSO[] allFiveChests;
        [SerializeField] private EnemyLootTableSO enemyTable;

        // Run a repeatable simulated rarity distribution without opening real chests.
        [ContextMenu("Run Chest Rarity Distribution Test")]
        private void TestChestPercent()
        {
            if (chestTable == null) { Debug.LogError("[StatTest] Assign ChestDropTableSO", this); return; }
            var rng = new System.Random(20261120);
            int[] counts = new int[Enum.GetValues(typeof(PowerupRarity)).Length];
            for (int i = 0; i < trials; i++)
                counts[(int)ChestLootRoller.RollRarity(chestTable.RarityWeights, rng)]++;
            for (int i = 0; i < counts.Length; i++)
                Debug.Log($"[ChestPercent] {(PowerupRarity)i}: {100d * counts[i] / trials:F2}% ({counts[i]}/{trials})", this);
        }

        // Ensure stacking recalculates stats and never mutates the underlying base value.
        [ContextMenu("Run Powerup Stack Stat Test")]
        private void TestPowerups()
        {
            if (catalog == null) { Debug.LogError("[StatTest] Assign PowerupCatalogSO", this); return; }
            var runtime = new PowerupRuntime(catalog);
            PowerupDefinitionSO definition = catalog.Find("red_pill");
            if (definition == null) { Debug.LogWarning("[StatTest] red_pill not in catalog", this); return; }
            runtime.TryAdd(definition.Code, 3);
            float boosted = StatCalculator.Calculate(100f, StatType.MaxHealth, runtime.GetStatModifiers());
            float repeat = StatCalculator.Calculate(100f, StatType.MaxHealth, runtime.GetStatModifiers());
            runtime.TryRemove(definition.Code, 1);
            float reduced = StatCalculator.Calculate(100f, StatType.MaxHealth, runtime.GetStatModifiers());
            Debug.Log($"[StatTest] 3 red_pill={boosted}, repeated={repeat}, 2 red_pill={reduced}. " +
                ((Mathf.Approximately(boosted, 130f) && Mathf.Approximately(repeat, 130f) && Mathf.Approximately(reduced, 120f)) ? "PASS" : "CHECK"), this);
        }
        // Validate actual configured assets and test observed rarity frequencies against sampling error.
        [ContextMenu("Run All Chest And Buff Checks")]
        private void RunAllChecks()
        {
            if(catalog==null || allFiveChests==null || allFiveChests.Length!=5)
            { Debug.LogError("[StatTest] Assign catalog and all 5 chest tables.",this); return; }
            var numbers=new HashSet<int>(); bool ok=true; var codes=new HashSet<string>();
            foreach(var d in catalog.Definitions)
                if(d==null || string.IsNullOrWhiteSpace(d.Code) || !codes.Add(d.Code) || (d.RuntimeEffects.Length==0 && d.ModifiersPerStack.Length==0)) ok=false;
            foreach(var table in allFiveChests)
            {
                if(table==null || !numbers.Add(table.ChestNumber)) { ok=false; continue; }
                var rng=new System.Random(12995); int[] counts=new int[6];
                try
                {
                    if(ChestLootRoller.RollPowerup(table,rng)==null) ok=false;
                    for(int i=0;i<trials;i++) counts[(int)ChestLootRoller.RollRarity(table.RarityWeights,rng)]++;
                    foreach(var entry in table.RarityWeights)
                    {
                        double p=entry.weight/100d, measured=(double)counts[(int)entry.rarity]/trials;
                        double sigma=Math.Sqrt(p*(1-p)/trials);
                        bool pass=p==0d ? measured==0d : Math.Abs(measured-p)<=5*sigma+1d/trials;
                        ok &= pass;
                        Debug.Log($"[StatTest] Chest {table.ChestNumber}, {entry.rarity}: {measured:P3} vs {p:P3}: {(pass?"PASS":"CHECK")}",this);
                    }
                }
                catch(Exception e) { ok=false; Debug.LogError(e.Message,this); }
            }
            var buffs=new TimedBuffRuntime(); var boost=new StatModifier(StatType.Attack,StatModifierMode.AdditivePercent,.2f);
            buffs.Apply("test",boost,2f); buffs.Apply("test",boost,2f);
            ok &= Mathf.Approximately(StatCalculator.Calculate(100f,StatType.Attack,buffs.GetModifiers()),120f);
            buffs.Tick(2f); ok &= Mathf.Approximately(StatCalculator.Calculate(100f,StatType.Attack,buffs.GetModifiers()),100f);
            Debug.Log($"[StatTest] Chest and buff checks: {(ok?"PASS":"CHECK")}",this);
        }

        // Verify the actual catalog's tool tiers and that raw mining damage is exactly zero for forbidden tools.
        [ContextMenu("Run Tool Tier Checks (Play Mode)")]
        private void TestToolGate()
        {
            var db=ItemDatabase.Instance;
            if(db==null || !db.IsReady) { Debug.LogError("[StatTest] ItemDatabase is not ready.",this); return; }
            ItemData wood=db.GetItemById(db.Catalog.GetByCode("wood_pickaxe").ItemId);
            ItemData steel=db.GetItemById(db.Catalog.GetByCode("steel_pickaxe").ItemId);
            ItemData axe=db.GetItemById(db.Catalog.GetByCode("wood_axe").ItemId);
            bool ok=ItemGameplayRules.GetBaseGatherDamage(wood,"iron_ore")>0f && ItemGameplayRules.GetBaseGatherDamage(wood,"gold_ore")==0f &&
                ItemGameplayRules.GetBaseGatherDamage(steel,"gold_ore")>0f && ItemGameplayRules.GetBaseGatherDamage(axe,"wood_birch")>0f &&
                ItemGameplayRules.GetBaseGatherDamage(axe,"iron_ore")==0f && ItemGameplayRules.GetBaseGatherDamage(wood,"wood")==0f;
            Debug.Log($"[StatTest] Tool gating: {(ok?"PASS":"CHECK")}",this);
        }

        // Sample the assigned enemy profile without adding gold or spawning world items.
        [ContextMenu("Run Enemy Loot Percent Test")]
        private void TestEnemyLoot()
        {
            if(enemyTable==null) { Debug.LogError("[StatTest] Assign an enemy loot table.",this); return; }
            var rng=new System.Random(289); var counts=new Dictionary<string,int>(); long totalGold=0;
            for(int i=0;i<trials;i++)
            {
                var r=EnemyLootRoller.Roll(enemyTable,rng); totalGold+=r.gold;
                foreach(var drop in r.items) counts[drop.code]=counts.TryGetValue(drop.code,out int n)?n+1:1;
            }
            foreach(var d in enemyTable.Drops) Debug.Log($"[EnemyLoot] {d.itemCode}: {100d*(counts.TryGetValue(d.itemCode,out int n)?n:0)/trials:F3}% vs {d.chancePercent}%",this);
            Debug.Log($"[EnemyLoot] Mean gold: {(double)totalGold/trials:F2}",this);
        }
    }
}
