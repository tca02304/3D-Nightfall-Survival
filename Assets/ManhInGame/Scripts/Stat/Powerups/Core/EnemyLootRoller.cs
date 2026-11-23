using System;
using System.Collections.Generic;
using UnityEngine;

namespace Nightfall.Stats
{
    public struct RolledItemDrop { public string code; public int amount; }
    public sealed class EnemyLootResult { public int gold; public List<RolledItemDrop> items=new List<RolledItemDrop>(); public PowerupDefinitionSO powerup; }

    public static class EnemyLootRoller
    {
        // Roll an inclusive range safely, including the int.MaxValue edge case.
        private static int RangeInclusive(System.Random rng,int min,int max) => (int)(min+Math.Floor(rng.NextDouble()*((long)max-min+1L)));

        // Scale quantities only, never change the chance of a rare drop.
        private static int ScaleAmount(int amount,float multiplier,System.Random rng)
        {
            double n=Math.Min(int.MaxValue,Math.Max(0d,amount*(double)multiplier));
            double whole=Math.Floor(n);
            if(whole<int.MaxValue && rng.NextDouble()<n-whole) whole++;
            return (int)whole;
        }

        // Validate the table first and roll each loot entry independently plus the guaranteed gold range.
        public static EnemyLootResult Roll(EnemyLootTableSO table,System.Random rng,float quantityMultiplier=1f,bool elite=false,PowerupRuntime powerups=null)
        {
            if(table==null || rng==null || table.MinGold<0 || table.MaxGold<table.MinGold ||
                !(quantityMultiplier>0f) || float.IsInfinity(quantityMultiplier)) throw new ArgumentException("Invalid enemy loot settings.");
            foreach(var d in table.Drops)
                if(string.IsNullOrWhiteSpace(d.itemCode) || !(d.chancePercent>=0f && d.chancePercent<=100f) ||
                    d.minAmount<1 || d.maxAmount<d.minAmount) throw new ArgumentException("Invalid loot entry: "+d.itemCode);
            float multiplier=quantityMultiplier*(elite?1.25f:1f);
            var result=new EnemyLootResult { gold=ScaleAmount(RangeInclusive(rng,table.MinGold,table.MaxGold),multiplier,rng) };
            foreach(var d in table.Drops)
            {
                if(rng.NextDouble()*100d>=d.chancePercent) continue;
                int amount=ScaleAmount(RangeInclusive(rng,d.minAmount,d.maxAmount),multiplier,rng);
                if(amount>0) result.items.Add(new RolledItemDrop { code=d.itemCode,amount=amount });
            }
            if(table.BossPowerupTable!=null) result.powerup=ChestLootRoller.RollPowerup(table.BossPowerupTable,rng,powerups);
            return result;
        }
    }
}
