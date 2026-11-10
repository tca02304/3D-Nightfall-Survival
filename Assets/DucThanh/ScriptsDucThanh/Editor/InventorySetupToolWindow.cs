#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.IO;

namespace DucThanh.Editor
{
    /// <summary>
    /// Editor Window chuyên biệt để cấu hình Túi đồ & Vũ khí cho Player.
    /// Giúp bạn thêm nhanh các Item, gán prefab, căn chỉnh vị trí/góc xoay tay cầm trực quan
    /// và gắn trực tiếp vào Player một cách tự động mà không cần làm thủ công.
    /// </summary>
    public class InventorySetupToolWindow : EditorWindow
    {
        private InventoryDatabaseConfigSO databaseConfig;
        private PlayerInventory targetPlayerInventory;

        private Vector2 scrollPos;
        private int selectedTab = 0;
        private readonly string[] tabTitles = new string[] { "Cấu Hình Túi Đồ", "Tạo Nhanh Item Mới", "Chỉnh Sửa Vị Trí Cầm Tay", "Công Cụ Tiện Ích" };

        // Fields for Hand Offset Editor Tab
        private ItemConfigSO itemToAdjust;
        private GameObject previewHeldInstance;

        // Fields for Quick Item Creator
        private string newItemId = "item_sword";
        private string newItemName = "Kiếm Thép";
        private string newItemDesc = "Vũ khí cận chiến sắc bén.";
        private ItemType newItemType = ItemType.Weapon;
        private GameObject newItemHeldPrefab;
        private float newItemDamage = 30f;
        private float newItemRange = 2.2f;
        private float newItemRadius = 1.2f;
        private int newItemMaxStack = 1;

        // Custom hand offsets
        private string handBoneName = "Weapon (1)";
        private Vector3 customPos = new Vector3(-0.03f, 0f, -0.24f);
        private Vector3 customRot = new Vector3(-190.923f, 103.418f, 0.39f);
        private Vector3 customScale = new Vector3(2.6312f, 0.52624f, 2.6312f);

        [MenuItem("Tools/DucThanh/Quản Lý Túi Đồ & Vật Phẩm", false, 10)]
        [MenuItem("Window/DucThanh/Quản Lý Túi Đồ & Vật Phẩm", false, 10)]
        [MenuItem("DucThanh/Quản Lý Túi Đồ & Vật Phẩm", false, 10)]
        public static void ShowWindow()
        {
            var window = GetWindow<InventorySetupToolWindow>("Túi Đồ & Vật Phẩm");
            window.minSize = new Vector2(480, 560);
            window.Show();
        }

        private void OnEnable()
        {
            FindReferences();
        }

        private void FindReferences()
        {
            if (targetPlayerInventory == null)
            {
                targetPlayerInventory = FindFirstObjectByType<PlayerInventory>();
            }

            if (databaseConfig == null)
            {
                string[] guids = AssetDatabase.FindAssets("t:InventoryDatabaseConfigSO");
                if (guids.Length > 0)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                    databaseConfig = AssetDatabase.LoadAssetAtPath<InventoryDatabaseConfigSO>(path);
                }
            }

            SyncKatanaIntoDatabase();
        }

        private void SyncKatanaIntoDatabase()
        {
            if (databaseConfig == null) return;

            string katanaPath = "Assets/DucThanh/InventoryConfig/Items/Katana.asset";
            ItemConfigSO katanaSO = AssetDatabase.LoadAssetAtPath<ItemConfigSO>(katanaPath);

            if (katanaSO == null)
            {
                string dir = "Assets/DucThanh/InventoryConfig/Items";
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                katanaSO = ScriptableObject.CreateInstance<ItemConfigSO>();
                SerializedObject so = new SerializedObject(katanaSO);
                so.FindProperty("itemId").stringValue = "item_katana";
                so.FindProperty("itemName").stringValue = "Katana";
                so.FindProperty("description").stringValue = "Thanh kiếm Katana Nhật Bản sắc bén.";
                so.FindProperty("itemType").enumValueIndex = (int)ItemType.Weapon;

                GameObject katanaPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/DucThanh/PrefabsDucThanh/Katana.prefab");
                if (katanaPrefab != null)
                {
                    so.FindProperty("heldPrefab").objectReferenceValue = katanaPrefab;
                    so.FindProperty("dropPrefab").objectReferenceValue = katanaPrefab;
                }

                so.FindProperty("handBoneName").stringValue = "Weapon (1)";
                so.FindProperty("handLocalPosition").vector3Value = new Vector3(-0.03f, 0f, -0.24f);
                so.FindProperty("handLocalEulerAngles").vector3Value = new Vector3(-190.923f, 103.418f, 0.39f);
                so.FindProperty("handLocalScale").vector3Value = new Vector3(2.6312f, 0.52624f, 2.6312f);
                so.FindProperty("attackDamage").floatValue = 25f;
                so.FindProperty("attackRange").floatValue = 2.2f;
                so.FindProperty("attackRadius").floatValue = 1.2f;
                so.FindProperty("maxStack").intValue = 1;
                so.ApplyModifiedProperties();

                AssetDatabase.CreateAsset(katanaSO, katanaPath);
                AssetDatabase.SaveAssets();
            }

            if (katanaSO != null && !databaseConfig.RegisteredItems.Contains(katanaSO))
            {
                databaseConfig.RegisteredItems.Add(katanaSO);
                EditorUtility.SetDirty(databaseConfig);
                AssetDatabase.SaveAssets();
            }
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(8);
            GUILayout.Label("HỆ THỐNG QUẢN LÝ TÚI ĐỒ & VŨ KHÍ DUCTHANH", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Tool hỗ trợ tạo item, gắn vị trí cầm trên tay player và liên kết túi đồ nhanh chóng như Spawner Quái.", MessageType.Info);
            EditorGUILayout.Space(6);

            selectedTab = GUILayout.Toolbar(selectedTab, tabTitles);
            EditorGUILayout.Space(8);

            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

            switch (selectedTab)
            {
                case 0:
                    DrawDatabaseTab();
                    break;
                case 1:
                    DrawQuickItemCreatorTab();
                    break;
                case 2:
                    DrawAdjustHandOffsetTab();
                    break;
                case 3:
                    DrawHandOffsetPreviewTab();
                    break;
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawDatabaseTab()
        {
            EditorGUILayout.LabelField("1. Cấu Hình Player & Database", EditorStyles.boldLabel);
            targetPlayerInventory = (PlayerInventory)EditorGUILayout.ObjectField("Player Inventory", targetPlayerInventory, typeof(PlayerInventory), true);
            databaseConfig = (InventoryDatabaseConfigSO)EditorGUILayout.ObjectField("Database Config (SO)", databaseConfig, typeof(InventoryDatabaseConfigSO), false);

            if (databaseConfig == null)
            {
                EditorGUILayout.HelpBox("Chưa có Database Config SO! Nhấn nút bên dưới để tạo tự động trong thư mục DucThanh.", MessageType.Warning);
                if (GUILayout.Button("Tạo Mới Inventory Database Config SO", GUILayout.Height(30)))
                {
                    CreateDatabaseSO();
                }
                return;
            }

            if (targetPlayerInventory == null)
            {
                EditorGUILayout.HelpBox("Chưa có PlayerInventory trên Scene! Nhấn nút bên dưới để tự động tìm hoặc gắn PlayerInventory vào Player.", MessageType.Warning);
                if (GUILayout.Button("Gắn PlayerInventory Lên Player Scene", GUILayout.Height(30)))
                {
                    AttachToPlayer();
                }
            }

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("2. Quản Lý Danh Sách Vật Phẩm", EditorStyles.boldLabel);

            SerializedObject serializedDb = new SerializedObject(databaseConfig);
            serializedDb.Update();

            SerializedProperty maxSlotsProp = serializedDb.FindProperty("maxSlots");
            EditorGUILayout.PropertyField(maxSlotsProp, new GUIContent("Số Ô Túi Đồ"));

            EditorGUILayout.Space(5);
            SerializedProperty startingItemsProp = serializedDb.FindProperty("startingItems");
            EditorGUILayout.PropertyField(startingItemsProp, new GUIContent("Vật Phẩm Bắt Đầu"), true);

            EditorGUILayout.Space(5);
            SerializedProperty registeredItemsProp = serializedDb.FindProperty("registeredItems");
            EditorGUILayout.PropertyField(registeredItemsProp, new GUIContent("Danh Mục Toàn Bộ Item"), true);

            serializedDb.ApplyModifiedProperties();

            EditorGUILayout.Space(15);
            if (GUILayout.Button("Lưu Thay Đổi Database", GUILayout.Height(32)))
            {
                EditorUtility.SetDirty(databaseConfig);
                AssetDatabase.SaveAssets();
                Debug.Log("[DucThanh] Đã lưu cấu hình Inventory Database!");
            }
        }

        private void DrawQuickItemCreatorTab()
        {
            EditorGUILayout.LabelField("Tạo Nhanh Một Item / Vũ Khí Mới", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Điền thông số và prefab, tool sẽ tạo ngay 1 ScriptableObject chuẩn cho bạn và tự add vào Database.", MessageType.None);

            EditorGUILayout.Space(6);
            newItemId = EditorGUILayout.TextField("Item ID (Định danh)", newItemId);
            newItemName = EditorGUILayout.TextField("Tên Vật Phẩm", newItemName);
            newItemDesc = EditorGUILayout.TextField("Mô Tả", newItemDesc);
            newItemType = (ItemType)EditorGUILayout.EnumPopup("Loại Vật Phẩm", newItemType);
            newItemHeldPrefab = (GameObject)EditorGUILayout.ObjectField("Prefab Khi Cầm/Rơi", newItemHeldPrefab, typeof(GameObject), false);

            if (newItemType == ItemType.Weapon)
            {
                EditorGUILayout.Space(4);
                EditorGUILayout.LabelField("Thông Số Chiến Đấu (Vũ Khí)", EditorStyles.miniBoldLabel);
                newItemDamage = EditorGUILayout.FloatField("Sát Thương (Damage)", newItemDamage);
                newItemRange = EditorGUILayout.FloatField("Tầm Đánh (Range)", newItemRange);
                newItemRadius = EditorGUILayout.FloatField("Bán Kính Quét Đòn", newItemRadius);
            }

            newItemMaxStack = EditorGUILayout.IntField("Số Lượng Xếp Chồng", newItemMaxStack);

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Vị Trí & Góc Xoay Cầm Trên Tay Player", EditorStyles.miniBoldLabel);
            handBoneName = EditorGUILayout.TextField("Tên Xương Tay", handBoneName);
            customPos = EditorGUILayout.Vector3Field("Vị Trí Cục Bộ (Pos)", customPos);
            customRot = EditorGUILayout.Vector3Field("Góc Xoay Cục Bộ (Euler)", customRot);
            customScale = EditorGUILayout.Vector3Field("Tỉ Lệ Cục Bộ (Scale)", customScale);

            EditorGUILayout.Space(12);
            if (GUILayout.Button("Tạo ItemConfigSO & Đăng Ký Vào Túi", GUILayout.Height(38)))
            {
                CreateAndRegisterItem();
            }
        }

        private void DrawAdjustHandOffsetTab()
        {
            EditorGUILayout.LabelField("CHỈNH SỬA VỊ TRÍ, GÓC XOAY & TỈ LỆ TRÊN TAY PLAYER", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Chọn món đồ bạn muốn căn chỉnh. Bạn có thể kéo Slider hoặc nhập toạ độ, hoặc bấm 'Bật Preview Trực Quan' để dùng Gizmo/Transform tool của Unity căn trực tiếp trong Scene rồi bấm Lưu!", MessageType.Info);

            EditorGUILayout.Space(6);
            itemToAdjust = (ItemConfigSO)EditorGUILayout.ObjectField("Chọn Item Cần Chỉnh", itemToAdjust, typeof(ItemConfigSO), false);

            if (itemToAdjust == null)
            {
                EditorGUILayout.HelpBox("Hãy chọn một ItemConfigSO (như Katana, Spear, Sword...) từ Project hoặc danh sách Database bên dưới.", MessageType.Warning);
                
                if (databaseConfig != null && databaseConfig.RegisteredItems.Count > 0)
                {
                    EditorGUILayout.Space(4);
                    EditorGUILayout.LabelField("Chọn nhanh từ Database:", EditorStyles.miniBoldLabel);
                    foreach (var it in databaseConfig.RegisteredItems)
                    {
                        if (it == null) continue;
                        if (GUILayout.Button(it.ItemName, GUILayout.Height(24)))
                        {
                            itemToAdjust = it;
                            break;
                        }
                    }
                }
                return;
            }

            EditorGUILayout.Space(8);
            SerializedObject so = new SerializedObject(itemToAdjust);
            so.Update();

            SerializedProperty boneProp = so.FindProperty("handBoneName");
            SerializedProperty posProp = so.FindProperty("handLocalPosition");
            SerializedProperty rotProp = so.FindProperty("handLocalEulerAngles");
            SerializedProperty scaleProp = so.FindProperty("handLocalScale");

            EditorGUILayout.PropertyField(boneProp, new GUIContent("Xương Tay Cầm"));
            EditorGUILayout.PropertyField(posProp, new GUIContent("Vị Trí Cục Bộ (Position)"));
            EditorGUILayout.PropertyField(rotProp, new GUIContent("Góc Xoay Cục Bộ (Euler Rotation)"));
            EditorGUILayout.PropertyField(scaleProp, new GUIContent("Tỉ Lệ Cục Bộ (Scale)"));

            so.ApplyModifiedProperties();

            EditorGUILayout.Space(12);
            EditorGUILayout.LabelField("Xem Trước & Căn Chỉnh Trực Tiếp Trong Scene (Visual Preview)", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Bật Preview Lên Tay Player Trong Scene", GUILayout.Height(32)))
            {
                SpawnPreviewInHand();
            }

            if (GUILayout.Button("Tắt Preview", GUILayout.Height(32)))
            {
                ClearPreview();
            }
            EditorGUILayout.EndHorizontal();

            if (previewHeldInstance != null)
            {
                EditorGUILayout.HelpBox($"Đang bật Preview GameObject '{previewHeldInstance.name}' trên tay Player! Bạn có thể chọn nó trên Scene Hierarchy để dùng Move/Rotate tool xoay chỉnh vị trí thoải mái.", MessageType.Warning);

                if (GUILayout.Button("👉 LƯU TỌA ĐỘ VỪA CHỈNH TRÊN SCENE VÀO ITEM CONFIG 👈", GUILayout.Height(36)))
                {
                    SavePreviewTransformToItem();
                }
            }

            EditorGUILayout.Space(10);
            if (GUILayout.Button("Lưu Trực Tiếp Thông Số Này Vào File Config", GUILayout.Height(30)))
            {
                EditorUtility.SetDirty(itemToAdjust);
                AssetDatabase.SaveAssets();
                Debug.Log($"[DucThanh] Đã lưu thông số tay cầm cho {itemToAdjust.ItemName}!");
            }
        }

        private void SpawnPreviewInHand()
        {
            if (itemToAdjust == null || itemToAdjust.HeldPrefab == null)
            {
                Debug.LogWarning("[DucThanh] Item không có HeldPrefab!");
                return;
            }

            ClearPreview();

            var player = FindFirstObjectByType<PlayerController>();
            if (player == null)
            {
                Debug.LogError("[DucThanh] Không tìm thấy Player trong Scene!");
                return;
            }

            Transform targetHand = null;
            Transform[] children = player.GetComponentsInChildren<Transform>(true);
            foreach (var c in children)
            {
                if (c.name.Equals(itemToAdjust.HandBoneName, System.StringComparison.OrdinalIgnoreCase))
                {
                    targetHand = c;
                    break;
                }
            }

            if (targetHand == null)
            {
                foreach (var c in children)
                {
                    if (c.name == "Weapon (1)")
                    {
                        targetHand = c;
                        break;
                    }
                }
            }

            if (targetHand == null) targetHand = player.transform;

            previewHeldInstance = (GameObject)PrefabUtility.InstantiatePrefab(itemToAdjust.HeldPrefab, targetHand);
            previewHeldInstance.name = $"[PREVIEW_{itemToAdjust.ItemName}]";
            previewHeldInstance.transform.localPosition = itemToAdjust.HandLocalPosition;
            previewHeldInstance.transform.localRotation = Quaternion.Euler(itemToAdjust.HandLocalEulerAngles);
            previewHeldInstance.transform.localScale = itemToAdjust.HandLocalScale;

            Selection.activeGameObject = previewHeldInstance;
            SceneView.FrameLastActiveSceneView();
        }

        private void ClearPreview()
        {
            if (previewHeldInstance != null)
            {
                DestroyImmediate(previewHeldInstance);
                previewHeldInstance = null;
            }

            // Dọn các preview cũ nếu còn sót
            var player = FindFirstObjectByType<PlayerController>();
            if (player != null)
            {
                var allT = player.GetComponentsInChildren<Transform>(true);
                foreach (var t in allT)
                {
                    if (t != null && t.name.StartsWith("[PREVIEW_"))
                    {
                        DestroyImmediate(t.gameObject);
                    }
                }
            }
        }

        private void SavePreviewTransformToItem()
        {
            if (previewHeldInstance == null || itemToAdjust == null) return;

            Undo.RecordObject(itemToAdjust, "Save Hand Offset");
            itemToAdjust.SetHandOffset(
                previewHeldInstance.transform.localPosition,
                previewHeldInstance.transform.localEulerAngles,
                previewHeldInstance.transform.localScale
            );

            EditorUtility.SetDirty(itemToAdjust);
            AssetDatabase.SaveAssets();

            Debug.Log($"<color=green>[DucThanh] ĐÃ LƯU THÀNH CÔNG VỊ TRÍ CẦM MỚI CHO {itemToAdjust.ItemName}:\n" +
                      $"Pos: {itemToAdjust.HandLocalPosition}\n" +
                      $"Rot: {itemToAdjust.HandLocalEulerAngles}\n" +
                      $"Scale: {itemToAdjust.HandLocalScale}</color>");
        }

        private void DrawHandOffsetPreviewTab()
        {
            EditorGUILayout.LabelField("Hỗ Trợ Copy Vị Trí Cầm Từ Katana Hiện Tại", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Katana mẫu trong DucThanh đã có sẵn vị trí chuẩn: \nPos: (-0.03, 0, -0.24)\nRot: (-190.92, 103.42, 0.39)\nScale: (2.63, 0.53, 2.63)", MessageType.Info);

            if (GUILayout.Button("Tự Động Copy Toạ Độ Mẫu Từ Katana Hiện Tại"))
            {
                customPos = new Vector3(-0.03f, 0f, -0.24f);
                customRot = new Vector3(-190.923f, 103.418f, 0.39f);
                customScale = new Vector3(2.6312f, 0.52624f, 2.6312f);
                handBoneName = "Weapon (1)";
                selectedTab = 1;
            }

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Spawn Thử Vật Phẩm Ra Mặt Đất Để Test Nhặt", EditorStyles.boldLabel);
            if (databaseConfig != null && databaseConfig.RegisteredItems.Count > 0)
            {
                foreach (var item in databaseConfig.RegisteredItems)
                {
                    if (item == null) continue;
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField(item.ItemName, GUILayout.Width(150));
                    if (GUILayout.Button("Spawn Ra Scene"))
                    {
                        SpawnItemInScene(item);
                    }
                    EditorGUILayout.EndHorizontal();
                }
            }
            else
            {
                EditorGUILayout.LabelField("Chưa có item nào trong database để test spawn.");
            }

            EditorGUILayout.Space(15);
            EditorGUILayout.LabelField("Biến Vật Thể Bất Kỳ Thành Vật Phẩm Nhặt Được (Ground Item)", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Chọn GameObject bất kỳ trong Hierarchy hoặc thả Prefab vào bên dưới, nhấn nút để tự động gắn ItemPickupWorld và Collider chuẩn.", MessageType.None);
            
            selectedGameObjectToConvert = (GameObject)EditorGUILayout.ObjectField("GameObject / Prefab", selectedGameObjectToConvert, typeof(GameObject), true);
            selectedItemConfigForConvert = (ItemConfigSO)EditorGUILayout.ObjectField("Item Config SO tương ứng", selectedItemConfigForConvert, typeof(ItemConfigSO), false);

            if (GUILayout.Button("Gắn ItemPickupWorld & Collider Lên Vật Thể", GUILayout.Height(30)))
            {
                ConvertSelectedToPickup();
            }
        }

        private GameObject selectedGameObjectToConvert;
        private ItemConfigSO selectedItemConfigForConvert;

        private void ConvertSelectedToPickup()
        {
            if (selectedGameObjectToConvert == null)
            {
                Debug.LogWarning("[DucThanh] Hãy chọn một GameObject hoặc Prefab để chuyển đổi!");
                return;
            }

            var pickup = selectedGameObjectToConvert.GetComponent<ItemPickupWorld>();
            if (pickup == null)
            {
                pickup = Undo.AddComponent<ItemPickupWorld>(selectedGameObjectToConvert);
            }

            var col = selectedGameObjectToConvert.GetComponent<Collider>();
            if (col == null)
            {
                var sc = Undo.AddComponent<SphereCollider>(selectedGameObjectToConvert);
                sc.isTrigger = true;
                sc.radius = 1.8f;
            }
            else
            {
                Undo.RecordObject(col, "Make Trigger");
                col.isTrigger = true;
            }

            if (selectedItemConfigForConvert != null)
            {
                var so = new SerializedObject(pickup);
                so.FindProperty("itemConfig").objectReferenceValue = selectedItemConfigForConvert;
                so.ApplyModifiedProperties();
            }

            EditorUtility.SetDirty(selectedGameObjectToConvert);
            Debug.Log($"[DucThanh] Đã gắn thành công ItemPickupWorld lên {selectedGameObjectToConvert.name}!");
        }

        private void CreateDatabaseSO()
        {
            string dir = "Assets/DucThanh/InventoryConfig";
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            string path = $"{dir}/InventoryDatabaseConfig.asset";
            var newConfig = ScriptableObject.CreateInstance<InventoryDatabaseConfigSO>();
            AssetDatabase.CreateAsset(newConfig, path);
            AssetDatabase.SaveAssets();

            databaseConfig = newConfig;
            Debug.Log($"[DucThanh] Đã tạo InventoryDatabaseConfig tại {path}");
        }

        private void AttachToPlayer()
        {
            var player = FindFirstObjectByType<PlayerController>();
            if (player == null)
            {
                Debug.LogError("[DucThanh] Không tìm thấy PlayerController trong Scene hiện tại!");
                return;
            }

            var inv = player.GetComponent<PlayerInventory>();
            if (inv == null)
            {
                inv = player.gameObject.AddComponent<PlayerInventory>();
            }

            // Gán config
            var soProp = new SerializedObject(inv);
            var configProp = soProp.FindProperty("config");
            configProp.objectReferenceValue = databaseConfig;
            soProp.ApplyModifiedProperties();

            targetPlayerInventory = inv;
            EditorUtility.SetDirty(player.gameObject);
            Debug.Log("[DucThanh] Đã gắn PlayerInventory lên Player thành công!");
        }

        private void CreateAndRegisterItem()
        {
            string dir = "Assets/DucThanh/InventoryConfig/Items";
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            string path = $"{dir}/{newItemId}.asset";
            var itemSO = ScriptableObject.CreateInstance<ItemConfigSO>();

            SerializedObject so = new SerializedObject(itemSO);
            so.FindProperty("itemId").stringValue = newItemId;
            so.FindProperty("itemName").stringValue = newItemName;
            so.FindProperty("description").stringValue = newItemDesc;
            so.FindProperty("itemType").enumValueIndex = (int)newItemType;
            so.FindProperty("heldPrefab").objectReferenceValue = newItemHeldPrefab;
            so.FindProperty("dropPrefab").objectReferenceValue = newItemHeldPrefab;
            so.FindProperty("handBoneName").stringValue = handBoneName;
            so.FindProperty("handLocalPosition").vector3Value = customPos;
            so.FindProperty("handLocalEulerAngles").vector3Value = customRot;
            so.FindProperty("handLocalScale").vector3Value = customScale;
            so.FindProperty("attackDamage").floatValue = newItemDamage;
            so.FindProperty("attackRange").floatValue = newItemRange;
            so.FindProperty("attackRadius").floatValue = newItemRadius;
            so.FindProperty("maxStack").intValue = newItemMaxStack;
            so.ApplyModifiedProperties();

            AssetDatabase.CreateAsset(itemSO, path);
            AssetDatabase.SaveAssets();

            if (databaseConfig != null)
            {
                if (!databaseConfig.RegisteredItems.Contains(itemSO))
                {
                    databaseConfig.RegisteredItems.Add(itemSO);
                    EditorUtility.SetDirty(databaseConfig);
                    AssetDatabase.SaveAssets();
                }
            }

            Debug.Log($"[DucThanh] Đã tạo thành công Item: {newItemName} ({path}) và đăng ký vào Database!");
            selectedTab = 0;
        }

        private void SpawnItemInScene(ItemConfigSO item)
        {
            if (item == null || item.DropPrefab == null)
            {
                Debug.LogWarning("[DucThanh] Item không có DropPrefab để spawn!");
                return;
            }

            Vector3 spawnPos = Vector3.zero;
            var player = FindFirstObjectByType<PlayerController>();
            if (player != null)
            {
                spawnPos = player.transform.position + player.transform.forward * 2f + Vector3.up * 0.5f;
            }

            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(item.DropPrefab);
            instance.transform.position = spawnPos;

            var pickup = instance.GetComponent<ItemPickupWorld>();
            if (pickup == null)
            {
                pickup = instance.AddComponent<ItemPickupWorld>();
            }

            var so = new SerializedObject(pickup);
            so.FindProperty("itemConfig").objectReferenceValue = item;
            so.FindProperty("quantity").intValue = 1;
            so.ApplyModifiedProperties();

            Selection.activeGameObject = instance;
            Undo.RegisterCreatedObjectUndo(instance, "Spawn Test Item");
            Debug.Log($"[DucThanh] Đã spawn {item.ItemName} tại vị trí {spawnPos}");
        }
    }
}
#endif
