# AGENTS.md — WPG_3

Prototipe game 2D top-down roguelike (Archero-like). Unity **6000.6.0f1** +
URP 2D + Input System + TextMeshPro. Target build: Android (touch) + Windows
Standalone, landscape 16:9. Scene: `Main Menu.unity` (pilih hero) +
`Main.unity` (gameplay, sengaja minimal — runtime bootstrap).

## Kontrak utama

- Baca `Assets/Scripts/API_CONTRACTS.md` sebelum menyentuh API publik.
  Signature di sana tidak boleh berubah tanpa diskusi tim.
- Peta folder (`Assets/Scripts/`, namespace `WpgGame.*`):
  `Core/` (GameManager, GameEvents, SceneSetup) · `Player/` (PlayerController,
  PlayerStats, AutoAimShooter, HeroSkill, CharacterData) · `Combat/`
  `Enemy/` (Prog 1) · `Progression/` `Economy/` (Prog 2) · `UI/`
  (HUD, LevelUpPopup, GameOverScreen, CharacterSelect*) (Prog 3) ·
  `Editor/` (tooling) · `Data/UpgradeOptions/` + `Data/Characters/` (4 hero:
  Aelindra unlocked, Bram/Circe/Dain locked) · `Prefabs/`.
- Hero terpilih: `PlayerPrefs "wpg3_hero"` + `GameplaySetup.PendingCharacter`
  (referensi langsung antar-scene, tanpa folder Resources).

## Aturan kerja

- `git pull` sebelum mulai; commit kecil lalu push. Format pesan:
  `[<role>] <aksi> <target>` — mis. `[gameplay] tambah PlayerController`.
- Kerjakan hanya di folder eksklusif role-mu (lihat `.opencode/agents/`);
  folder lain hanya dibaca. Lintas-folder = wewenang lead. Satu class satu
  file — duplikat nama class antar-folder = compile break.
- Peran subagent (dipanggil lead via Task): `@gameplay` = Player/Combat/Enemy,
  `@progression` = Progression/Economy/Core, `@ui` = UI.

## Konvensi teknis (wajib)

- Subscribe event di `OnEnable`, unsubscribe di `OnDisable`.
- `Rigidbody2D.linearVelocity` (bukan `velocity`). UI text TextMeshPro;
  event UI via `UnityEngine.EventSystems`. Jangan pakai nama enum TMP ala
  TextAnchor (`Left`/`TopLeft`, bukan `MiddleLeft`/`UpperLeft`);
  `enableWordWrapping` obsolete → `textWrappingMode`.
- Input via asset `Assets/Settings/InputSystem_Actions.inputactions` —
  jangan buat asset baru (tambah binding di asset yang sama bila perlu):
  `Player/Move`, `Player/Attack`, `Player/Skill1|2|3` (= tombol 1/2/3),
  `Player/Previous|Next` (= Q/E), `UI/Submit`, `UI/Cancel`.
- Komunikasi antar-sistem lewat `GameEvents`, bukan referensi antar manager.
- Hormati `GameManager.State` (`Playing`/`Paused`/`LevelUp`/`GameOver`):
  gerak, tembak, spawn, dan cast skill freeze saat bukan `Playing`.

## Unity Editor live (skill `unity-cli`)

- Panggil CLI via path penuh `C:\Users\Pongo\AppData\Local\Unity\bin\unity.exe`
  (bare `unity` gagal `spawn EPERM` di sandbox ini). Cek dulu
  `status --format json` (state harus `ready`; port bisa berubah).
- Jangan hand-edit YAML `.unity`/`.prefab`: drive Editor (`command`,
  `eval`, `save_scene`) atau tulis skrip bootstrap editor
  (`Tools/WPG_3/...`, pola `GameplaySetup`/`CharacterSelectBootstrap`).
- Batasan `eval`: string C# harus escape (`\"`); `GameObject.Find` tidak
  menemukan objek inactive (telusuri `Transform.Find` dari root aktif);
  `eval` jalan di konteks edit-mode (Destroy biasa error → pakai
  DestroyImmediate bila `!Application.isPlaying`); API editor
  (Build/AssetDatabase/save) ditolak saat play mode.
- Loop hapus child hierarchy harus mundur by-index (forward `foreach` +
  DestroyImmediate me-skip child). Validasi: `recompile` →
  `recompile_status` → `get_console_logs`.

## uGUI (skill `ui-ugui` untuk Canvas/prefab)

- Set flag layout eksplisit setiap Build (default tidak bisa diandalkan):
  `childControlWidth/Height`, `childForceExpandWidth/Height`.
- Baris list runtime: tinggi dikunci (`LayoutElement` min+preferred sama),
  satu TMP full-stretch + `Ellipsis`, tanpa nested layout group.
- Tooling validasi: `Tools/WPG_3/Validate Slice` → `[SliceCheck] ... PASS`;
  scene disimpan bersih (panel nonaktif, tanpa sisa objek runtime).

## Skills proyek

- `wpg3-architecture` (semua role) · `wpg3-gameplay-combat` (Prog 1) ·
  `wpg3-progression-systems` (Prog 2) · `wpg3-ui-workflow` (Prog 3) ·
  `wpg3-release` (lead: validasi run penuh spawn→EXP→level-up→game
  over→restart bersih, build, `versi 0.x` hanya setelah lolos).
