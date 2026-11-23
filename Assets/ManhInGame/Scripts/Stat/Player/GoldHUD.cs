using TMPro;
using UnityEngine;

namespace Nightfall.Stats
{
    public sealed class GoldHUD : MonoBehaviour
    {
        [SerializeField] private GoldWallet wallet;
        [SerializeField] private TMP_Text label;

        // Find the wallet from the Player hierarchy and refresh when HUD becomes visible.
        private void OnEnable()
        {
            if(wallet==null) wallet=GetComponentInParent<GoldWallet>();
            if(label==null) label=GetComponent<TMP_Text>();
            if(wallet!=null) { wallet.GoldChanged+=Render; Render(wallet.CurrentGold); }
        }

        // Release the HUD subscription when disabled.
        private void OnDisable() { if(wallet!=null) wallet.GoldChanged-=Render; }

        // Display the current wallet, never a second copy of gold state.
        private void Render(int amount) { if(label!=null) label.text=amount.ToString("N0"); }
    }
}
