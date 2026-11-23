using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace Nightfall.Crafting
{
    [Serializable]
    public sealed class FurnaceState
    {
        public int oreId, oreAmount, fuelId, fuelAmount, outputId, outputAmount, remainingFuelUses;
        public float progressSeconds;
    }

    [DisallowMultipleComponent]
    public sealed class FurnaceController : MonoBehaviour
    {
        [SerializeField] private TextAsset smeltingRecipesCsv;
        [SerializeField] private TextAsset furnaceFuelCsv;
        [SerializeField,Min(1)] private int slotCapacity=64;
        [SerializeField] private bool simulateAutomatically=true;
        private struct Recipe { public int outputId; public float seconds; }
        private readonly Dictionary<int,Recipe> recipes=new Dictionary<int,Recipe>();
        private readonly Dictionary<int,int> fuels=new Dictionary<int,int>();
        private FurnaceState state=new FurnaceState();
        private bool ready, badConfiguration, busy;
        public bool IsReady => ready;
        public int OreId => state.oreId;
        public int OreAmount => state.oreAmount;
        public int FuelId => state.fuelId;
        public int FuelAmount => state.fuelAmount;
        public int OutputId => state.outputId;
        public int OutputAmount => state.outputAmount;
        public int RemainingFuelUses => state.remainingFuelUses;
        public float ProgressSeconds => state.progressSeconds;
        public float CurrentRecipeSeconds => recipes.TryGetValue(OreId,out var r)?r.seconds:0f;
        public float Progress01 => CurrentRecipeSeconds>0f?Mathf.Clamp01(ProgressSeconds/CurrentRecipeSeconds):0f;
        public event Action OnFurnaceChanged;

        // Initialize lazily after the actual local ItemDatabase is ready; simulation continues even when UI is closed.
        private void Update()
        {
            if(!ready && !badConfiguration) Initialize();
            if(ready && simulateAutomatically) Tick(Time.deltaTime);
        }

        // Assign data files to a newly spawned station or isolated test before initialization.
        public bool ConfigureTables(TextAsset recipesCsv, TextAsset fuelCsv)
        {
            if(ready || busy || OreAmount>0 || FuelAmount>0 || OutputAmount>0 || RemainingFuelUses>0) return false;
            smeltingRecipesCsv=recipesCsv; furnaceFuelCsv=fuelCsv; badConfiguration=false;
            return Initialize();
        }

        // Parse configured CSV files against current ItemCatalog definitions rather than legacy hardcoded numeric IDs.
        public bool Initialize()
        {
            if(ready) return true;
            if(ItemDatabase.Instance==null || !ItemDatabase.Instance.IsReady) return false;
            recipes.Clear(); fuels.Clear();
            try
            {
                if(smeltingRecipesCsv==null || furnaceFuelCsv==null) throw new FormatException("Assign SmeltingRecipes and FurnaceFuel CSV.");
                foreach(var row in Rows(smeltingRecipesCsv))
                {
                    if(row.Length!=3 || !float.TryParse(row[2],NumberStyles.Float,CultureInfo.InvariantCulture,out float seconds) || !(seconds>0f) || float.IsInfinity(seconds))
                        throw new FormatException("Invalid smelting recipe: "+string.Join(",",row));
                    int input=RequireId(row[0]), output=RequireId(row[1]);
                    if(input==output || recipes.ContainsKey(input)) throw new FormatException("Invalid or duplicate ore: "+row[0]);
                    recipes.Add(input,new Recipe { outputId=output,seconds=seconds });
                }
                foreach(var row in Rows(furnaceFuelCsv))
                {
                    if(row.Length!=2 || !int.TryParse(row[1],out int uses) || uses<1 || uses>100) throw new FormatException("Invalid fuel row.");
                    int id=RequireId(row[0]); if(fuels.ContainsKey(id)) throw new FormatException("Duplicate fuel: "+row[0]);
                    fuels.Add(id,uses);
                }
                if(recipes.Count==0 || fuels.Count==0) throw new FormatException("Empty furnace table.");
                ready=true; badConfiguration=false; return true;
            }
            catch(Exception e) { recipes.Clear(); fuels.Clear(); badConfiguration=true; Debug.LogError("[Furnace] "+e.Message,this); return false; }
        }

        // Read simple project CSV rows, stripping BOM, whitespace and the header.
        private static IEnumerable<string[]> Rows(TextAsset csv)
        {
            var lines=csv.text.Replace("\r","").TrimStart('\ufeff').Split('\n');
            for(int i=1;i<lines.Length;i++)
            {
                if(string.IsNullOrWhiteSpace(lines[i])) continue;
                var fields=lines[i].Split(','); for(int j=0;j<fields.Length;j++) fields[j]=fields[j].Trim();
                yield return fields;
            }
        }

        // Resolve a known active catalog item code to its persistent numeric identity.
        private static int RequireId(string code)
        {
            var d=ItemDatabase.Instance.Catalog.GetByCode(code);
            if(d==null || !d.IsActive) throw new FormatException("Unknown or inactive item: "+code);
            return d.ItemId;
        }

        // Bound the furnace slot capacity by both station configuration and item-specific maximum stack.
        private int Capacity(int id)
        {
            var item=ItemDatabase.Instance!=null?ItemDatabase.Instance.GetItemById(id):null;
            return item!=null?Mathf.Min(slotCapacity,item.maxStack):0;
        }

        // Insert validated ore from an actual inventory slot, retaining the original stable item ID.
        public bool TryInsertOreFromSlot(int slotIndex,int amount=1) => TryInsert(slotIndex,amount,true);

        // Insert validated fuel from an actual inventory slot, rejecting non-fuel materials.
        public bool TryInsertFuelFromSlot(int slotIndex,int amount=1) => TryInsert(slotIndex,amount,false);

        // Transfer an exact quantity once; the inventory service otherwise permits partial removal.
        private bool TryInsert(int slotIndex,int amount,bool ore)
        {
            if(!Initialize() || busy || amount<=0) return false;
            var inv=InventoryManager.Instance;
            if(inv==null || slotIndex<0 || slotIndex>=inv.Slots.Count) return false;
            var slot=inv.Slots[slotIndex]; if(slot.isEmpty() || slot.Amount<amount) return false;
            int id=slot.ItemId, current=ore?state.oreId:state.fuelId, quantity=ore?state.oreAmount:state.fuelAmount;
            if((ore?!recipes.ContainsKey(id):!fuels.ContainsKey(id)) || (quantity>0 && current!=id) || (long)quantity+amount>Capacity(id)) return false;
            busy=true;
            try
            {
                if(!inv.RemoveItemAt(slotIndex,amount)) return false;
                if(ore) { if(state.oreId!=id) state.progressSeconds=0f; state.oreId=id; state.oreAmount+=amount; }
                else { state.fuelId=id; state.fuelAmount+=amount; }
            }
            finally { busy=false; }
            OnFurnaceChanged?.Invoke(); return true;
        }

        // Check ore, available fuel and compatible output space before advancing progress.
        private bool CanProcess(out Recipe recipe)
        {
            recipe=default;
            return state.oreAmount>0 && recipes.TryGetValue(state.oreId,out recipe) &&
                (state.remainingFuelUses>0 || (state.fuelAmount>0 && fuels.ContainsKey(state.fuelId))) &&
                (state.outputAmount==0 || state.outputId==recipe.outputId) && state.outputAmount<Capacity(recipe.outputId);
        }

        // Simulate elapsed game time; blocked output or missing fuel never consumes ore or resets accumulated progress.
        public void Tick(float seconds)
        {
            if(!ready || busy || !(seconds>0f) || float.IsInfinity(seconds)) return;
            bool changed=false;
            // The slot limit bounds ordinary work; the loop cap protects the frame from malformed large delta values.
            int completed=0;
            while(seconds>0f && completed<256 && CanProcess(out var recipe))
            {
                float required=Mathf.Max(0f,recipe.seconds-state.progressSeconds);
                if(seconds<required) { state.progressSeconds+=seconds; changed=true; break; }
                seconds-=required; state.progressSeconds=0f;
                if(state.remainingFuelUses==0)
                {
                    state.remainingFuelUses=fuels[state.fuelId]; state.fuelAmount--;
                    if(state.fuelAmount==0) state.fuelId=0;
                }
                state.remainingFuelUses--; state.oreAmount--;
                if(state.oreAmount==0) state.oreId=0;
                state.outputId=recipe.outputId; state.outputAmount++;
                changed=true; completed++;
            }
            if(changed) OnFurnaceChanged?.Invoke();
        }

        // Return as much output as inventory can accept, keeping all overflow in the furnace.
        public int TakeOutput(int amount=int.MaxValue) => Take(2,amount);

        // Return unprocessed ore, preserving progress only if the same ore remains.
        public int TakeOre(int amount=int.MaxValue) => Take(0,amount);

        // Return unused physical fuel; already loaded burn credits are not converted back into coal.
        public int TakeFuel(int amount=int.MaxValue) => Take(1,amount);

        // Perform a guarded partial transfer back to the existing inventory; return the number actually moved.
        private int Take(int kind,int amount)
        {
            if(!ready || busy || amount<=0 || InventoryManager.Instance==null || ItemDatabase.Instance==null) return 0;
            int id=kind==0?OreId:kind==1?FuelId:OutputId;
            int available=kind==0?OreAmount:kind==1?FuelAmount:OutputAmount;
            if(id<=0 || available<=0) return 0;
            var data=ItemDatabase.Instance.GetItemById(id); if(data==null) return 0;
            int request=Mathf.Min(amount,available), moved=0;
            busy=true;
            try
            {
                int remaining=InventoryManager.Instance.AddItem(data,request); moved=request-Mathf.Clamp(remaining,0,request);
                if(kind==0) { state.oreAmount-=moved; if(state.oreAmount==0) { state.oreId=0; state.progressSeconds=0f; } }
                else if(kind==1) { state.fuelAmount-=moved; if(state.fuelAmount==0) state.fuelId=0; }
                else { state.outputAmount-=moved; if(state.outputAmount==0) state.outputId=0; }
            }
            finally { busy=false; }
            if(moved>0) OnFurnaceChanged?.Invoke(); return moved;
        }

        // Capture a copy of the complete workstation state for a world save.
        public FurnaceState Capture() => new FurnaceState { oreId=OreId,oreAmount=OreAmount,fuelId=FuelId,fuelAmount=FuelAmount,
            outputId=OutputId,outputAmount=OutputAmount,remainingFuelUses=RemainingFuelUses,progressSeconds=ProgressSeconds };

        // Validate three slots and progress atomically before restoring a stored furnace instance.
        public bool TryRestore(FurnaceState snapshot)
        {
            if(!Initialize() || busy || snapshot==null || !ValidSlot(snapshot.oreId,snapshot.oreAmount) || !ValidSlot(snapshot.fuelId,snapshot.fuelAmount) ||
                !ValidSlot(snapshot.outputId,snapshot.outputAmount) || snapshot.remainingFuelUses<0 || !(snapshot.progressSeconds>=0f) || float.IsInfinity(snapshot.progressSeconds)) return false;
            int maxUses=0; foreach(int uses in fuels.Values) maxUses=Math.Max(maxUses,uses);
            if(snapshot.remainingFuelUses>=maxUses || (snapshot.oreAmount>0 && !recipes.ContainsKey(snapshot.oreId)) ||
                (snapshot.fuelAmount>0 && !fuels.ContainsKey(snapshot.fuelId))) return false;
            bool validOutput=snapshot.outputAmount==0;
            foreach(var r in recipes.Values) if(r.outputId==snapshot.outputId) validOutput=true;
            if(!validOutput || (snapshot.oreAmount==0 && snapshot.progressSeconds!=0f) ||
                (snapshot.oreAmount>0 && snapshot.progressSeconds>=recipes[snapshot.oreId].seconds)) return false;
            state=new FurnaceState { oreId=snapshot.oreId,oreAmount=snapshot.oreAmount,fuelId=snapshot.fuelId,fuelAmount=snapshot.fuelAmount,
                outputId=snapshot.outputId,outputAmount=snapshot.outputAmount,remainingFuelUses=snapshot.remainingFuelUses,progressSeconds=snapshot.progressSeconds };
            OnFurnaceChanged?.Invoke(); return true;
        }

        // Accept only canonical empty slots or a positive known stack within capacity.
        private bool ValidSlot(int id,int amount) => amount==0?id==0:amount>0 && id>0 && amount<=Capacity(id);
    }
}
