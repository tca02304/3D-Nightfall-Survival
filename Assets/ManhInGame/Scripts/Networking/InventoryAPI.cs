using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public class InventoryAPI : MonoBehaviour
{
    [Header("Backend Settings")]
    [SerializeField]
    private string apiUrl =
        "https://localhost:7133/api/Inventory";

    [Header("Test Player")]
    [SerializeField] private int playerId = 1;

    [Header("Load Settings")]
    [SerializeField] private bool loadOnStart = true;

    private IEnumerator Start()
    {
        if (!loadOnStart)
            yield break;

        while (ItemDatabase.Instance == null ||
            InventoryManager.Instance == null)
        {
            yield return null;
        }

        yield return StartCoroutine(GetInventory());
    }

    [ContextMenu("Test/Load Inventory From Backend")]
    public void TestLoadInventory()
    {
        StartCoroutine(GetInventory());
    }

    public IEnumerator GetInventory()
    {
        string url = $"{apiUrl}/{playerId}";

        using UnityWebRequest request =
            UnityWebRequest.Get(url);

        yield return request.SendWebRequest();

        if(request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError(
                "[InventoryAPI] API Error: " +
                request.error + " | " + url);

            yield break;
        }

        InventoryResponse response;

        try
        {
            response = JsonUtility.FromJson<InventoryResponse>(
                request.downloadHandler.text);
        }
        catch(System.Exception ex)
        {
            Debug.LogError(
                "[InventotyAPI] JSON Error: " + ex.Message);

            yield break;
        }

        if(response == null || !response.success)
        {
            Debug.LogError(
                "[InventoryAPI] Invalid response: " +
                request.downloadHandler.text);

            yield break;
        }

        if(response.data == null)
        {
            response.data = Array.Empty<InventorySlotResponse>();
        }

        if(InventoryManager.Instance == null)
        {
            Debug.LogError(
                "[InventoryAPI] InventoryManager not found");

            yield break;
        }

        bool loaded = InventoryManager.Instance.LoadInventory(response.data);

        if (!loaded)
        {
            Debug.LogError("[InventoryAPI] Failed to apply inventory");
            yield break;
        }

        Debug.Log(
            $"[InventoryAPI] Loaded {response.data.Length} slots " +
            $"for player {playerId}");

        foreach(InventorySlotResponse slot in response.data)
        {
            Debug.Log(
                $"Slot: {slot.slotIndex} |" +
                $"ItemId: {slot.itemId} |" +
                $"Amount: {slot.amount}");
        }
    }
}
