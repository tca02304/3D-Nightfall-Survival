using System.Collections.Generic;
using UnityEngine;

namespace DucThanh
{
    /// <summary>
    /// Component quản lý Object Pooling chuyên dụng cho quái vật trong DucThanh.
    /// Giúp tái sử dụng GameObject, loại bỏ hiện tượng rác bộ nhớ (Garbage Collection) và lag/giật khung hình khi spawn quái dồn dập.
    /// </summary>
    [DisallowMultipleComponent]
    public class EnemyPoolManager : MonoBehaviour
    {
        private static EnemyPoolManager instance;
        public static EnemyPoolManager Instance => instance;

        [System.Serializable]
        public class PoolItem
        {
            public GameObject prefab;
            public int initialPoolSize = 10;
        }

        [Header("Preload Configuration")]
        [SerializeField] private List<PoolItem> preloadPools = new List<PoolItem>();

        private readonly Dictionary<GameObject, Queue<GameObject>> poolDictionary = new Dictionary<GameObject, Queue<GameObject>>();
        private readonly Dictionary<GameObject, GameObject> instanceToPrefabMap = new Dictionary<GameObject, GameObject>();

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;

            InitializePools();
        }

        private void InitializePools()
        {
            foreach (var item in preloadPools)
            {
                if (item.prefab == null) continue;
                EnsurePoolExists(item.prefab, item.initialPoolSize);
            }
        }

        private void EnsurePoolExists(GameObject prefab, int count)
        {
            if (!poolDictionary.TryGetValue(prefab, out Queue<GameObject> queue))
            {
                queue = new Queue<GameObject>();
                poolDictionary[prefab] = queue;
            }

            while (queue.Count < count)
            {
                GameObject obj = Instantiate(prefab, transform);
                obj.SetActive(false);
                instanceToPrefabMap[obj] = prefab;
                queue.Enqueue(obj);
            }
        }

        /// <summary>
        /// Lấy quái từ Pool hoặc khởi tạo thêm nếu rỗng
        /// </summary>
        public GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation)
        {
            if (prefab == null) return null;

            if (!poolDictionary.TryGetValue(prefab, out Queue<GameObject> queue))
            {
                queue = new Queue<GameObject>();
                poolDictionary[prefab] = queue;
            }

            GameObject obj = null;
            while (queue.Count > 0)
            {
                GameObject candidate = queue.Dequeue();
                if (candidate != null)
                {
                    obj = candidate;
                    break;
                }
            }

            if (obj == null)
            {
                obj = Instantiate(prefab, position, rotation, transform);
                instanceToPrefabMap[obj] = prefab;
            }
            else
            {
                obj.transform.position = position;
                obj.transform.rotation = rotation;
            }

            obj.SetActive(true);
            return obj;
        }

        /// <summary>
        /// Trả quái về Pool thay vì Destroy
        /// </summary>
        public bool Despawn(GameObject obj)
        {
            if (obj == null) return false;

            if (instanceToPrefabMap.TryGetValue(obj, out GameObject originalPrefab))
            {
                if (!poolDictionary.TryGetValue(originalPrefab, out Queue<GameObject> queue))
                {
                    queue = new Queue<GameObject>();
                    poolDictionary[originalPrefab] = queue;
                }

                obj.SetActive(false);
                obj.transform.SetParent(transform);
                queue.Enqueue(obj);
                return true;
            }

            // Nếu không thuộc pool này, chỉ disable hoặc destroy
            obj.SetActive(false);
            return false;
        }
    }
}
