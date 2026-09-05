#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using WpgGame.Progression;

namespace WpgGame.EditorTools
{
    /// <summary>
    /// Auto-generate 5 UpgradeOption default ke Assets/Data/UpgradeOptions.
    /// Jalankan manual: Tools > WPG_3 > Create Default Upgrade Options,
    /// atau dari CLI/batch: -executeMethod WpgGame.EditorTools.UpgradeOptionCreator.CreateAll
    /// Idempotent: tidak menimpa asset yang sudah ada.
    /// </summary>
    public static class UpgradeOptionCreator
    {
        private const string RootFolder = "Assets/Data";
        private const string TargetFolder = "Assets/Data/UpgradeOptions";

        [MenuItem("Tools/WPG_3/Create Default Upgrade Options")]
        public static void CreateAll()
        {
            EnsureFolder("Assets", "Data");
            EnsureFolder(RootFolder, "UpgradeOptions");

            CreateOption("damage_plus",      "Damage +1",       "Damage bertambah 1 per tembakan.",       UpgradeType.Damage,    1f);
            CreateOption("move_speed_plus",  "Move Speed +0.2", "Kecepatan gerak bertambah 0.2.",         UpgradeType.MoveSpeed, 0.2f);
            CreateOption("fire_rate_plus",   "Fire Rate +0.3",  "Tembakan per detik bertambah 0.3.",      UpgradeType.FireRate,  0.3f);
            CreateOption("max_health_plus",  "Max Health +20",  "HP maksimum bertambah 20.",              UpgradeType.MaxHealth, 20f);
            CreateOption("multi_shot_plus",  "MultiShot +1",    "Proyektil per tembakan bertambah 1.",    UpgradeType.MultiShot, 1f);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[UpgradeOptionCreator] 5 default UpgradeOption siap di " + TargetFolder);
        }

        private static void EnsureFolder(string parent, string child)
        {
            if (!AssetDatabase.IsValidFolder(parent + "/" + child))
            {
                AssetDatabase.CreateFolder(parent, child);
            }
        }

        private static void CreateOption(string id, string displayName, string description, UpgradeType type, float value)
        {
            string path = TargetFolder + "/" + id + ".asset";

            if (AssetDatabase.LoadAssetAtPath<UpgradeOption>(path) != null)
            {
                Debug.Log($"[UpgradeOptionCreator] {path} sudah ada, dilewati (idempotent).");
                return;
            }

            var option = ScriptableObject.CreateInstance<UpgradeOption>();
            option.Id = id;
            option.DisplayName = displayName;
            option.Description = description;
            option.Icon = null;
            option.Type = type;
            option.Value = value;

            AssetDatabase.CreateAsset(option, path);
            Debug.Log($"[UpgradeOptionCreator] Buat {path}.");
        }
    }
}
#endif