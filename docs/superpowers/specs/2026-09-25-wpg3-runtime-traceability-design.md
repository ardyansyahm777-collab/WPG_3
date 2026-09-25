# WPG_3 Runtime Traceability — Design Spec (Opsi 1)

Tanggal: 2026-09-25
Status: Disetujui user (`oke jalankan opsi 1`)
Scope: Architectural ringan — additive only, tanpa ubah gameplay.

## 1. Masalah

- Hierarchy play-mode berisi objek yang tidak ada di edit-mode:
  `[GameManager]`, `[EventSystem]`, `[Main Camera]`, `Player`,
  `EnemyTemplate`, `ProjectileTemplate`, `[EnemySpawner]`,
  `[Pool] X`, `Row_*`, `Card_*`, tombol LevelUp.
- User mencoba edit di play-mode (tidak tersimpan) dan kesulitan
  menemukan sumber kode karena spawn tersebar via kode.
- `README.md` basi: judul placeholder, struktur folder beda dari aktual,
  tidak menjelaskan runtime-vs-edit.

Sumber yang dipetakan (manual graphify — python/graphify CLI tidak tersedia
di mesin ini, jadi pemetaan via baca kode):

- `Assets/Scripts/Core/SceneSetup.cs`: `EnsureGameManager`, `EnsureEventSystem`, `EnsureMainCamera`
- `Assets/Scripts/Player/GameplaySetup.cs`: `CreatePlayer`, `CreateEnemyTemplate`, `CreateProjectileTemplate`, `[EnemySpawner]` wiring
- `Assets/Scripts/Core/PrefabPool.cs`: `[Pool] prefab.name`, `Spawn/Despawn`
- `Assets/Scripts/Enemy/EnemySpawner.cs`: wave spawn via `PrefabPool.Spawn`
- `Assets/Scripts/UI/CharacterSelectController.cs`: `RenderCards` (`Card_*`), `AddRow/AddParagraph` (`Row_*` dari `StatRow`/`ParaRow` prefab)
- `Assets/Scripts/UI/LevelUpPopup.cs`: instantiate `choiceTemplate`
- `Assets/Editor/CharacterSelectBootstrap.cs` + `SceneBootstrap.cs`: build edit-mode (idempoten)

## 2. Keputusan desain

- Tidak mengubah alur spawn / gameplay / state machine.
- Tambah penanda sumber yang terlihat di Inspector saat play-mode
  (`RuntimeSpawnTag`) + tool log edit-mode/play-mode.
- Dokumentasikan peta di `RUNTIME_MAP.md` + perbaiki `README.md`.
- Patuhi `AGENTS.md`: satu class satu file, namespace `WpgGame.*`,
  subscribe `OnEnable`/`Unsubscribe OnDisable`, `linearVelocity`,
  TMP, Input asset bawaan, komunikasi via `GameEvents`, hormati `GameManager.State`.
- Jangan hand-edit YAML `.unity`/`.prefab`.

## 3. Perubahan

1. Baru: `Assets/Scripts/Core/RuntimeSpawnTag.cs`
   - `MonoBehaviour`, namespace `WpgGame.Core`.
   - Field: `SourceScript`, `PrefabPath`, `SceneName`, `SpawnedAt`.
   - Static `Tag(go, sourceScript, prefabPath)`: tambah-jika-belum-ada, isi field, `hideFlags = None`.
2. Baru: `Assets/Editor/RuntimeSourceLogger.cs`
   - Namespace `WpgGame.EditorTools`, asmdef `WPG_3.Editor` sudah mereferensikan `WPG_3.Scripts`.
   - Menu `Tools/WPG_3/Log Runtime Source`: list root objects + anak 2 level,
     kolom: nama, `RuntimeSpawnTag.SourceScript` bila ada, inferensi fallback
     (prefix `[`, `Row_`, `Card_`, `Template`, `[Pool]`), scene, active.
   - Read-only: tidak membuat/menghapus objek.
3. Patch additive (tambah 1 baris `Tag` tiap titik buat objek):
   - `SceneSetup.EnsureGameManager/EventSystem/MainCamera`
   - `GameplaySetup.CreatePlayer/EnemyTemplate/ProjectileTemplate` + `[EnemySpawner]` di `OnSceneLoaded`/`MenuBootstrap`
   - `PrefabPool.GetOrCreate/CreateInstance/Get`
   - `CharacterSelectController.AddRow/AddParagraph` (setelah `Instantiate`/buat GO)
   - `LevelUpPopup.HandleLevelUp` (setelah `Instantiate`)
4. Baru: `RUNTIME_MAP.md` di root — tabel: Hierarchy | Sumber kode | Prefab | Edit/Runtime | Catatan.
5. Update `README.md`: judul aktual, engine `6000.6.0f1`, struktur aktual,
   seksi "Runtime vs Edit-mode", cara pakai `Log Runtime Source`, link ke
   `RUNTIME_MAP.md`, `AGENTS.md`, `API_CONTRACTS.md`.

## 4. Non-goals

- Tidak sentralisasi spawner (itu Opsi 2).
- Tidak ubah prefab structure / styling.
- Tidak ubah API publik di `API_CONTRACTS.md`.

## 5. Validasi

- `unity.exe status` ready → `recompile` → `recompile_status` → `get_console_logs` bersih.
- Buka `Main Menu.unity`: `Tools/WPG_3/Validate Slice` → `[SliceCheck] ... PASS`.
- Buka `Main.unity`: Play 10 detik, cek Inspector objek runtime punya `RuntimeSpawnTag`, Stop — scene bersih (tanpa sisa runtime).
- `Tools/WPG_3/Log Runtime Source` jalan di edit + play, output terbaca.

## 6. Risiko

- Tambah `GetComponent/AddComponent` di titik panas (pool): overhead negligible (sekali per spawn), tidak di per-frame.
- `DontDestroyOnLoad` GameManager: tag hanya sekali saat create, aman.
