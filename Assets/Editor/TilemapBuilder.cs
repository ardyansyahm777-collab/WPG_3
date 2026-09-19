using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.Tilemaps;

namespace WpgGame.Editor
{
    /// <summary>
    /// Menu tool: Tools > WPG_3 > Setup Jalan Tilemap
    /// Membuat Grid + Tilemap ground di scene menggunakan Sprite dari Assets/Gambar/Environtment/Texture Jalan.png.
    /// Ground = TANPA collider (player jalan di atasnya). Jangan tambah TilemapCollider2D/Composite di sini.
    /// </summary>
    public static class TilemapBuilder
    {
        private const string TexturePath = "Assets/Gambar/Environtment/Texture Jalan.png";
        private const string TileAssetDir = "Assets/Data/Tiles/";
        private const string TileAssetPath = "Assets/Data/Tiles/JalanTile.asset";

        [MenuItem("Tools/WPG_3/Setup Jalan Tilemap")]
        public static void SetupJalanTilemap()
        {
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(TexturePath);
            if (tex == null)
            {
                EditorUtility.DisplayDialog("Error", $"Texture tidak ditemukan: {TexturePath}", "OK");
                return;
            }

            // Ambil Sprite yang sudah diimport dari texture (jangan Sprite.Create dari Texture2D
            // yang isReadable=false — itu pasti gagal). Texture ini bertipe Sprite (Multiple)
            // dengan sub-asset "Texture Jalan_0".
            Sprite roadSprite = null;
            var subAssets = AssetDatabase.LoadAllAssetsAtPath(TexturePath);
            foreach (var a in subAssets)
            {
                if (a is Sprite s)
                {
                    roadSprite = s;
                    break;
                }
            }
            if (roadSprite == null)
            {
                EditorUtility.DisplayDialog("Error",
                    $"Tidak ada Sprite di {TexturePath}.\nUbah Texture Type menjadi Sprite (Single/Multiple) lalu Apply.",
                    "OK");
                return;
            }

            // Load atau buat Tile asset. Perbaiki juga asset lama yang sprite-nya null
            // (akibat Sprite.Create dari texture non-readable).
            Tile jalanTile = AssetDatabase.LoadAssetAtPath<Tile>(TileAssetPath);
            if (jalanTile == null)
            {
                Directory.CreateDirectory(TileAssetDir);
                var newTile = ScriptableObject.CreateInstance<Tile>();
                newTile.sprite = roadSprite;
                AssetDatabase.CreateAsset(newTile, TileAssetPath);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                jalanTile = AssetDatabase.LoadAssetAtPath<Tile>(TileAssetPath);
            }
            else if (jalanTile.sprite == null || jalanTile.sprite != roadSprite)
            {
                jalanTile.sprite = roadSprite;
                EditorUtility.SetDirty(jalanTile);
                AssetDatabase.SaveAssets();
            }
            if (jalanTile == null)
            {
                EditorUtility.DisplayDialog("Error", $"Gagal membuat Tile: {TileAssetPath}", "OK");
                return;
            }

            // Buat Grid di root scene kalau belum ada. Grid WAJIB punya komponen Grid
            // (sebelumnya hanya GameObject kosong bernama "Grid").
            GameObject gridGo = GameObject.Find("Grid");
            if (gridGo == null)
            {
                gridGo = new GameObject("Grid");
                Undo.RegisterCreatedObjectUndo(gridGo, "Create Ground Grid");
                gridGo.transform.SetAsFirstSibling();
            }
            if (gridGo.GetComponent<Grid>() == null)
            {
                Undo.AddComponent<Grid>(gridGo);
            }

            // Buat Tilemap anak Grid kalau belum ada (cari termasuk inactive).
            Tilemap tilemap = gridGo.GetComponentInChildren<Tilemap>(true);
            GameObject tilemapGo;
            if (tilemap == null)
            {
                tilemapGo = new GameObject("Tilemap");
                Undo.RegisterCreatedObjectUndo(tilemapGo, "Create Ground Tilemap");
                tilemapGo.transform.SetParent(gridGo.transform, false);
                tilemap = Undo.AddComponent<Tilemap>(tilemapGo);
                Undo.AddComponent<TilemapRenderer>(tilemapGo);
            }
            else
            {
                tilemapGo = tilemap.gameObject;
            }

            // Ground tidak boleh menghalangi gerak: bersihkan sisa collider dari run lama.
            // (TilemapCollider2D + CompositeCollider2D butuh Rigidbody2D dan bikin jalan jadi tembok.)
            var oldTileCollider = tilemapGo.GetComponent<TilemapCollider2D>();
            if (oldTileCollider != null) Undo.DestroyObjectImmediate(oldTileCollider);
            var oldComposite = tilemapGo.GetComponent<CompositeCollider2D>();
            if (oldComposite != null) Undo.DestroyObjectImmediate(oldComposite);
            var oldBody = tilemapGo.GetComponent<Rigidbody2D>();
            if (oldBody != null) Undo.DestroyObjectImmediate(oldBody);

            var tilemapRenderer = tilemapGo.GetComponent<TilemapRenderer>();
            // "Background" tidak ada di TagManager (hanya "Default") — pakai Default + order negatif.
            tilemapRenderer.sortingLayerName = "Default";
            tilemapRenderer.sortingOrder = -10;

            int width = 20;
            int height = 20;

            // Kosongkan tilemap yang ada
            tilemap.ClearAllTiles();

            // Fill area CENTERED di sekitar origin agar player di (0,0) ada di tengah jalan,
            // bukan di pojok (sebelumnya 0..19).
            int x0 = -width / 2;
            int y0 = -height / 2;
            for (int x = x0; x < x0 + width; x++)
            {
                for (int y = y0; y < y0 + height; y++)
                {
                    tilemap.SetTile(new Vector3Int(x, y, 0), jalanTile);
                }
            }

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

            EditorUtility.DisplayDialog("Selesai", $"Tilemap jalan dibuat: {width}x{height} tile (centered), tile asset: {TileAssetPath}", "OK");

            // Kamera sengaja TIDAK dipindah: GameplaySetup / camera-follow yang mengatur framing.
            // (Versi lama memindah Main Camera ke tengah tile sehingga follow player rusak.)

            Debug.Log($"[TilemapBuilder] Jalan tilemap selesai: {width}x{height} tile (centered)");
        }
    }
}

