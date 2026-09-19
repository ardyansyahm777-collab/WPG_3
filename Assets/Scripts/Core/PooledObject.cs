using UnityEngine;

namespace WpgGame.Core
{
    /// <summary>
    /// Penanda objek milik <see cref="PrefabPool"/>. Dipasang otomatis saat pool membuat instance.
    /// <see cref="Release"/> mengembalikan ke pool; bila tidak punya pool (spawn manual di luar
    /// pool, mis. alat editor) fallback ke <c>Destroy</c> agar perilaku lama tetap aman.
    /// </summary>
    public class PooledObject : MonoBehaviour
    {
        [Tooltip("Pool asal. Diisi otomatis oleh PrefabPool; jangan diisi manual.")]
        public PrefabPool HomePool;

        /// <summary>
        /// Kembalikan ke pool (atau Destroy bila tidak punya pool).
        /// Aman dipanggil dari callback fisika: hanya SetActive(false), bukan Destroy langsung.
        /// </summary>
        public void Release()
        {
            if (HomePool != null) HomePool.Release(gameObject);
            else Destroy(gameObject);
        }
    }
}
