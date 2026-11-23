using UnityEngine;

namespace Nightfall.Stats
{
    public sealed class PowerupBillboard : MonoBehaviour
    {
        // Face the active camera so fallback powerup icons stay visible in the world.
        private void LateUpdate()
        {
            var cam=Camera.main;
            if(cam!=null) transform.rotation=cam.transform.rotation;
        }
    }
}
