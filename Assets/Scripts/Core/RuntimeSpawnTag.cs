using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace WpgGame.Core
{
    /// <summary>
    /// Penanda sumber objek runtime: terlihat di Inspector saat play-mode
    /// agar hierarchy yang muncul tiba-tiba bisa dilacak ke kode/prefab.
    /// Additive only: tidak mengubah perilaku, hanya metadata.
    /// Lihat RUNTIME_MAP.md di root repo.
    /// </summary>
    public class RuntimeSpawnTag : MonoBehaviour
    {
        [Tooltip("Script/metode sumber, mis. WpgGame.Core.SceneSetup.EnsureGameManager.")]
        public string SourceScript;

        [Tooltip("Prefab sumber bila ada, mis. Assets/Prefabs/UI/StatRow.prefab. Kosong = dibuat via kode.")]
        public string PrefabPath;

        [Tooltip("Scene saat di-spawn.")]
        public string SceneName;

        [Tooltip("Waktu spawn (Time.time saat Tag dipanggil).")]
        public float SpawnedAt;

        /// <summary>
        /// Tempel penanda ke GameObject (tambah-jika-belum-ada). Aman dipanggil berulang.
        /// </summary>
        public static RuntimeSpawnTag Tag(GameObject go, string sourceScript, string prefabPath = "")
        {
            if (go == null) return null;
            var tag = go.GetComponent<RuntimeSpawnTag>();
            if (tag == null) tag = go.AddComponent<RuntimeSpawnTag>();
            tag.SourceScript = sourceScript ?? string.Empty;
            tag.PrefabPath = prefabPath ?? string.Empty;
            try
            {
                tag.SceneName = go.scene.IsValid() ? go.scene.name : SceneManager.GetActiveScene().name;
            }
            catch
            {
                tag.SceneName = string.Empty;
            }
            try
            {
                tag.SpawnedAt = Application.isPlaying ? Time.time : 0f;
            }
            catch
            {
                tag.SpawnedAt = 0f;
            }
            return tag;
        }
    }
}
