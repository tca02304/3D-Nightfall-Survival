
using System.Collections.Generic;
using UnityEngine;

namespace Nightfall.Stats
{
    [CreateAssetMenu(fileName = "PowerupCatalog", menuName = "Nightfall/Stat/Powerup Catalog")]
    public sealed class PowerupCatalogSO : ScriptableObject
    {
        [SerializeField] private List<PowerupDefinitionSO> definitions = new List<PowerupDefinitionSO>();
        public IReadOnlyList<PowerupDefinitionSO> Definitions => definitions;

        // Find a powerup by its stable string code.
        public PowerupDefinitionSO Find(string code)
        {
            if (string.IsNullOrWhiteSpace(code)) return null;
            for (int i = 0; i < definitions.Count; i++)
            {
                PowerupDefinitionSO p = definitions[i];
                if (p != null && p.Code == code) return p;
            }
            return null;
        }

        // Update the catalog when creating balance-data assets in the Editor.
        public void Configure(IEnumerable<PowerupDefinitionSO> values)
        {
            definitions = values == null ? new List<PowerupDefinitionSO>() : new List<PowerupDefinitionSO>(values);
        }
    }
}
