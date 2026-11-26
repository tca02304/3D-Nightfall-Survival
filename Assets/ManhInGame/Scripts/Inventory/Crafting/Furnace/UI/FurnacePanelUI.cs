using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nightfall.Crafting
{
    public sealed class FurnacePanelUI : MonoBehaviour
    {
        [SerializeField] private FurnaceController furnace;
        [SerializeField] private Image oreIcon,fuelIcon,outputIcon;
        [SerializeField] private TMP_Text oreCount,fuelCount,outputCount,remainingFuelLabel;
        [SerializeField] private Slider progress;
        [SerializeField,Min(1)] private int insertAmount=1;
        private FurnaceController subscribed;

        // Subscribe the panel to the selected furnace only when the panel is active.
        private void OnEnable() { Subscribe(); Refresh(); }

        // Closing the UI unsubscribes presentation without stopping furnace processing.
        private void OnDisable() { if(subscribed!=null) subscribed.OnFurnaceChanged-=Refresh; subscribed=null; }

        // Bind a dynamically placed furnace when the existing interact system opens the panel.
        public void Bind(FurnaceController target) { furnace=target; if(isActiveAndEnabled) Subscribe(); Refresh(); }

        // Keep only one subscription even when the same furnace is opened repeatedly.
        private void Subscribe()
        {
            if(subscribed==furnace) return;
            if(subscribed!=null) subscribed.OnFurnaceChanged-=Refresh;
            subscribed=furnace;
            if(subscribed!=null) subscribed.OnFurnaceChanged+=Refresh;
        }

        // Refresh material icons, exact amounts, burn credits, and progress using existing item data.
        public void Refresh()
        {
            RenderSlot(oreIcon,oreCount,furnace!=null?furnace.OreId:0,furnace!=null?furnace.OreAmount:0);
            RenderSlot(fuelIcon,fuelCount,furnace!=null?furnace.FuelId:0,furnace!=null?furnace.FuelAmount:0);
            RenderSlot(outputIcon,outputCount,furnace!=null?furnace.OutputId:0,furnace!=null?furnace.OutputAmount:0);
            if(progress!=null) progress.normalizedValue=furnace!=null?furnace.Progress01:0f;
            if(remainingFuelLabel!=null) remainingFuelLabel.text=(furnace!=null?furnace.RemainingFuelUses:0).ToString();
        }

        // Render only the item image, never disable the entire furnace slot object.
        private static void RenderSlot(Image icon,TMP_Text text,int id,int amount)
        {
            var d=ItemDatabase.Instance!=null?ItemDatabase.Instance.GetDefinitionById(id):null;
            if(icon!=null) { icon.sprite=d!=null?d.Icon:null; icon.enabled=amount>0 && icon.sprite!=null; icon.preserveAspect=true; }
            if(text!=null) text.text=amount>0?amount.ToString():string.Empty;
        }

        // Insert ore from the selected hotbar slot through a Unity UI button.
        public void InsertSelectedOre() { if(furnace!=null && HotbarManager.Instance!=null) furnace.TryInsertOreFromSlot(HotbarManager.Instance.SelectedInventoryIndex,insertAmount); }

        // Insert fuel from the selected hotbar slot through a Unity UI button.
        public void InsertSelectedFuel() { if(furnace!=null && HotbarManager.Instance!=null) furnace.TryInsertFuelFromSlot(HotbarManager.Instance.SelectedInventoryIndex,insertAmount); }

        // Support inventory drag-drop handlers without introducing a second inventory manager.
        public bool InsertOreFromSlot(int slotIndex,int amount) => furnace!=null && furnace.TryInsertOreFromSlot(slotIndex,amount);

        // Support inventory drag-drop handlers for the fuel slot.
        public bool InsertFuelFromSlot(int slotIndex,int amount) => furnace!=null && furnace.TryInsertFuelFromSlot(slotIndex,amount);

        // Recover as much output as fits in the inventory.
        public void TakeAllOutput() { if(furnace!=null) furnace.TakeOutput(); }

        // Recover all remaining ore that fits.
        public void TakeAllOre() { if(furnace!=null) furnace.TakeOre(); }

        // Recover all unused fuel that fits.
        public void TakeAllFuel() { if(furnace!=null) furnace.TakeFuel(); }
    }
}
