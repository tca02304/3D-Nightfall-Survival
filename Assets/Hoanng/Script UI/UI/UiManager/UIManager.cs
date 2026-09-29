using UnityEngine.UIElements;
using UnityEngine;
using System;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }
    public UiBase uiSetting;
    private void Awake()
    {
        DontDestroyOnLoad(gameObject);

    }
    
    public void ShowSetting()
    {
        uiSetting.Show();
    }
    public void HideSetting() 
    {
        uiSetting.Hide();
    }
    
}
