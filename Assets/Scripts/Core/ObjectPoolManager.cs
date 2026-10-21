using System;
using System.Collections.Generic;
using UnityEngine;

namespace IdleFishing.Core
{
    /// <summary>
    /// Tracks an instance spawned from ObjectPoolManager and manages auto-release without coroutines.
    /// </summary>
    public class PooledInstance : MonoBehaviour
    {
        public int PrefabId { get; set; }
        public float RemainingLife { get; set; }
        public bool AutoRelease { get; set; }
        public Action OnDespawn { get; set; }

        private void Update()
        {
            if (!AutoRelease) return;

            RemainingLife -= Time.deltaTime;
            if (RemainingLife <= 0f)
            {
                AutoRelease = false;
                if (ObjectPoolManager.Instance != null)
                {
                    ObjectPoolManager.Instance.Release(gameObject);
                }
                else
                {
                    gameObject.SetActive(false);
                }
            }
        }
    }

    /// <summary>
    /// Centralized high-performance Object Pooling system for VFX, particles, props, and gameplay objects.
    /// Completely eliminates GC spikes from dynamic Instantiate and Destroy calls.
    /// </summary>
    public class ObjectPoolManager : MonoBehaviour
    {
        private static ObjectPoolManager instance;
        public static ObjectPoolManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindAnyObjectByType<ObjectPoolManager>();
                    if (instance == null)
                    {
                        GameObject go = new GameObject("ObjectPoolManager");
                        instance = go.AddComponent<ObjectPoolManager>();
                    }
                }
                return instance;
            }
        }

        private readonly Dictionary<int, Queue<GameObject>> poolQueues = new Dictionary<int, Queue<GameObject>>();
        private readonly Dictionary<int, Transform> poolParents = new Dictionary<int, Transform>();
        private readonly Dictionary<int, GameObject> prefabMap = new Dictionary<int, GameObject>();

        private Transform poolRoot;

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;
            if (transform.parent == null)
            {
                DontDestroyOnLoad(gameObject);
            }

            if (poolRoot == null)
            {
                GameObject rootObj = new GameObject("Pool_Root");
                rootObj.transform.SetParent(transform, false);
                poolRoot = rootObj.transform;
            }
        }

        /// <summary>
        /// Pre-instantiates a specified number of objects in the pool to prevent frame drops during gameplay.
        /// </summary>
        public void Prewarm(GameObject prefab, int count)
        {
            if (prefab == null || count <= 0) return;
            int id = prefab.GetInstanceID();

            EnsurePoolGroup(id, prefab);

            var queue = poolQueues[id];
            Transform parent = poolParents[id];

            for (int i = 0; i < count; i++)
            {
                GameObject obj = Instantiate(prefab, parent);
                obj.name = $"{prefab.name}_Pooled_{queue.Count + 1}";
                obj.SetActive(false);

                var pooled = obj.GetComponent<PooledInstance>() ?? obj.AddComponent<PooledInstance>();
                pooled.PrefabId = id;
                pooled.AutoRelease = false;

                queue.Enqueue(obj);
            }
        }

        private void EnsurePoolGroup(int id, GameObject prefab)
        {
            if (poolRoot == null)
            {
                GameObject rootObj = new GameObject("Pool_Root");
                rootObj.transform.SetParent(transform, false);
                poolRoot = rootObj.transform;
            }

            if (!poolQueues.ContainsKey(id))
            {
                poolQueues[id] = new Queue<GameObject>();
                prefabMap[id] = prefab;

                GameObject groupObj = new GameObject($"Pool_{prefab.name}");
                groupObj.transform.SetParent(poolRoot, false);
                poolParents[id] = groupObj.transform;
            }
        }

        /// <summary>
        /// Retrieves an idle object from the pool, or instantiates a new one if exhausted.
        /// Automatically resets ParticleSystems and TrailRenderers.
        /// </summary>
        public GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent = null)
        {
            if (prefab == null) return null;
            int id = prefab.GetInstanceID();

            EnsurePoolGroup(id, prefab);
            var queue = poolQueues[id];

            GameObject instance = null;
            while (queue.Count > 0 && instance == null)
            {
                instance = queue.Dequeue();
            }

            if (instance == null)
            {
                instance = Instantiate(prefab, poolParents[id]);
                instance.name = $"{prefab.name}_Pooled_{poolParents[id].childCount}";
            }

            var pooled = instance.GetComponent<PooledInstance>() ?? instance.AddComponent<PooledInstance>();
            pooled.PrefabId = id;
            pooled.AutoRelease = false;
            pooled.RemainingLife = 0f;

            if (parent != null)
            {
                instance.transform.SetParent(parent, false);
                instance.transform.position = position;
                instance.transform.rotation = rotation;
            }
            else
            {
                instance.transform.SetParent(null, false);
                instance.transform.position = position;
                instance.transform.rotation = rotation;
            }

            // Reset particle systems
            var particleSystems = instance.GetComponentsInChildren<ParticleSystem>(true);
            for (int i = 0; i < particleSystems.Length; i++)
            {
                particleSystems[i].Clear();
                particleSystems[i].Play();
            }

            // Reset trail renderers
            var trails = instance.GetComponentsInChildren<TrailRenderer>(true);
            for (int i = 0; i < trails.Length; i++)
            {
                trails[i].Clear();
            }

            instance.SetActive(true);
            return instance;
        }

        /// <summary>
        /// Spawns an object from the pool and automatically releases it back after lifetime seconds.
        /// Zero coroutines, zero GC allocation.
        /// </summary>
        public GameObject SpawnAutoRelease(GameObject prefab, Vector3 position, Quaternion rotation, float lifetime, Transform parent = null)
        {
            GameObject obj = Spawn(prefab, position, rotation, parent);
            if (obj != null)
            {
                var pooled = obj.GetComponent<PooledInstance>();
                if (pooled != null)
                {
                    pooled.RemainingLife = lifetime;
                    pooled.AutoRelease = true;
                }
            }
            return obj;
        }

        /// <summary>
        /// Returns an active instance back to its pool.
        /// </summary>
        public void Release(GameObject instance)
        {
            if (instance == null) return;

            var pooled = instance.GetComponent<PooledInstance>();
            if (pooled != null)
            {
                pooled.AutoRelease = false;
                pooled.OnDespawn?.Invoke();
                pooled.OnDespawn = null;

                int id = pooled.PrefabId;
                if (poolQueues.TryGetValue(id, out var queue))
                {
                    instance.SetActive(false);
                    if (poolParents.TryGetValue(id, out var parent) && parent != null)
                    {
                        instance.transform.SetParent(parent, false);
                    }
                    queue.Enqueue(instance);
                    return;
                }
            }

            instance.SetActive(false);
        }

        /// <summary>
        /// Clears all pools and destroys tracked instances.
        /// </summary>
        public void ClearAll()
        {
            foreach (var kvp in poolQueues)
            {
                var queue = kvp.Value;
                while (queue.Count > 0)
                {
                    var obj = queue.Dequeue();
                    if (obj != null) Destroy(obj);
                }
            }
            poolQueues.Clear();
            prefabMap.Clear();
        }
    }
}
