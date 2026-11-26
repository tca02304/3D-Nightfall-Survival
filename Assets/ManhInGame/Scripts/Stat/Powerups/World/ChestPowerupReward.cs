using System;
using UnityEngine;

namespace Nightfall.Stats
{
    public enum ChestOpenResult { Success, AlreadyOpened, MissingConfiguration, InsufficientGold, NoEligibleReward, GrantFailed }

    [DisallowMultipleComponent]
    public sealed class ChestPowerupReward : MonoBehaviour
    {
        [SerializeField] private ChestDropTableSO dropTable;
        [SerializeField] private Transform rewardPoint;
        [SerializeField] private PowerupPickup pickupPrefab;
        [SerializeField] private bool spawnPickup=true;
        private readonly System.Random rng=new System.Random(Guid.NewGuid().GetHashCode());
        private bool opening, opened;
        public bool IsOpened => opened;
        public int GoldCost => dropTable!=null ? dropTable.GoldCost : -1;
        public ChestDropTableSO Table => dropTable;
        public event Action<PowerupDefinitionSO> Opened;

        // Configure the table when the existing teammate spawner already knows the chest tier.
        public void Configure(ChestDropTableSO table) { if(!opening && !opened) dropTable=table; }

        // Validate reward and player, charge once, then grant or spawn one pickup; keep original chest spawning unchanged.
        public ChestOpenResult TryOpen(PlayerStatProvider player)
        {
            if(opening || opened) return ChestOpenResult.AlreadyOpened;
            if(dropTable==null || player==null || player.Powerups==null) return ChestOpenResult.MissingConfiguration;
            var wallet=player.GetComponent<GoldWallet>();
            if(dropTable.GoldCost>0 && wallet==null) return ChestOpenResult.MissingConfiguration;
            if(dropTable.GoldCost>0 && !wallet.CanSpend(dropTable.GoldCost)) return ChestOpenResult.InsufficientGold;
            opening=true;
            try
            {
                PowerupDefinitionSO reward;
                try { reward=ChestLootRoller.RollPowerup(dropTable,rng,player.Powerups); }
                catch(ArgumentException e) { Debug.LogError("[ChestReward] "+e.Message,this); return ChestOpenResult.MissingConfiguration; }
                if(reward==null || !player.Powerups.CanAdd(reward.Code)) return ChestOpenResult.NoEligibleReward;
                int cost=dropTable.GoldCost;
                if(cost>0 && !wallet.TrySpendGold(cost)) return ChestOpenResult.InsufficientGold;
                bool granted;
                if(spawnPickup)
                {
                    Vector3 p=rewardPoint!=null?rewardPoint.position:transform.position+Vector3.up*1.25f;
                    granted=PowerupPickup.Spawn(reward,p,1,pickupPrefab)!=null;
                }
                else granted=player.TryGrantPowerup(reward);
                if(!granted)
                {
                    if(cost>0) wallet.TryAddGold(cost);
                    return ChestOpenResult.GrantFailed;
                }
                opened=true;
                Opened?.Invoke(reward);
                return ChestOpenResult.Success;
            }
            finally { opening=false; }
        }

        // Reset only for a deliberately new pooled chest spawn, never on a regular UI close/open.
        public void ResetForNewSpawn() { if(!opening) opened=false; }

        // Restore an explicitly saved state for a chest with a stable world identity managed by the spawner.
        public void RestoreOpenedState(bool wasOpened) { if(!opening) opened=wasOpened; }
    }
}
