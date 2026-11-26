#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Nightfall.Stats
{
    public static class StatDataAssetGenerator
    {
        private const string Folder="Assets/ManhInGame/Data/Powerups";

        // Rebuild the catalog from the existing migrated assets; never create duplicate Powerups or overwrite their effects.
        [MenuItem("Tools/Nightfall/Stat/Generate Demo Powerups + Chest Tables")]
        private static void Generate()
        {
            var definitions=new List<PowerupDefinitionSO>(); var codes=new HashSet<string>();
            foreach(string guid in AssetDatabase.FindAssets("t:PowerupDefinitionSO",new[]{Folder}))
            {
                var def=AssetDatabase.LoadAssetAtPath<PowerupDefinitionSO>(AssetDatabase.GUIDToAssetPath(guid));
                if(def==null || string.IsNullOrWhiteSpace(def.Code) || !codes.Add(def.Code) || (def.RuntimeEffects.Length==0 && def.ModifiersPerStack.Length==0))
                { Debug.LogError("[StatData] Missing effect, blank or duplicate powerup code. Apply v12 migrated assets first.",def); return; }
                definitions.Add(def);
            }
            if(definitions.Count==0) { Debug.LogError("[StatData] No Powerup definitions."); return; }
            definitions.Sort((a,b)=>string.CompareOrdinal(a.Code,b.Code));
            var catalog=GetOrCreate<PowerupCatalogSO>(Folder+"/PowerupCatalog.asset");
            catalog.Configure(definitions); EditorUtility.SetDirty(catalog);
            if(!AssetDatabase.IsValidFolder(Folder+"/Chests")) AssetDatabase.CreateFolder(Folder,"Chests");
            MakeChest("Basic",ChestTier.Basic,0,catalog,new float[]{90,9,1,0,0,0});
            MakeChest("Wooden",ChestTier.Wooden,50,catalog,new float[]{60,30,8,2,0,0});
            MakeChest("Iron",ChestTier.Iron,150,catalog,new float[]{35,40,18,6,1,0});
            MakeChest("Crystal",ChestTier.Crystal,400,catalog,new float[]{15,35,32,14,3.5f,.5f});
            MakeChest("Aether",ChestTier.Aether,1000,catalog,new float[]{0,15,35,32,15,3});
            AssetDatabase.SaveAssets(); AssetDatabase.Refresh();
            Debug.Log($"[StatData] {definitions.Count} existing Powerups linked, five chest tables updated. No duplicate powerup assets created.");
        }

        // Reuse or create one chest asset with stable GUID and six approved rarity weights.
        private static void MakeChest(string name,ChestTier tier,int cost,PowerupCatalogSO catalog,float[] values)
        {
            var table=GetOrCreate<ChestDropTableSO>(Folder+"/Chests/ChestDrop_"+name+".asset");
            var weights=new RarityWeight[values.Length];
            for(int i=0;i<values.Length;i++) weights[i]=new RarityWeight((PowerupRarity)i,values[i]);
            table.Configure(tier,cost,catalog,weights); EditorUtility.SetDirty(table);
        }

        // Reuse the catalog/table asset if present rather than replacing existing project references.
        private static T GetOrCreate<T>(string path) where T:ScriptableObject
        {
            var value=AssetDatabase.LoadAssetAtPath<T>(path);
            if(value!=null) return value;
            if(AssetDatabase.LoadMainAssetAtPath(path)!=null) throw new InvalidOperationException("Wrong asset type: "+path);
            value=ScriptableObject.CreateInstance<T>(); AssetDatabase.CreateAsset(value,path); return value;
        }
    }
}
#endif
