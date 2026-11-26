using UnityEngine;

public static class RewardWorldDrop
{
    // Spawn exactly a specified reward quantity without removing anything from the player's inventory.
    public static bool Spawn(ItemDefinitionSO definition,int amount,Vector3 position)
    {
        if(definition==null || !definition.IsActive || amount<=0 || definition.MaxStack<=0) return false;
        // WorldItem.AddItem already handles partial pickup, even when a reward stack exceeds inventory maxStack.
        if(definition.WorldPrefab!=null)
        {
            WorldItem world=Object.Instantiate(definition.WorldPrefab,position,Quaternion.identity);
            world.Setup(definition.ItemId,amount);
            return true;
        }
        var go=new GameObject("Loot_"+definition.ItemCode); go.SetActive(false); go.transform.position=position;
        var body=go.AddComponent<Rigidbody>(); body.collisionDetectionMode=CollisionDetectionMode.ContinuousDynamic;
        var solid=go.AddComponent<SphereCollider>(); solid.radius=.18f; solid.isTrigger=false;
        var worldItem=go.AddComponent<WorldItem>(); worldItem.Setup(definition.ItemId,amount);
        var icon=new GameObject("Visual"); icon.transform.SetParent(go.transform,false);
        if(definition.Icon!=null)
        {
            var sr=icon.AddComponent<SpriteRenderer>(); sr.sprite=definition.Icon;
            float size=Mathf.Max(definition.Icon.bounds.size.x,definition.Icon.bounds.size.y);
            if(size>.0001f) icon.transform.localScale=Vector3.one*(.36f/size);
            icon.AddComponent<Nightfall.Stats.PowerupBillboard>();
        }
        else
        {
            var marker=GameObject.CreatePrimitive(PrimitiveType.Cube); marker.transform.SetParent(icon.transform,false);
            marker.transform.localScale=Vector3.one*.28f;
            var collider=marker.GetComponent<Collider>(); collider.enabled=false; Object.Destroy(collider);
        }
        go.SetActive(true); return true;
    }
}
