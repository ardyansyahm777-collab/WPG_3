using System.Collections.Generic;
using UnityEngine;

namespace WpgGame.Core
{
    /// <summary>
    /// Pool generik per-prefab untuk menekan Instantiate/Destroy saat wave ramai.
    /// Pemakaian: <c>PrefabPool.Spawn(prefab, pos, rot)</c> sebagai pengganti
    /// <c>Instantiate</c>, dan <c>PooledObject.Release()</c> sebagai pengganti
    /// <c>Destroy</c>. Pool terikat scene (TANPA DontDestroyOnLoad) sehingga Restart
    /// (reload scene) otomatis bersih; registry statis memvalidasi pool yang sudah
    /// hancur dan membuatnya ulang bila perlu.
    /// </summary>
    public class PrefabPool : MonoBehaviour
    {
        [Tooltip("Prefab yang di-pool oleh pool ini.")]
        public GameObject Prefab;

        [Tooltip("Jumlah instance yang disiapkan di awal (hindari hitch saat wave pertama).")]
        public int InitialSize = 12;

        [Tooltip("Maksimum instance menganggur yang disimpan. Kelebihan di-Destroy.")]
        public int MaxSize = 120;

        /// <summary>Total Instantiate sejak sesi dimulai (diagnostik: harus plateau saat pool hangat).</summary>
        public static int TotalInstantiated { get; private set; }

        private readonly Stack<GameObject> _free = new Stack<GameObject>();
        private static readonly Dictionary<GameObject, PrefabPool> _pools = new Dictionary<GameObject, PrefabPool>();

        /// <summary>Ambil instance aktif dari pool (pengganti Instantiate).</summary>
        public static GameObject Spawn(GameObject prefab, Vector3 pos, Quaternion rot)
        {
            var pool = GetOrCreate(prefab);
            if (pool == null) return null;
            return pool.Get(pos, rot);
        }

        /// <summary>Kembalikan instance (pengganti Destroy). Aman untuk objek non-pool juga.</summary>
        public static void Despawn(GameObject instance)
        {
            if (instance == null) return;
            var pooled = instance.GetComponent<PooledObject>();
            if (pooled != null) pooled.Release();
            else Destroy(instance);
        }

        /// <summary>Ambil/buat pool untuk prefab. Ukuran custom hanya dipakai saat pool pertama dibuat.</summary>
        public static PrefabPool GetOrCreate(GameObject prefab, int initialSize = 12, int maxSize = 120)
        {
            if (prefab == null) return null;
            if (_pools.TryGetValue(prefab, out var pool) && pool != null) return pool;

            var root = new GameObject("[Pool] " + prefab.name);
            pool = root.AddComponent<PrefabPool>();
            pool.Prefab = prefab;
            pool.InitialSize = Mathf.Max(0, initialSize);
            pool.MaxSize = Mathf.Max(1, maxSize);
            pool.Prewarm();
            _pools[prefab] = pool;
            return pool;
        }

        private void Prewarm()
        {
            for (int i = 0; i < InitialSize; i++)
                _free.Push(CreateInstance());
        }

        private GameObject CreateInstance()
        {
            var go = Instantiate(Prefab);
            TotalInstantiated++;
            go.name = Prefab.name;
            var pooled = go.GetComponent<PooledObject>();
            if (pooled == null) pooled = go.AddComponent<PooledObject>();
            pooled.HomePool = this;
            go.transform.SetParent(transform, false);
            go.SetActive(false);
            return go;
        }

        /// <summary>Ambil satu instance aktif di posisi/rotasi yang diminta.</summary>
        public GameObject Get(Vector3 pos, Quaternion rot)
        {
            GameObject go = _free.Count > 0 ? _free.Pop() : CreateInstance();
            go.transform.SetParent(null);
            go.transform.SetPositionAndRotation(pos, rot);
            go.SetActive(true);
            return go;
        }

        /// <summary>Kembalikan instance menganggur (reset velocity, nonaktifkan).</summary>
        public void Release(GameObject go)
        {
            if (go == null) return;
            var rb = go.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.linearVelocity = Vector2.zero;
                rb.angularVelocity = 0f;
            }
            go.SetActive(false);
            if (_free.Count < MaxSize)
            {
                go.transform.SetParent(transform, false);
                _free.Push(go);
            }
            else
            {
                Destroy(go);
            }
        }

        /// <summary>Jumlah instance menganggur saat ini (diagnostik).</summary>
        public int FreeCount => _free.Count;
    }
}
