using System;
using UnityEngine;
using UnityEngine.Events;

namespace Nightfall.Stats
{
    [Serializable]
    public sealed class StatValueBinding
    {
        public StatType stat;
        public UnityEvent<float> assignValue=new UnityEvent<float>();
    }

    [DisallowMultipleComponent]
    public sealed class PlayerStatBridge : MonoBehaviour
    {
        [SerializeField] private PlayerStatProvider provider;
        [SerializeField] private StatValueBinding[] bindings=Array.Empty<StatValueBinding>();
        [SerializeField] private UnityEvent<float> healRequested=new UnityEvent<float>();
        [SerializeField] private bool forwardHealthRegeneration;
        private float regenElapsed;
        private float[] last;
        private bool[] initialized;
        private bool sending;

        // Locate the existing data provider; the bridge itself never creates HP or movement state.
        private void Awake() { if(provider==null) provider=GetComponent<PlayerStatProvider>(); }

        // Listen for data changes and apply each configured absolute setter once on enable.
        private void OnEnable()
        {
            if(provider==null) return;
            regenElapsed=0f; last=new float[bindings.Length]; initialized=new bool[bindings.Length];
            provider.StatsChanged+=Refresh; provider.HealRequested+=OnHealRequested; Refresh();
        }

        // Remove subscriptions so scene loads and panel open/close cannot multiply stat bonuses.
        private void OnDisable()
        {
            if(provider==null) return;
            provider.StatsChanged-=Refresh; provider.HealRequested-=OnHealRequested;
        }

        // Push only changed absolute values into the teammate's public setter methods through dynamic float UnityEvents.
        public void Refresh()
        {
            if(provider==null || sending || last==null) return;
            sending=true;
            try
            {
                for(int i=0;i<bindings.Length && i<last.Length;i++)
                {
                    var b=bindings[i]; if(b==null) continue;
                    float value=provider.GetStat(b.stat);
                    if(initialized[i] && Mathf.Approximately(value,last[i])) continue;
                    last[i]=value; initialized[i]=true; b.assignValue?.Invoke(value);
                }
            }
            finally { sending=false; }
        }

        // Optionally apply regeneration to the existing health system once per scaled gameplay second.
        private void Update()
        {
            if(!forwardHealthRegeneration || provider==null) return;
            regenElapsed+=Time.deltaTime;
            if(regenElapsed<1f) return;
            float amount=provider.GetStat(StatType.HealthRegen)*regenElapsed;
            regenElapsed=0f;
            if(amount>0f) healRequested?.Invoke(amount);
        }

        // Forward lifesteal to the real player's heal method rather than storing separate health.
        private void OnHealRequested(float amount) => healRequested?.Invoke(amount);
    }
}
