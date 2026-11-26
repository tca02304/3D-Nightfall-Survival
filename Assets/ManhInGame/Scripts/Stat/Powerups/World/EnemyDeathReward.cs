using System;
using UnityEngine;

namespace Nightfall.Stats
{
    [DisallowMultipleComponent]
    public sealed class EnemyDeathReward : MonoBehaviour
    {
        [SerializeField] private EnemyLootTableSO lootTable;
        [SerializeField] private bool elite;
        private readonly System.Random rng=new System.Random(Guid.NewGuid().GetHashCode());
        private bool rewarded;
        public bool Rewarded => rewarded;
        public event Action<EnemyLootResult> RewardGranted;

        // Configure an existing enemy's reward table; independent of enemy AI, spawning, and HP implementation.
        public void Configure(EnemyLootTableSO table,bool isElite=false) { if(!rewarded) { lootTable=table; elite=isElite; } }

        // Call from the existing confirmed Death event with the actual killer, before destroying this enemy.
        public bool TryRewardDeath(PlayerStatProvider killer)
        {
            if(rewarded || lootTable==null || killer==null || ItemDatabase.Instance==null || !ItemDatabase.Instance.IsReady) return false;
            var wallet=killer.GetComponent<GoldWallet>();
            if(lootTable.MaxGold>0 && wallet==null) return false;
            // Validate all configured item identities before touching the wallet or marking the reward consumed.
            foreach(var drop in lootTable.Drops)
            {
                var d=ItemDatabase.Instance.Catalog.GetByCode(drop.itemCode);
                if(d==null || !d.IsActive) { Debug.LogError("[EnemyDeathReward] Unknown loot: "+drop.itemCode,this); return false; }
            }
            EnemyLootResult result;
            try { result=EnemyLootRoller.Roll(lootTable,rng,killer.GetStat(StatType.LootMultiplier),elite,killer.Powerups); }
            catch(ArgumentException e) { Debug.LogError("[EnemyDeathReward] "+e.Message,this); return false; }
            rewarded=true;
            if(result.gold>0) wallet.TryAddGold(result.gold);
            int index=0;
            foreach(var drop in result.items)
            {
                var definition=ItemDatabase.Instance.Catalog.GetByCode(drop.code);
                float a=index++*2.399963f;
                Vector3 p=transform.position+Vector3.up*.55f+new Vector3(Mathf.Cos(a),0f,Mathf.Sin(a))*.4f;
                RewardWorldDrop.Spawn(definition,drop.amount,p);
            }
            if(result.powerup!=null) PowerupPickup.Spawn(result.powerup,transform.position+Vector3.up*1.1f);
            killer.NotifyEnemyKilled();
            RewardGranted?.Invoke(result); return true;
        }

        // Reset only when an enemy pool creates a new life with new combat state.
        public void ResetForNewLife() => rewarded=false;
    }
}
