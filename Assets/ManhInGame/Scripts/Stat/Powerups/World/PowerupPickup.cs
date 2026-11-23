using UnityEngine;

namespace Nightfall.Stats
{
    [DisallowMultipleComponent]
    public sealed class PowerupPickup : MonoBehaviour
    {
        [SerializeField] private PowerupDefinitionSO definition;
        [SerializeField,Min(1)] private int stacks=1;
        [SerializeField] private SpriteRenderer iconRenderer;
        [SerializeField,Min(.1f)] private float pickupRadius=.8f;
        private bool claimed;
        public PowerupDefinitionSO Definition => definition;

        // Use or create a trigger and ensure trigger events can reach the root without altering solid colliders.
        private void Awake()
        {
            bool hasTrigger=false;
            foreach(var c in GetComponentsInChildren<Collider>(true)) if(c.isTrigger) { hasTrigger=true; break; }
            if(!hasTrigger)
            {
                var child=new GameObject("PowerupTrigger"); child.layer=gameObject.layer; child.transform.SetParent(transform,false);
                var sphere=child.AddComponent<SphereCollider>(); sphere.isTrigger=true; sphere.radius=pickupRadius;
            }
            if(GetComponent<Rigidbody>()==null)
            { var rb=gameObject.AddComponent<Rigidbody>(); rb.isKinematic=true; rb.useGravity=false; }
        }

        // Initialize a spawned pickup with an approved catalog definition.
        public void Setup(PowerupDefinitionSO powerup,int amount=1)
        {
            definition=powerup; stacks=Mathf.Max(1,amount); claimed=false;
            if(iconRenderer!=null) iconRenderer.sprite=powerup!=null?powerup.Icon:null;
        }

        // Collect on contact without requiring an interaction key.
        private void OnTriggerEnter(Collider other) => TryCollect(other.GetComponentInParent<PlayerStatProvider>());

        // Retry for players who were not ready or were temporarily at the stack limit.
        private void OnTriggerStay(Collider other) => TryCollect(other.GetComponentInParent<PlayerStatProvider>());

        // Guard against multiple player colliders or duplicate event callbacks before granting a stack.
        public bool TryCollect(PlayerStatProvider player)
        {
            if(claimed || definition==null || player==null || player.Powerups==null || !player.Powerups.CanAdd(definition.Code,stacks)) return false;
            claimed=true;
            if(!player.TryGrantPowerup(definition,stacks)) { claimed=false; return false; }
            gameObject.SetActive(false); Destroy(gameObject); return true;
        }

        // Create a standalone pickup with a sprite fallback; the teammate's own Powerup prefab is also supported.
        public static PowerupPickup Spawn(PowerupDefinitionSO powerup,Vector3 position,int amount=1,PowerupPickup prefab=null)
        {
            if(powerup==null || amount<=0) return null;
            if(prefab!=null)
            { var p=Instantiate(prefab,position,Quaternion.identity); p.Setup(powerup,amount); return p; }
            var go=new GameObject("Powerup_"+powerup.Code); go.transform.position=position;
            var pickup=go.AddComponent<PowerupPickup>(); pickup.Setup(powerup,amount);
            var sprite=new GameObject("Icon"); sprite.transform.SetParent(go.transform,false);
            if(powerup.Icon!=null)
            {
                var sr=sprite.AddComponent<SpriteRenderer>(); sr.sprite=powerup.Icon;
                float size=Mathf.Max(powerup.Icon.bounds.size.x,powerup.Icon.bounds.size.y);
                if(size>.0001f) sprite.transform.localScale=Vector3.one*(.6f/size);
            }
            else
            {
                var marker=GameObject.CreatePrimitive(PrimitiveType.Sphere); marker.transform.SetParent(sprite.transform,false);
                marker.transform.localScale=Vector3.one*.35f; var col=marker.GetComponent<Collider>(); col.enabled=false; Destroy(col);
            }
            sprite.AddComponent<PowerupBillboard>(); return pickup;
        }
    }
}
