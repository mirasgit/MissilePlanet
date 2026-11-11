using System.Collections.Generic;
using UnityEngine;

namespace MissilePlanet
{
    /// <summary>
    /// Tiny GameObject pool with prewarm support and a retention cap so very late waves
    /// do not keep every historical projectile alive in memory forever.
    /// </summary>
    public sealed class SimplePool : MonoBehaviour
    {
        [SerializeField, Min(16)] private int _maxRetainedPerPrefab = 160;

        private readonly Dictionary<int, Queue<GameObject>> _available = new Dictionary<int, Queue<GameObject>>();
        private readonly Dictionary<int, int> _instanceToPrefab = new Dictionary<int, int>();

        public void Prewarm(GameObject prefab, int count)
        {
            if (prefab == null || count <= 0)
                return;

            int prefabId = prefab.GetInstanceID();
            if (!_available.TryGetValue(prefabId, out Queue<GameObject> queue))
            {
                queue = new Queue<GameObject>();
                _available[prefabId] = queue;
            }

            int desired = Mathf.Min(Mathf.Max(0, count), Mathf.Max(16, _maxRetainedPerPrefab));
            while (queue.Count < desired)
            {
                GameObject instance = Instantiate(prefab, transform);
                instance.SetActive(false);
                _instanceToPrefab[instance.GetInstanceID()] = prefabId;
                queue.Enqueue(instance);
            }
        }

        public GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation)
        {
            if (prefab == null)
                return null;

            int prefabId = prefab.GetInstanceID();
            GameObject instance = null;

            if (_available.TryGetValue(prefabId, out Queue<GameObject> queue))
            {
                while (queue.Count > 0 && instance == null)
                    instance = queue.Dequeue();
            }

            if (instance == null)
            {
                instance = Instantiate(prefab);
                _instanceToPrefab[instance.GetInstanceID()] = prefabId;
            }

            instance.transform.SetParent(null, false);
            instance.transform.SetPositionAndRotation(position, rotation);
            instance.SetActive(true);
            return instance;
        }

        public T Spawn<T>(GameObject prefab, Vector3 position, Quaternion rotation) where T : Component
        {
            GameObject instance = Spawn(prefab, position, rotation);
            return instance == null ? null : instance.GetComponent<T>();
        }

        public void Despawn(GameObject instance)
        {
            if (instance == null)
                return;

            int instanceId = instance.GetInstanceID();
            if (!_instanceToPrefab.TryGetValue(instanceId, out int prefabId))
            {
                Destroy(instance);
                return;
            }

            if (!_available.TryGetValue(prefabId, out Queue<GameObject> queue))
            {
                queue = new Queue<GameObject>();
                _available[prefabId] = queue;
            }

            if (queue.Count >= Mathf.Max(16, _maxRetainedPerPrefab))
            {
                _instanceToPrefab.Remove(instanceId);
                Destroy(instance);
                return;
            }

            instance.SetActive(false);
            instance.transform.SetParent(transform, false);
            queue.Enqueue(instance);
        }
    }
}
