using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
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

    [Header("Dependencies")]
    [SerializeField] private ItemAPI itemAPI;

    [SerializeField] private GridCraftingController craftingController;

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
            Debug.LogWarning("[InventoryAPI] Load On Start disabled");
            yield break;
        }

        while (InventoryManager.Instance == null ||
               ItemDatabase.Instance == null)
        {
            yield return null;
        }

        if (itemAPI == null)
            itemAPI = GetComponent<ItemAPI>();

        if (itemAPI == null)
        {
            Debug.LogError("[InventoryAPI] ItemAPI not found");
            yield break;
        }

        Debug.Log("[InventoryAPI] Waiting for ItemAPI...");

        while (!itemAPI.IsLoaded)
        {
            yield return null;
        }

        Debug.Log("[InventoryAPI] ItemAPI ready. Loading inventory...");

        yield return StartCoroutine(GetInventory());

        if (isLoaded)
        {
            Subscribe();
            Debug.Log("[InventoryAPI] Auto Save enabled");
        }
    }

    private void Subscribe()
    {
        InventoryManager.Instance.OnInventoryChanged += HandleInventoryChanged;

        // [ADD] Thay đổi Crafting cũng kích hoạt Auto Save
        if (craftingController != null)
        {
            craftingController.OnGridChanged += HandleInventoryChanged;
        }

        isSubscribed = true;
    }

    private void OnDestroy()
    {
        if(isSubscribed && InventoryManager.Instance != null)
        {
            InventoryManager.Instance.OnInventoryChanged -= HandleInventoryChanged;
        }

        if (isSubscribed && craftingController != null)
        {
            craftingController.OnGridChanged -= HandleInventoryChanged;
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

            long versionToSave = changeVersion;

            yield return new WaitForSecondsRealtime(Mathf.Max(0.1f, saveDelay));

            if (changeVersion != versionToSave)
            {
                continue;
            }

            bool success = false;

            yield return StartCoroutine(
                SendInventorySnapshot(result => success = result));

            if (!success)
            {
                Debug.LogError(
        "[InventoryAPI] Auto Save failed. Retrying...");

                yield return new WaitForSecondsRealtime(2f);
                continue;
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

        if (itemAPI == null)
        {
            itemAPI = GetComponent<ItemAPI>();
        }

        if (itemAPI == null)
        {
            Debug.LogError("[InventoryAPI] ItemAPI reference is missing.");
            yield break;
        }

        if (!itemAPI.IsLoaded)
        {
            Debug.LogWarning(
                "[InventoryAPI] Cannot load Inventory: ItemAPI is not ready."
            );
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

        if (response.cursorSlot != null &&
            response.cursorSlot.itemId == 0 &&
            response.cursorSlot.amount == 0)
        {
            response.cursorSlot = null;
        }

        // [DEBUG] Xem JSON thuc te Backend tra ve
        Debug.Log(
            "[InventoryAPI] Backend response: " +
            request.downloadHandler.text
        );

        Debug.Log(
            $"[InventoryAPI] Snapshot counts: " +
            $"Inventory={response.data?.Length ?? -1}, " +
            $"Crafting={response.craftingSlots?.Length ?? -1}, " +
            $"Cursor={(response.cursorSlot == null ? "Empty" : "Occupied")}"
        );

        if (InventoryManager.Instance == null)
            yield break;

        // [ADD] Backend mới bắt buộc trả craftingSlots
        if (response.craftingSlots == null ||
            craftingController == null)
        {
            Debug.LogError(
                "[InventoryAPI] Missing Crafting snapshot or controller");
            yield break;
        }

        InventoryManager manager = InventoryManager.Instance;

        // [ADD] Kiểm tra dữ liệu trước khi Restore

        bool inventoryValid = ValidateSlots(
            response.data,
            manager.Slots.Count,
            manager
        );

        bool craftingValid =
            response.craftingSlots != null &&
            ValidateSlots(
                response.craftingSlots,
                9,
                manager
            );
        // [DEBUG] Kiem tra Cursor tu Backend
        if (response.cursorSlot == null)
        {
            Debug.Log("[InventoryAPI] Backend Cursor: NULL");
        }
        else
        {
            var cursor = response.cursorSlot;
            ItemData item = manager.GetItemData(cursor.itemId);

            Debug.Log(
                $"[InventoryAPI] Backend Cursor: " +
                $"SlotIndex={cursor.slotIndex}, " +
                $"ItemId={cursor.itemId}, " +
                $"Amount={cursor.amount}, " +
                $"ItemFound={item != null}, " +
                $"MaxStack={(item != null ? item.maxStack : -1)}"
            );
        }

        bool cursorValid = ValidateCursor(
            response.cursorSlot,
            manager
        );

        if (!inventoryValid || !craftingValid || !cursorValid)
        {
            Debug.LogError(
                "[InventoryAPI] Invalid full snapshot: " +
                $"Inventory={inventoryValid}, " +
                $"Crafting={craftingValid}, " +
                $"Cursor={cursorValid}"
            );

            yield break;
        }


        if (!manager.CursorSlot.isEmpty())
        {
            Debug.LogWarning(
                "[InventoryAPI] Put cursor item away before reloading");
            yield break;
        }

        isApplyingLoad = true;

        bool loaded = false;

        try
        {
            loaded = InventoryResponseAdapter.Apply(manager, response.data);

            // [ADD] Restore 9 ô Crafting
            if (loaded)
            {
                loaded = craftingController.TryLoadGridFromResponse(
                    response.craftingSlots);
            }

            // [ADD] Restore Cursor
            if (loaded)
            {
                RestoreCursor(response.cursorSlot, manager);
            }
        }
        finally
        {
            isApplyingLoad = false;
        }

        if (!loaded)
        {
            Debug.LogError(
                $"[InventoryAPI] Failed to apply inventory. " +
                $"PlayerId={playerId}, " +
                $"ReceivedSlots={response.data.Length}, " +
                $"ItemAPIReady={(itemAPI != null && itemAPI.IsLoaded)}"
            );

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

        // [MODIFY] Phải có cả Inventory và Crafting
        if (manager == null || craftingController == null)
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


        // [ADD] Lấy bản sao 9 ô Crafting
        GridIngredient[] gridSnapshot =
            craftingController.CreateSnapshot();

        List<SaveInventorySlot> craftingSlots = new();

        for (int i = 0; i < gridSnapshot.Length; i++)
        {
            GridIngredient cell = gridSnapshot[i];

            if (cell.itemId <= 0 || cell.amount <= 0)
                continue;

            craftingSlots.Add(new SaveInventorySlot
            {
                slotIndex = i,
                itemId = cell.itemId,
                amount = cell.amount
            });
        }

        // [ADD] Lưu cả Item đang cầm trên Cursor
        InventorySlotData cursor = manager.CursorSlot;

        SaveInventorySlot cursorRecord = cursor.isEmpty()
            ? null
            : new SaveInventorySlot
            {
                slotIndex = 0,
                itemId = cursor.ItemId,
                amount = cursor.Amount
            };



        // [MODIFY] Khong serialize Cursor neu dang trong.
        string json;

        if (cursorRecord == null)
        {
            // Cursor trong: khong gui truong cursorSlot.
            var body = new SaveInventoryRequestWithoutCursor
            {
                slots = slots.ToArray(),
                craftingSlots = craftingSlots.ToArray()
            };

            json = JsonUtility.ToJson(body);
        }
        else
        {
            // Cursor co Item: gui du thong tin.
            var body = new SaveInventoryRequest
            {
                slots = slots.ToArray(),
                craftingSlots = craftingSlots.ToArray(),
                cursorSlot = cursorRecord
            };

            json = JsonUtility.ToJson(body);
        }

        // [DEBUG] Kiem tra JSON truoc khi gui.
        Debug.Log("[InventoryAPI] Save JSON: " + json);

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


    private static bool ValidateSnapshot(
        InventoryResponse response,
        InventoryManager manager)
    {
        if (response == null ||
            manager == null ||
            response.data == null ||
            response.craftingSlots == null)
            return false;

        return ValidateSlots(
                   response.data, manager.Slots.Count, manager) &&
               ValidateSlots(
                   response.craftingSlots, 9, manager) &&
               ValidateCursor(response.cursorSlot, manager);
    }

    private static bool ValidateSlots(
        InventorySlotResponse[] records,
        int slotCount,
        InventoryManager manager)
    {
        HashSet<int> used = new();

        foreach (InventorySlotResponse record in records)
        {
            if (record == null ||
                record.slotIndex < 0 ||
                record.slotIndex >= slotCount ||
                !used.Add(record.slotIndex))
                return false;

            ItemData item = manager.GetItemData(record.itemId);

            if (item == null ||
                record.amount <= 0 ||
                record.amount > item.maxStack)
                return false;
        }

        return true;
    }

    private static bool ValidateCursor(
        InventorySlotResponse record,
        InventoryManager manager)
    {
        if (record == null)
            return true;

        ItemData item = manager.GetItemData(record.itemId);

        return record.slotIndex == 0 &&
               item != null &&
               record.amount > 0 &&
               record.amount <= item.maxStack;
    }

    private static void RestoreCursor(
        InventorySlotResponse record,
        InventoryManager manager)
    {
        InventorySlotData cursor = manager.CursorSlot;

        cursor.Clear();

        if (record != null)
        {
            ItemData item = manager.GetItemData(record.itemId);

            cursor.ItemId = item.id;
            cursor.ItemCode = item.itemCode;
            cursor.ItemName = item.name;
            cursor.MaxStack = item.maxStack;
            cursor.Amount = record.amount;
        }

        manager.NotifyChanged();
    }

}