using System;

[Serializable]
public class ItemResponse
{
    public bool success;
    public string message;
    public ItemData[] data;
}
