using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace DucThanh.Editor
{
    /// <summary>
    /// Công cụ Editor tự động thiết lập hoặc cập nhật Prefab Nỏ (Crossbow / Ballista) và Máy Bắn Đá (Catapult / Onager).
    /// Tự động trích xuất AnimationClip từ FBX, tạo AnimatorController, gắn component Animator và cấu hình bắn.
    /// </summary>
    public static class SiegeEngineSetupHelper
    {
        [MenuItem("DucThanh/Siege Engines/1. Tự động thiết lập Prefabs Nỏ & Máy Bắn Đá (Kèm Animation)", false, 50)]
        public static void SetupAll()
        {
            // 1. Tạo Prefab đạn nếu chưa có
            GameObject arrowPrefab = CreateArrowProjectilePrefab();
            GameObject stonePrefab = CreateBoulderProjectilePrefab();

            // 2. Tạo AnimatorController cho Nỏ và Máy Bắn Đá
            RuntimeAnimatorController ballistaController = SetupBallistaAnimatorController();
            RuntimeAnimatorController onagerController = SetupOnagerAnimatorController();

            // 3. Cập nhật Prefabs Nỏ & Máy Bắn Đá
            SetupCrossbowPrefab(arrowPrefab, ballistaController);
            SetupCatapultPrefab(stonePrefab, onagerController);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("<color=green><b>[SiegeEngineSetup] Đã hoàn tất cài đặt toàn bộ Prefabs, AnimatorController và Animation bắn cho Nỏ & Máy Bắn Đá!</b></color>");
        }

        private static AnimationClip LoadAnimationClipFromAsset(string assetPath)
        {
            Object[] objects = AssetDatabase.LoadAllAssetsAtPath(assetPath);
            foreach (var obj in objects)
            {
                if (obj is AnimationClip clip && !clip.name.StartsWith("__preview__"))
                {
                    return clip;
                }
            }
            return null;
        }

        private static RuntimeAnimatorController SetupBallistaAnimatorController()
        {
            string controllerPath = "Assets/DucThanh/AnimationsDucThanh/Ballista_Controller.controller";
            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);

            string fireFbxPath = "Assets/DucThanh/PolyClem_Siege_Engines 1/FBX/Ballista_Fire.fbx";
            string reloadFbxPath = "Assets/DucThanh/PolyClem_Siege_Engines 1/FBX/Ballista_Reload.fbx";

            AnimationClip fireClip = LoadAnimationClipFromAsset(fireFbxPath);
            AnimationClip reloadClip = LoadAnimationClipFromAsset(reloadFbxPath);

            if (controller == null)
            {
                controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
            }

            // Đảm bảo có Trigger Fire
            bool hasFireParam = false;
            foreach (var p in controller.parameters)
            {
                if (p.name == "Fire") { hasFireParam = true; break; }
            }
            if (!hasFireParam)
            {
                controller.AddParameter("Fire", AnimatorControllerParameterType.Trigger);
            }

            // Cấu hình Layers & States
            var rootStateMachine = controller.layers[0].stateMachine;
            rootStateMachine.states = new ChildAnimatorState[0]; // Reset states để setup sạch

            // Idle State (hoặc dùng Reload clip ở frame đầu làm idle)
            var idleState = rootStateMachine.AddState("Idle");
            if (reloadClip != null) idleState.motion = reloadClip;

            // Fire State
            var fireState = rootStateMachine.AddState("Fire");
            if (fireClip != null) fireState.motion = fireClip;

            rootStateMachine.defaultState = idleState;

            // Transition: AnyState -> Fire (khi kích hoạt Trigger Fire)
            var anyToFire = rootStateMachine.AddAnyStateTransition(fireState);
            anyToFire.AddCondition(AnimatorConditionMode.If, 0, "Fire");
            anyToFire.hasExitTime = false;
            anyToFire.duration = 0.05f;

            // Transition: Fire -> Idle (sau khi bắn xong)
            var fireToIdle = fireState.AddTransition(idleState);
            fireToIdle.hasExitTime = true;
            fireToIdle.exitTime = 0.95f;
            fireToIdle.duration = 0.1f;

            EditorUtility.SetDirty(controller);
            return controller;
        }

        private static RuntimeAnimatorController SetupOnagerAnimatorController()
        {
            string controllerPath = "Assets/DucThanh/AnimationsDucThanh/Onager_Controller.controller";
            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);

            string fireFbxPath = "Assets/DucThanh/PolyClem_Siege_Engines 1/FBX/Onager_Fire.fbx";
            string reloadFbxPath = "Assets/DucThanh/PolyClem_Siege_Engines 1/FBX/Onager_Reload.fbx";

            AnimationClip fireClip = LoadAnimationClipFromAsset(fireFbxPath);
            AnimationClip reloadClip = LoadAnimationClipFromAsset(reloadFbxPath);

            if (controller == null)
            {
                controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
            }

            bool hasFireParam = false;
            foreach (var p in controller.parameters)
            {
                if (p.name == "Fire") { hasFireParam = true; break; }
            }
            if (!hasFireParam)
            {
                controller.AddParameter("Fire", AnimatorControllerParameterType.Trigger);
            }

            var rootStateMachine = controller.layers[0].stateMachine;
            rootStateMachine.states = new ChildAnimatorState[0];

            var idleState = rootStateMachine.AddState("Idle");
            if (reloadClip != null) idleState.motion = reloadClip;

            var fireState = rootStateMachine.AddState("Fire");
            if (fireClip != null) fireState.motion = fireClip;

            rootStateMachine.defaultState = idleState;

            var anyToFire = rootStateMachine.AddAnyStateTransition(fireState);
            anyToFire.AddCondition(AnimatorConditionMode.If, 0, "Fire");
            anyToFire.hasExitTime = false;
            anyToFire.duration = 0.05f;

            var fireToIdle = fireState.AddTransition(idleState);
            fireToIdle.hasExitTime = true;
            fireToIdle.exitTime = 0.95f;
            fireToIdle.duration = 0.1f;

            EditorUtility.SetDirty(controller);
            return controller;
        }

        private static GameObject CreateArrowProjectilePrefab()
        {
            string prefabPath = "Assets/DucThanh/PrefabsDucThanh/Arrow_Projectile.prefab";
            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (existing != null) return existing;

            string boltFbxPath = "Assets/DucThanh/PolyClem_Siege_Engines 1/FBX/Ballista_Bolt.fbx";
            GameObject boltFbx = AssetDatabase.LoadAssetAtPath<GameObject>(boltFbxPath);

            GameObject go;
            if (boltFbx != null)
            {
                go = Object.Instantiate(boltFbx);
                go.name = "Arrow_Projectile";
            }
            else
            {
                go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                go.name = "Arrow_Projectile";
                go.transform.localScale = new Vector3(0.1f, 0.6f, 0.1f);
            }

            var col = go.GetComponent<Collider>();
            if (col == null)
            {
                var box = go.AddComponent<BoxCollider>();
                box.isTrigger = true;
                box.size = new Vector3(0.5f, 0.5f, 2f);
            }
            else
            {
                col.isTrigger = true;
            }

            var rb = go.GetComponent<Rigidbody>();
            if (rb == null)
            {
                rb = go.AddComponent<Rigidbody>();
                rb.isKinematic = true;
                rb.useGravity = false;
            }

            if (go.GetComponent<ArrowProjectile>() == null)
            {
                go.AddComponent<ArrowProjectile>();
            }

            GameObject saved = PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
            Object.DestroyImmediate(go);
            return saved;
        }

        private static GameObject CreateBoulderProjectilePrefab()
        {
            string prefabPath = "Assets/DucThanh/PrefabsDucThanh/Boulder_Projectile.prefab";
            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (existing != null) return existing;

            string stoneFbxPath = "Assets/DucThanh/PolyClem_Siege_Engines 1/FBX/Onager_Stone.fbx";
            GameObject stoneFbx = AssetDatabase.LoadAssetAtPath<GameObject>(stoneFbxPath);

            GameObject go;
            if (stoneFbx != null)
            {
                go = Object.Instantiate(stoneFbx);
                go.name = "Boulder_Projectile";
            }
            else
            {
                go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                go.name = "Boulder_Projectile";
                go.transform.localScale = new Vector3(0.8f, 0.8f, 0.8f);
            }

            var col = go.GetComponent<Collider>();
            if (col == null)
            {
                var sphere = go.AddComponent<SphereCollider>();
                sphere.isTrigger = true;
                sphere.radius = 0.5f;
            }
            else
            {
                col.isTrigger = true;
            }

            var rb = go.GetComponent<Rigidbody>();
            if (rb == null)
            {
                rb = go.AddComponent<Rigidbody>();
                rb.isKinematic = true;
                rb.useGravity = false;
            }

            if (go.GetComponent<BoulderProjectile>() == null)
            {
                go.AddComponent<BoulderProjectile>();
            }

            GameObject saved = PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
            Object.DestroyImmediate(go);
            return saved;
        }

        private static void SetupCrossbowPrefab(GameObject arrowPrefab, RuntimeAnimatorController animController)
        {
            string prefabPath = "Assets/DucThanh/PrefabsDucThanh/No.prefab";
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null) return;

            GameObject instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            if (instance == null) return;

            Animator anim = instance.GetComponent<Animator>();
            if (anim == null)
            {
                anim = instance.AddComponent<Animator>();
            }
            if (animController != null)
            {
                anim.runtimeAnimatorController = animController;
            }

            CrossbowTurret turret = instance.GetComponent<CrossbowTurret>();
            if (turret == null)
            {
                turret = instance.AddComponent<CrossbowTurret>();
            }

            SerializedObject so = new SerializedObject(turret);
            so.FindProperty("targetTag").stringValue = "Enemy";
            so.FindProperty("attackRange").floatValue = 25f;
            so.FindProperty("fireRate").floatValue = 1.5f;
            so.FindProperty("attackDamage").floatValue = 45f;
            so.FindProperty("projectileSpeed").floatValue = 35f;
            so.FindProperty("projectileSpawnDelay").floatValue = 0.125f; // Khớp chuẩn frame bắn nỏ
            so.FindProperty("fireAnimationTrigger").stringValue = "Fire";

            so.FindProperty("animator").objectReferenceValue = anim;

            if (arrowPrefab != null)
            {
                so.FindProperty("projectilePrefab").objectReferenceValue = arrowPrefab;
            }

            so.ApplyModifiedProperties();

            PrefabUtility.SaveAsPrefabAsset(instance, prefabPath);
            Object.DestroyImmediate(instance);
            Debug.Log($"<color=cyan>[SiegeEngineSetup] Đã cấu hình Animator và CrossbowTurret cho {prefabPath}</color>");
        }

        private static void SetupCatapultPrefab(GameObject stonePrefab, RuntimeAnimatorController animController)
        {
            string prefabPath = "Assets/DucThanh/PrefabsDucThanh/MayBanDa.prefab";
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null) return;

            GameObject instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            if (instance == null) return;

            Animator anim = instance.GetComponent<Animator>();
            if (anim == null)
            {
                anim = instance.AddComponent<Animator>();
            }
            if (animController != null)
            {
                anim.runtimeAnimatorController = animController;
            }

            CatapultTurret turret = instance.GetComponent<CatapultTurret>();
            if (turret == null)
            {
                turret = instance.AddComponent<CatapultTurret>();
            }

            SerializedObject so = new SerializedObject(turret);
            so.FindProperty("targetTag").stringValue = "Enemy";
            so.FindProperty("attackRange").floatValue = 35f;
            so.FindProperty("fireRate").floatValue = 3.5f;
            so.FindProperty("attackDamage").floatValue = 80f;
            so.FindProperty("projectileSpeed").floatValue = 28f;
            so.FindProperty("projectileSpawnDelay").floatValue = 0.375f; // Khớp chuẩn frame vung gáo máy bắn đá
            so.FindProperty("fireAnimationTrigger").stringValue = "Fire";

            so.FindProperty("animator").objectReferenceValue = anim;

            if (stonePrefab != null)
            {
                so.FindProperty("projectilePrefab").objectReferenceValue = stonePrefab;
            }

            so.ApplyModifiedProperties();

            PrefabUtility.SaveAsPrefabAsset(instance, prefabPath);
            Object.DestroyImmediate(instance);
            Debug.Log($"<color=cyan>[SiegeEngineSetup] Đã cấu hình Animator và CatapultTurret cho {prefabPath}</color>");
        }
    }
}
