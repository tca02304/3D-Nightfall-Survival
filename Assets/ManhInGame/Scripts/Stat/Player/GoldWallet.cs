using System;
using UnityEngine;

namespace Nightfall.Stats
{
    [DisallowMultipleComponent]
    public sealed class GoldWallet : MonoBehaviour
    {
        [SerializeField,Min(0)] private int gold;
        public int CurrentGold => gold;
        public event Action<int> GoldChanged;

        // Check whether a nonnegative price is affordable; zero is allowed for the basic chest.
        public bool CanSpend(int amount) => amount>=0 && gold>=amount;

        // Charge the exact amount or change nothing when funds are insufficient.
        public bool TrySpendGold(int amount)
        {
            if(!CanSpend(amount)) return false;
            if(amount==0) return true;
            gold-=amount; GoldChanged?.Invoke(gold); return true;
        }

        // Credit earned gold with saturating overflow protection.
        public bool TryAddGold(int amount)
        {
            if(amount<=0) return false;
            gold=(int)Math.Min(int.MaxValue,(long)gold+amount);
            GoldChanged?.Invoke(gold); return true;
        }

        // Restore a validated wallet balance for future unified run saves.
        public bool TryRestore(int amount)
        {
            if(amount<0) return false;
            gold=amount; GoldChanged?.Invoke(gold); return true;
        }
    }
}
