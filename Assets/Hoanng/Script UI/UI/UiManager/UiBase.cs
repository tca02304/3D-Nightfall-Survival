using UnityEngine;

public abstract class UiBase : MonoBehaviour
{
    public virtual void Show()
    { 
        gameObject.SetActive(true); 
    }
    public virtual void Hide()
    {
        gameObject.SetActive(false);
    }
}
