using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Put on the child Icon image of an ingredient or on ResultProductIcon.
/// Works with the EXISTING Inventory ItemSlot.OnDrop via a small bridge patch.
/// </summary>
[RequireComponent(typeof(Image))]
[RequireComponent(typeof(CanvasGroup))]
public sealed class GridCraftingDragSource : MonoBehaviour,
    IBeginDragHandler, IDragHandler, IEndDragHandler, IInitializePotentialDragHandler
{
    private GridCraftingController controller;
    private int cellIndex;
    private bool isResult;
    private bool dragging;
    private Canvas canvas;
    private CanvasGroup canvasGroup;
    private RectTransform ghostRect;
    private GameObject ghost;

    public int CellIndex => cellIndex;
    public bool IsResult => isResult;

    public void Configure(GridCraftingController target, int index, bool result)
    {
        controller = target;
        cellIndex = index;
        isResult = result;
        GetComponent<Image>().raycastTarget = true;
    }

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        canvas = GetComponentInParent<Canvas>();
    }

    public void OnInitializePotentialDrag(PointerEventData eventData)
    {
        eventData.useDragThreshold = false;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left || controller == null)
            return;
        if (isResult)
        {
            if (!controller.CanCraft(out string reason)) return;
        }
        else if (controller.GetCell(cellIndex).itemId <= 0) return;

        var image = GetComponent<Image>();
        if(image.sprite == null || !image.enabled)
        {
            Debug.LogWarning($"[GridCraftingDragSource] Missing visible icon at {(isResult ? "Result" : $"Cell {cellIndex}")}", this);
            return;
        }

        if (canvas == null) canvas = GetComponentInParent<Canvas>();
        Canvas rootCanvas = canvas != null ? canvas.rootCanvas : null;
        if (rootCanvas == null) return;

        int amount = isResult ? controller.MatchedRecipe.outputAmount : 1;
        CreateGhost(rootCanvas, image.sprite, amount);
        dragging = true;
        canvasGroup.alpha = 0.35f;
        canvasGroup.blocksRaycasts = false;
        UpdateGhost(eventData);
    }

    private void CreateGhost(Canvas rootCanvas, Sprite sprite, int amount)
    {
        ghost = new GameObject("CraftingDragGhost", typeof(RectTransform), typeof(Image));
        ghost.transform.SetParent(rootCanvas.transform, false);
        ghost.transform.SetAsLastSibling();
        ghostRect = ghost.GetComponent<RectTransform>();
        ghostRect.anchorMin = ghostRect.anchorMax = new Vector2(0.5f, 0.5f);
        ghostRect.pivot = new Vector2(0.5f, 0.5f);
        ghostRect.sizeDelta = ((RectTransform)transform).rect.size;
        ghostRect.localScale = Vector3.one;

        var ghostImage = ghost.GetComponent<Image>();
        ghostImage.sprite = sprite;
        ghostImage.color = Color.white;
        ghostImage.preserveAspect = true;
        ghostImage.raycastTarget = false;

        if (amount > 1)
        {
            var labelObject = new GameObject("AmountText", typeof(RectTransform), typeof(TextMeshProUGUI));
            labelObject.transform.SetParent(ghost.transform, false);
            var labelRect = (RectTransform)labelObject.transform;
            labelRect.anchorMin = new Vector2(0f, 0f);
            labelRect.anchorMax = new Vector2(1f, 0f);
            labelRect.pivot = new Vector2(1f, 0f);
            labelRect.offsetMin = new Vector2(0f, 0f);
            labelRect.offsetMax = new Vector2(0f, 24f);

            var label = labelObject.GetComponent<TextMeshProUGUI>();
            label.text = amount.ToString();
            label.fontSize = 18;
            label.alignment = TextAlignmentOptions.BottomRight;
            label.color = Color.white;
            label.fontStyle = FontStyles.Bold;
            label.raycastTarget = false;
            label.textWrappingMode = TextWrappingModes.NoWrap;
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (dragging) UpdateGhost(eventData);
    }

    private void UpdateGhost(PointerEventData eventData)
    {
        if (ghostRect == null) return;
        RectTransform canvasRect = ghostRect.parent as RectTransform;
        if (canvasRect == null) return;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect, eventData.position, eventData.pressEventCamera, out Vector2 localPoint))
            ghostRect.anchoredPosition = localPoint;
    }

    public bool DropOnInventory(int targetSlotIndex)
    {
        if (!dragging || controller == null) return false;
        if (isResult)
        {
            bool success = controller.TryTakeResultToInventory(targetSlotIndex, out string msg);
            if (!success) Debug.LogWarning("[GridCrafting] " + msg);
            return success;
        }
        return controller.TryReturnCellToInventory(cellIndex, targetSlotIndex);
    }

    public void OnEndDrag(PointerEventData eventData) => Cleanup();
    private void OnDisable() => Cleanup();

    private void Cleanup()
    {
        dragging = false;
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.blocksRaycasts = true;
        }
        if (ghost != null) Destroy(ghost);
        ghost = null;
        ghostRect = null;
    }
}
