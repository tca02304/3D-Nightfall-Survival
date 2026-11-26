using UnityEngine;

namespace Nightfall.Crafting
{
    public sealed class FurnaceRuntimeTest : MonoBehaviour
    {
        [SerializeField] private TextAsset smeltingRecipesCsv;
        [SerializeField] private TextAsset furnaceFuelCsv;

        // Test the real controller in a disposable isolated station; never touch the player's live inventory.
        [ContextMenu("Run Isolated Furnace Test (Play Mode)")]
        private void RunTest()
        {
            if(!Application.isPlaying || ItemDatabase.Instance==null || !ItemDatabase.Instance.IsReady)
            { Debug.LogError("[FurnaceTest] Enter Play mode with a ready ItemDatabase.",this); return; }
            var go=new GameObject("Furnace_Test_Only"); go.SetActive(false);
            var furnace=go.AddComponent<FurnaceController>();
            try
            {
                if(!furnace.ConfigureTables(smeltingRecipesCsv,furnaceFuelCsv)) return;
                var catalog=ItemDatabase.Instance.Catalog;
                int iron=catalog.GetByCode("iron_ore").ItemId, coal=catalog.GetByCode("coal").ItemId;
                int bar=catalog.GetByCode("iron_bar").ItemId;
                bool ok=furnace.TryRestore(new FurnaceState { oreId=iron,oreAmount=3,fuelId=coal,fuelAmount=1 });
                furnace.Tick(5f); ok &= furnace.OutputAmount==0 && furnace.OreAmount==3;
                furnace.Tick(1f); ok &= furnace.OutputId==bar && furnace.OutputAmount==1 && furnace.RemainingFuelUses==1;
                furnace.Tick(6f); ok &= furnace.OutputAmount==2 && furnace.OreAmount==1 && furnace.RemainingFuelUses==0;
                furnace.Tick(60f); ok &= furnace.OutputAmount==2 && furnace.OreAmount==1;
                Debug.Log($"[FurnaceTest] 3 ore + 1 coal -> 2 bars and 1 ore remains: {(ok?"PASS":"CHECK")}",this);
            }
            finally { Destroy(go); }
        }
    }
}
