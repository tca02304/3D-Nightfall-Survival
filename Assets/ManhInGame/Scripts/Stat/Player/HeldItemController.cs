using System;
using UnityEngine;

namespace Nightfall.Stats
{
    [Serializable]
    public struct HeldItemPose
    {
        public string itemCode;
        public Vector3 localPosition;
        public Vector3 localEuler;
        [Min(.01f)] public float size;
    }

    [DisallowMultipleComponent]
    public sealed class HeldItemController : MonoBehaviour
    {
        [SerializeField] private PlayerStatProvider stats;
        [Header("Assign a dedicated grip or leave empty for an automatic camera socket")]
        [SerializeField] private Transform handSocket;
        [SerializeField] private Vector3 cameraSocketPosition = new Vector3(.45f, -.32f, .85f);
        [SerializeField] private Vector3 defaultEuler = new Vector3(10f, 30f, -15f);
        [SerializeField, Min(.01f)] private float defaultSize = .55f;
        [SerializeField] private HeldItemPose[] poses = Array.Empty<HeldItemPose>();
        [SerializeField] private bool showVisual = true;

        [Header("First-person viewmodel (local player only)")]
        [SerializeField] private Camera firstPersonCamera;
        [SerializeField] private string viewModelLayerName = "ViewModel";
        [SerializeField] private bool renderForLocalPlayer = true;
        [SerializeField] private bool centerWorldPrefabGeometry = true;

        [Header("Category pose fallback (individual poses take priority)")]
        [SerializeField] private Vector3 axeEuler = new Vector3(0f, -40f, -30f);
        [SerializeField] private Vector3 pickaxeEuler = new Vector3(0f, -45f, -25f);
        [SerializeField] private Vector3 swordEuler = new Vector3(5f, 25f, -20f);
        private HotbarManager bound;
        private GameObject visual;
        private Transform autoSocket;
        private ItemData displayedItem;
        public ItemData CurrentItem => displayedItem;

        // Find the provider on the same Player root without adding or modifying teammate scripts.
        private void Awake()
        {
            if (stats == null) stats = GetComponent<PlayerStatProvider>();
        }

        // Subscribe and refresh on enable, even if this player is instantiated after persistent Managers.
        private void OnEnable() { Bind(); Refresh(); }

        // Rebind only if HotbarManager changes, and retry initial readiness without rebuilding each frame.
        private void LateUpdate()
        {
            if (bound != HotbarManager.Instance) Bind();
            if (bound != null && !ReferenceEquals(displayedItem, bound.GetSelectedItem())) Refresh();
            if (handSocket == null && autoSocket == null && displayedItem != null && showVisual && TryGetSocket() != null) RebuildVisual();
        }

        // Bind exactly once to the existing selection and inventory-change event.
        private void Bind()
        {
            if (bound == HotbarManager.Instance) return;
            if (bound != null) bound.OnSelectedItemChanged -= OnSelectedItemChanged;
            bound = HotbarManager.Instance;
            if (bound != null) bound.OnSelectedItemChanged += OnSelectedItemChanged;
            Refresh();
        }

        // Stop observing stale Managers and remove the presentation object.
        private void OnDisable()
        {
            if (bound != null) bound.OnSelectedItemChanged -= OnSelectedItemChanged;
            bound = null; displayedItem = null;
            if (stats != null) stats.SetHeldItem(null);
            ClearVisual();
        }

        // Release an automatically created socket; an assigned teammate socket is never destroyed.
        private void OnDestroy() { if (autoSocket != null) Destroy(autoSocket.gameObject); }

        // Consume the manager event without changing inventory amount or selecting another slot.
        private void OnSelectedItemChanged(ItemData item) => SetSelected(item);

        // Read the selection directly, including an empty slot.
        public void Refresh() => SetSelected(bound != null ? bound.GetSelectedItem() : null);

        // Update stat data and render only when the selected item actually changes.
        private void SetSelected(ItemData item)
        {
            if (ReferenceEquals(displayedItem, item)) return;
            displayedItem = item;
            if (stats != null) stats.SetHeldItem(item);
            RebuildVisual();
        }

        // Enable the first-person presentation only for the locally controlled character.
        public void SetLocalViewEnabled(bool enabled)
        {
            if (renderForLocalPlayer == enabled) return;
            renderForLocalPlayer = enabled;
            if (!enabled) ClearVisual();
            else RebuildVisual();
        }

        // Hide or show only the mesh; the held item remains selected when the inventory UI opens.
        public void SetVisualVisible(bool visible)
        {
            showVisual = visible;
            if (visual != null) visual.SetActive(visible);
            else if (visible) RebuildVisual();
        }

        // Prefer an explicitly assigned hand grip; use the player's Camera for a first-person fallback.
        private Transform TryGetSocket()
        {
            if (handSocket != null) return handSocket;
            if (autoSocket != null) return autoSocket;
            Camera playerCamera = firstPersonCamera;
            if (playerCamera == null)
            {
                // Use the main camera when it belongs to this player, not an overlay camera.
                Camera main = Camera.main;
                if (main != null && main.transform.IsChildOf(transform)) playerCamera = main;
                else playerCamera = GetComponentInChildren<Camera>(true);
            }
            if (playerCamera == null) return null;
            var socket = new GameObject("HeldItemSocket"); autoSocket = socket.transform;
            autoSocket.SetParent(playerCamera.transform, false); autoSocket.localPosition = cameraSocketPosition;
            return autoSocket;
        }

        // Build pure render geometry; never instantiate WorldItem logic, physics, pickup, or enemy scripts into the hand.
        private void RebuildVisual()
        {
            ClearVisual();
            if (!renderForLocalPlayer || !showVisual || displayedItem == null || ItemDatabase.Instance == null) return;
            int layer = LayerMask.NameToLayer(viewModelLayerName);
            if (layer < 0)
            {
                Debug.LogError($"[HeldItemController] Create Unity layer '{viewModelLayerName}' before enabling the first-person visual.", this);
                return;
            }
            Transform socket = TryGetSocket(); if (socket == null) return;
            if (firstPersonCamera != null && (firstPersonCamera.cullingMask & (1 << layer)) != 0)
                Debug.LogWarning("[HeldItemController] World camera still renders the ViewModel layer. Remove it from the world camera Culling Mask.", this);
            var definition = ItemDatabase.Instance.GetDefinitionById(displayedItem.id);
            if (definition == null) return;
            visual = new GameObject("Held_" + definition.ItemCode); visual.transform.SetParent(socket, false);
            visual.layer = layer;
            var content = new GameObject("Visual"); content.layer = layer; content.transform.SetParent(visual.transform, false);
            Transform source = definition.HeldVisualPrefab != null ? definition.HeldVisualPrefab.transform : definition.WorldPrefab != null ? definition.WorldPrefab.transform : null;
            int copied = source != null ? CopyRenderTree(source, content.transform, true, layer) : 0;
            if (copied == 0 && definition.Icon != null)
            {
                var sr = content.AddComponent<SpriteRenderer>(); sr.sprite = definition.Icon;
                sr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
            float size = defaultSize; Vector3 offset = Vector3.zero;
            Vector3 euler = GetCategoryEuler(definition.ItemCode);
            foreach (var pose in poses) if (pose.itemCode == definition.ItemCode)
            { size = Mathf.Max(.01f, pose.size); offset = pose.localPosition; euler = pose.localEuler; break; }
            FitSize(content.transform, size, centerWorldPrefabGeometry && definition.HeldVisualPrefab == null);
            visual.transform.localPosition = offset; visual.transform.localRotation = Quaternion.Euler(euler);
        }

        // Pick a reasonable class pose before applying any exact per-item pose override.
        private Vector3 GetCategoryEuler(string code)
        {
            if (string.IsNullOrEmpty(code)) return defaultEuler;
            code = code.ToLowerInvariant();
            if (code.Contains("pickaxe")) return pickaxeEuler;
            if (code.Contains("axe")) return axeEuler;
            if (code.Contains("sword") || code.Contains("blade")) return swordEuler;
            return defaultEuler;
        }

        // Copy only static meshes and sprites while preserving relative hierarchy and shared materials.
        private int CopyRenderTree(Transform source, Transform target, bool root, int layer)
        {
            if (!source.gameObject.activeSelf) return 0;
            Transform dst = target;
            if (!root)
            {
                var go = new GameObject(source.name); go.layer = layer; dst = go.transform; dst.SetParent(target, false);
                dst.localPosition = source.localPosition; dst.localRotation = source.localRotation; dst.localScale = source.localScale;
            }
            else dst.localScale = source.localScale;
            int count = 0;
            var mf = source.GetComponent<MeshFilter>(); var mr = source.GetComponent<MeshRenderer>();
            if (mf != null && mf.sharedMesh != null && mr != null && mr.enabled)
            {
                dst.gameObject.AddComponent<MeshFilter>().sharedMesh = mf.sharedMesh;
                var r = dst.gameObject.AddComponent<MeshRenderer>(); r.sharedMaterials = mr.sharedMaterials;
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; count++;
            }
            var originalSprite = source.GetComponent<SpriteRenderer>();
            if (originalSprite != null && originalSprite.sprite != null && originalSprite.enabled && dst.GetComponent<Renderer>() == null)
            { var r = dst.gameObject.AddComponent<SpriteRenderer>(); r.sprite = originalSprite.sprite; r.color = originalSprite.color; count++; }
            foreach (Transform child in source) count += CopyRenderTree(child, dst, false, layer);
            return count;
        }

        // Normalize model dimensions and optionally remove world-prefab mesh offsets.
        private void FitSize(Transform content, float desired, bool centerGeometry)
        {
            var renderers = content.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return;
            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
            float largest = Mathf.Max(bounds.size.x, Mathf.Max(bounds.size.y, bounds.size.z));
            if (largest > .0001f) content.localScale *= desired / largest;
            if (!centerGeometry) return;
            bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
            Transform parent = content.parent;
            if (parent != null) content.localPosition -= parent.InverseTransformPoint(bounds.center);
        }

        // Hide immediately before destroying the old geometry at the end of the frame.
        private void ClearVisual()
        {
            if (visual != null) { visual.SetActive(false); Destroy(visual); visual = null; }
        }
    }
}
