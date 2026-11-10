using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

public class InventoryAPI : MonoBehaviour
{
    [Header("Backend Settings")]
    [SerializeField]
    private string apiUrl =
        "https://localhost:7133/api/Inventory";

    [SerializeField] private int playerId = 1;

    [Header("Load Settings")]
    [SerializeField] private bool loadOnStart = true;

    [Header("Auto Save")]
    [SerializeField] private float saveDelay = 0.5f;

    private bool isLoaded;
    private bool isApplyingLoad;
    private bool isSubscribed;

    private long changeVersion;
    private long savedVersion;

    private Coroutine saveRoutine;

    private IEnumerator Start()
    {
        if (!loadOnStart)
        {
            Debug.LogWarning("[InventoryAPI] Auto Save requires initial load");
            yield break;
        }

        while (ItemDatabase.Instance == null ||
            InventoryManager.Instance == null)
        {
            yield return null;
        }

        yield return StartCoroutine(GetInventory());

        if (isLoaded)
        {
            Subscribe();
            Debug.Log("[InventoryAPI] Auto Save enabled");
        }
    }

    private void Subscribe()
    {
        if (isSubscribed || InventoryManager.Instance == null)
            return;

        InventoryManager.Instance.OnInventoryChanged += HandleInventoryChanged;

        isSubscribed = true;
    }

    private void OnDestroy()
    {
        if(isSubscribed && InventoryManager.Instance != null)
        {
            InventoryManager.Instance.OnInventoryChanged -= HandleInventoryChanged;
        }

        isSubscribed = false;
    }

    private void HandleInventoryChanged()
    {
        if (!isLoaded || isApplyingLoad)
            return;

        changeVersion++;

        if(saveRoutine == null)
        {
            saveRoutine = StartCoroutine(AutoSaveRoutine());
        }
    }

    private IEnumerator AutoSaveRoutine()
    {
        InventoryManager manager = InventoryManager.Instance;

        while(savedVersion < changeVersion)
        {
            if (manager == null)
                break;

            if (!manager.CursorSlot.isEmpty())
            {
                yield return null;
                continue;
            }

            long versionToSave = changeVersion;

            yield return new WaitForSecondsRealtime(Mathf.Max(0.1f, saveDelay));

            if(changeVersion != versionToSave ||
                !manager.CursorSlot.isEmpty())
            {
                continue;
            }

            bool success = false;

            yield return StartCoroutine(
                SendInventorySnapshot(result => success = result));

            if (!success)
            {
                Debug.LogError("[InventoryAPI] Auto Save failed" +
                    "Changes remain unsaved");
                break;
            }

            savedVersion = versionToSave;

            Debug.Log($"[InventoryAPI] Save version {savedVersion}");
        }

        saveRoutine = null;
    }

    [ContextMenu("Test/Load Inventory From Backend")]
    public void TestLoadInventory()
    {
        StartCoroutine(GetInventory());
    }

    public IEnumerator GetInventory()
    {
        if (saveRoutine != null)
        {
            Debug.LogWarning(
                "[InventoryAPI] Cannot reload while save is pending");
            yield break;
        }

        string url = $"{apiUrl}/{playerId}";

        using UnityWebRequest request =
            UnityWebRequest.Get(url);

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError(
                "[InventoryAPI] Load failed: " +
                request.error);
            yield break;
        }

        InventoryResponse response;

        try
        {
            response = JsonUtility.FromJson<InventoryResponse>(
                request.downloadHandler.text);
        }
        catch (Exception ex)
        {
            Debug.LogError(
                "[InventoryAPI] JSON Error: " + ex.Message);
            yield break;
        }

        if (response == null || !response.success)
        {
            Debug.LogError(
                "[InventoryAPI] Invalid response");
            yield break;
        }

        if (response.data == null)
            response.data = Array.Empty<InventorySlotResponse>();

        if (InventoryManager.Instance == null)
            yield break;

        isApplyingLoad = true;

        bool loaded = false;

        try
        {
            loaded = InventoryResponseAdapter.Apply(
                InventoryManager.Instance,
                response.data);
        }
        finally
        {
            isApplyingLoad = false;
        }

        if (!loaded)
        {
            Debug.LogError(
                "[InventoryAPI] Failed to apply inventory");
            yield break;
        }

        isLoaded = true;

        Debug.Log(
            $"[InventoryAPI] Loaded {response.data.Length} slots " +
            $"for player {playerId}");

        Subscribe();
    }

    [ContextMenu("Test/Save Inventory To Backend")]
    public void SaveNow()
    {
        if (!isLoaded)
        {
            Debug.LogWarning(
                "[InventoryAPI] Inventory has not loaded yet");
            return;
        }

        // Đưa yêu cầu Save vào cùng hàng đợi Auto Save
        HandleInventoryChanged();
    }

    public IEnumerator SendInventorySnapshot(Action<bool> onComplete)
    {
        InventoryManager manager = InventoryManager.Instance;

        if(manager == null || !manager.CursorSlot.isEmpty())
        {
            onComplete?.Invoke(false);
            yield break;
        }

        List<SaveInventorySlot> slots = new();

        for (int i = 0; i < manager.Slots.Count; i++)
        {
            InventorySlotData slot = manager.Slots[i];

            if (slot.isEmpty())
                continue;

            slots.Add(new SaveInventorySlot
            {
                slotIndex = i,
                itemId = slot.ItemId,
                amount = slot.Amount
            });
        }

        SaveInventoryRequest body = new()
        {
            slots = slots.ToArray()
        };

        string json = JsonUtility.ToJson(body);
        string url = $"{apiUrl}/{playerId}/save-test";

        byte[] bytes = Encoding.UTF8.GetBytes(json);

        using UnityWebRequest request = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST);

        request.uploadHandler = new UploadHandlerRaw(bytes);
        request.downloadHandler = new DownloadHandlerBuffer();

        request.uploadHandler.contentType = "application/json";
        request.SetRequestHeader("Accept", "application/json");

        yield return request.SendWebRequest();

        if(request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError(
                "[InventoryAPI] Save failed: " +
                request.responseCode + " | " +
                request.downloadHandler.text);

            onComplete?.Invoke(false);
            yield break;
        }

        Debug.Log("[InventoryAPI] Save success");

        onComplete?.Invoke(true);
    }
}