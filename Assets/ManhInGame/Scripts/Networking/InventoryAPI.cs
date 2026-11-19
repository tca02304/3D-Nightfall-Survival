using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

// Keep the legacy InventoryAPI component and public methods, while selecting local or server persistence.
public sealed class InventoryAPI : MonoBehaviour
{
    public enum PersistenceMode { Local, BackendDevelopment }

    [Header("Persistence Mode")]
    [SerializeField] private PersistenceMode mode = PersistenceMode.Local;
    [SerializeField, Min(1)] private int playerId = 1;
    [SerializeField] private bool loadOnStart = true;
    [SerializeField, Min(0.1f)] private float saveDelay = 0.5f;

    [Header("Backend Development Only")]
    [SerializeField] private string apiUrl = "https://localhost:7133/api/Inventory";
    [SerializeField, Min(3)] private int timeoutSeconds = 10;

    [Header("Existing Crafting Manager")]
    [SerializeField] private GridCraftingController craftingController;

    private bool isLoaded;
    private bool isApplyingLoad;
    private bool isSubscribed;
    private bool isSaving;
    private long changeVersion;
    private long savedVersion;
    private Coroutine saveRoutine;

    public bool IsLoaded => isLoaded;
    public PersistenceMode Mode => mode;
    private string SavePath => Path.Combine(Application.persistentDataPath,
        $"nightfall_player_{playerId}_inventory_v1.json");

    // Wait for local item definitions, then initialize selected persistence safely.
    private IEnumerator Start()
    {
        while (InventoryManager.Instance == null || ItemDatabase.Instance == null)
            yield return null;

        if (!ItemDatabase.Instance.IsReady)
        {
            Debug.LogError("[InventoryAPI] ItemCatalog is invalid or not assigned.", this);
            yield break;
        }

        if (craftingController == null)
            craftingController = GetComponent<GridCraftingController>();

        if (craftingController == null)
        {
            Debug.LogError("[InventoryAPI] Assign GridCraftingController on Managers.", this);
            yield break;
        }

#if !UNITY_EDITOR && !DEVELOPMENT_BUILD
        if (mode == PersistenceMode.BackendDevelopment)
        {
            Debug.LogError("[InventoryAPI] BackendDevelopment is disabled in release builds. Use Local or implement authenticated production persistence.", this);
            yield break;
        }
#endif
        if (loadOnStart)
        {
            yield return GetInventory();
        }
        else
        {
            isLoaded = true;
            Subscribe();
            Debug.LogWarning("[InventoryAPI] Starting with empty runtime state; existing save not loaded.", this);
        }
    }

    // Subscribe once to inventory and crafting changes for debounced persistence.
    private void Subscribe()
    {
        if (isSubscribed) return;
        InventoryManager.Instance.OnInventoryChanged += HandleInventoryChanged;
        craftingController.OnGridChanged += HandleInventoryChanged;
        isSubscribed = true;
    }

    // Unsubscribe and attempt final local save if state changed.
    private void OnDestroy()
    {
        if (isSubscribed && InventoryManager.Instance != null)
            InventoryManager.Instance.OnInventoryChanged -= HandleInventoryChanged;
        if (isSubscribed && craftingController != null)
            craftingController.OnGridChanged -= HandleInventoryChanged;
        isSubscribed = false;
        if (mode == PersistenceMode.Local && isLoaded && changeVersion > savedVersion)
            SaveLocalSnapshot();
    }

    // Mark the snapshot dirty when gameplay changes inventory or crafting.
    private void HandleInventoryChanged()
    {
        if (!isLoaded || isApplyingLoad) return;
        changeVersion++;
        if (saveRoutine == null)
            saveRoutine = StartCoroutine(AutoSaveRoutine());
    }

    // Save latest stable state; retry failures without discarding dirty data.
    private IEnumerator AutoSaveRoutine()
    {
        while (savedVersion < changeVersion)
        {
            long targetVersion = changeVersion;
            yield return new WaitForSecondsRealtime(Mathf.Max(0.1f, saveDelay));
            if (targetVersion != changeVersion) continue;
            bool ok = false;
            yield return SendInventorySnapshot(result => ok = result);
            if (ok) savedVersion = targetVersion;
            else yield return new WaitForSecondsRealtime(2f);
        }
        saveRoutine = null;
    }

    // Request loading the selected save source from Unity's component context menu.
    [ContextMenu("Test/Load Inventory From Selected Storage")]
    public void TestLoadInventory() { StartCoroutine(GetInventory()); }

    // Read either a local JSON snapshot or the development backend snapshot.
    public IEnumerator GetInventory()
    {
        if (isSaving || (isLoaded && savedVersion < changeVersion))
        {
            Debug.LogWarning("[InventoryAPI] Reload blocked while unsaved changes exist.", this);
            yield break;
        }

        InventoryResponse response = null;
        if (mode == PersistenceMode.Local)
        {
            if (!File.Exists(SavePath))
            {
                isLoaded = true;
                Subscribe();
                Debug.Log("[InventoryAPI] No local save; starting with empty slots.", this);
                yield break;
            }
            try { response = JsonUtility.FromJson<InventoryResponse>(File.ReadAllText(SavePath)); }
            catch (Exception e)
            {
                Debug.LogError("[InventoryAPI] Local save corrupt or unreadable: " + e.Message, this);
                yield break;
            }
        }
        else
        {
            using (UnityWebRequest request = UnityWebRequest.Get($"{apiUrl.TrimEnd('/')}/{playerId}"))
            {
                request.timeout = timeoutSeconds;
                yield return request.SendWebRequest();
                if (request.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError("[InventoryAPI] Backend load failed: " + request.error, this);
                    yield break;
                }
                try { response = JsonUtility.FromJson<InventoryResponse>(request.downloadHandler.text); }
                catch (Exception e)
                {
                    Debug.LogError("[InventoryAPI] Invalid backend JSON: " + e.Message, this);
                    yield break;
                }
            }
        }

        if (response != null && response.cursorSlot != null &&
            response.cursorSlot.itemId == 0 && response.cursorSlot.amount == 0)
            response.cursorSlot = null;

        if (!ValidateSnapshot(response))
        {
            Debug.LogError("[InventoryAPI] Rejected invalid snapshot; runtime state unchanged.", this);
            yield break;
        }
        if (!ApplySnapshot(response))
        {
            Debug.LogError("[InventoryAPI] Snapshot restore failed.", this);
            yield break;
        }
        isLoaded = true;
        changeVersion = savedVersion = 0;
        Subscribe();
        Debug.Log($"[InventoryAPI] Loaded snapshot ({mode}) for player {playerId}.", this);
    }

    // Mark current state for save through the existing component context menu.
    [ContextMenu("Test/Save Inventory To Selected Storage")]
    public void SaveNow()
    {
        if (!isLoaded) { Debug.LogWarning("[InventoryAPI] Not loaded yet.", this); return; }
        HandleInventoryChanged();
    }

    // Persist inventory, crafting grid, and cursor using the selected adapter.
    public IEnumerator SendInventorySnapshot(Action<bool> onComplete)
    {
        if (!isLoaded || isSaving)
        {
            onComplete?.Invoke(false);
            yield break;
        }
        isSaving = true;
        try
        {
            if (mode == PersistenceMode.Local)
            {
                onComplete?.Invoke(SaveLocalSnapshot());
                yield break;
            }
            string json = BuildSaveRequestJson();
            if (json == null) { onComplete?.Invoke(false); yield break; }
            byte[] payload = Encoding.UTF8.GetBytes(json);
            using (UnityWebRequest request = new UnityWebRequest(
                $"{apiUrl.TrimEnd('/')}/{playerId}/save-test", UnityWebRequest.kHttpVerbPOST))
            {
                request.uploadHandler = new UploadHandlerRaw(payload);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");
                request.timeout = timeoutSeconds;
                yield return request.SendWebRequest();
                bool ok = request.result == UnityWebRequest.Result.Success;
                if (!ok) Debug.LogError($"[InventoryAPI] Backend save failed ({request.responseCode}): {request.error}; {request.downloadHandler.text}", this);
                onComplete?.Invoke(ok);
            }
        }
        finally { isSaving = false; }
    }

    // Build one normalized snapshot from inventory, crafting, and cursor data.
    private InventoryResponse CaptureSnapshot()
    {
        InventoryManager inventory = InventoryManager.Instance;
        if (inventory == null || craftingController == null) return null;
        var slots = new List<InventorySlotResponse>();
        for (int i = 0; i < inventory.Slots.Count; i++)
        {
            InventorySlotData slot = inventory.Slots[i];
            if (slot != null && !slot.isEmpty())
                slots.Add(new InventorySlotResponse { slotIndex = i, itemId = slot.ItemId, amount = slot.Amount });
        }
        var crafting = new List<InventorySlotResponse>();
        GridIngredient[] cells = craftingController.CreateSnapshot();
        for (int i = 0; i < cells.Length; i++)
            if (cells[i].itemId > 0 && cells[i].amount > 0)
                crafting.Add(new InventorySlotResponse { slotIndex = i, itemId = cells[i].itemId, amount = cells[i].amount });
        var cursor = inventory.CursorSlot;
        return new InventoryResponse
        {
            success = true,
            message = "Local inventory snapshot v1",
            data = slots.ToArray(),
            craftingSlots = crafting.ToArray(),
            cursorSlot = cursor.isEmpty() ? null : new InventorySlotResponse { slotIndex = 0, itemId = cursor.ItemId, amount = cursor.Amount }
        };
    }

    // Serialize the server's existing SaveInventoryRequest DTO without null CursorSlot.
    private string BuildSaveRequestJson()
    {
        InventoryResponse snapshot = CaptureSnapshot();
        if (snapshot == null || !ValidateSnapshot(snapshot)) return null;
        var slots = ConvertRecords(snapshot.data);
        var crafting = ConvertRecords(snapshot.craftingSlots);
        if (snapshot.cursorSlot == null)
            return JsonUtility.ToJson(new SaveInventoryRequestWithoutCursor { slots = slots, craftingSlots = crafting });
        return JsonUtility.ToJson(new SaveInventoryRequest
        {
            slots = slots,
            craftingSlots = crafting,
            cursorSlot = new SaveInventorySlot { slotIndex = 0, itemId = snapshot.cursorSlot.itemId, amount = snapshot.cursorSlot.amount }
        });
    }

    // Convert a neutral slot array to the legacy backend request format.
    private static SaveInventorySlot[] ConvertRecords(InventorySlotResponse[] records)
    {
        var result = new SaveInventorySlot[records.Length];
        for (int i = 0; i < records.Length; i++)
            result[i] = new SaveInventorySlot { slotIndex = records[i].slotIndex, itemId = records[i].itemId, amount = records[i].amount };
        return result;
    }

    // Atomically replace the local save file and retain the previous file as backup.
    private bool SaveLocalSnapshot()
    {
        InventoryResponse snapshot = CaptureSnapshot();
        if (snapshot == null || !ValidateSnapshot(snapshot))
        {
            Debug.LogError("[InventoryAPI] Refused to save invalid local snapshot.", this);
            return false;
        }
        string tempPath = SavePath + ".tmp";
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(SavePath));
            File.WriteAllText(tempPath, JsonUtility.ToJson(snapshot, true));
            if (File.Exists(SavePath))
                File.Replace(tempPath, SavePath, SavePath + ".bak");
            else
                File.Move(tempPath, SavePath);
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError("[InventoryAPI] Local save failed: " + e.Message, this);
            return false;
        }
    }

    // Validate all three areas against the local catalog before mutating runtime state.
    private bool ValidateSnapshot(InventoryResponse snapshot)
    {
        InventoryManager inv = InventoryManager.Instance;
        if (snapshot == null || !snapshot.success || inv == null || craftingController == null ||
            snapshot.data == null || snapshot.craftingSlots == null) return false;
        return ValidateRecords(snapshot.data, inv.Slots.Count, inv) &&
               ValidateRecords(snapshot.craftingSlots, 9, inv) &&
               (snapshot.cursorSlot == null ||
                (snapshot.cursorSlot.slotIndex == 0 && ValidateRecord(snapshot.cursorSlot, inv)));
    }

    // Reject duplicate indices, invalid item IDs, and stacks above the SO max.
    private static bool ValidateRecords(InventorySlotResponse[] records, int slotCount, InventoryManager inv)
    {
        var used = new HashSet<int>();
        foreach (InventorySlotResponse slot in records)
            if (slot == null || slot.slotIndex < 0 || slot.slotIndex >= slotCount ||
                !used.Add(slot.slotIndex) || !ValidateRecord(slot, inv)) return false;
        return true;
    }

    // Check one slot's ID and amount against ItemDatabase's local definitions.
    private static bool ValidateRecord(InventorySlotResponse slot, InventoryManager inv)
    {
        ItemData item = inv.GetItemData(slot.itemId);
        return item != null && item.isActive && slot.amount > 0 && slot.amount <= item.maxStack;
    }

    // Restore the three validated areas; inventory and crafting are committed together.
    private bool ApplySnapshot(InventoryResponse snapshot)
    {
        InventoryManager inv = InventoryManager.Instance;
        if (!inv.CursorSlot.isEmpty())
        {
            Debug.LogWarning("[InventoryAPI] Clear cursor before loading another save.", this);
            return false;
        }
        var records = new List<InventorySlotRecord>(snapshot.data.Length);
        foreach (InventorySlotResponse slot in snapshot.data)
            records.Add(new InventorySlotRecord(slot.slotIndex, slot.itemId, slot.amount));
        isApplyingLoad = true;
        try
        {
            if (!craftingController.TryLoadGridFromResponse(snapshot.craftingSlots)) return false;
            if (!inv.TryLoadSlots(records)) return false;
            inv.CursorSlot.Clear();
            if (snapshot.cursorSlot != null)
            {
                ItemData item = inv.GetItemData(snapshot.cursorSlot.itemId);
                inv.CursorSlot.ItemId = item.id;
                inv.CursorSlot.ItemCode = item.itemCode;
                inv.CursorSlot.ItemName = item.name;
                inv.CursorSlot.MaxStack = item.maxStack;
                inv.CursorSlot.Amount = snapshot.cursorSlot.amount;
            }
            inv.NotifyChanged();
            return true;
        }
        finally { isApplyingLoad = false; }
    }
}
