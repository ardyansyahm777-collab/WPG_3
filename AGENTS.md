# AGENTS.md — WPG_3

Prototipe game 2D top-down roguelike (Archero-like). Unity 6000.6.x + URP 2D +
Input System + TextMeshPro. Target build: Android (touch) + Windows Standalone,
landscape 16:9.

## Kontrak utama

- Baca `Assets/Scripts/API_CONTRACTS.md` sebelum ngoding. Signature publik di
  sana tidak boleh berubah tanpa diskusi tim.
- Peta folder (`Assets/Scripts/`, namespace `WpgGame.*`):
  `Core/` (GameManager, GameEvents, SceneSetup) · `Player/` `Combat/` `Enemy/`
  (Prog 1) · `Progression/` `Economy/` (Prog 2) · `UI/` (Prog 3) ·
  `Editor/` (tooling) · `Data/UpgradeOptions/` (ScriptableObject) · `Prefabs/`.

## Aturan kerja

- `git pull` sebelum mulai; commit kecil lalu push. Format pesan:
  `[<role>] <aksi> <target>` — mis. `[gameplay] tambah PlayerController`.
- Kerjakan hanya di folder eksklusif role-mu; folder lain hanya dibaca sebagai
  referensi. Perubahan lintas-folder adalah wewenang lead.

## Konvensi teknis (wajib)

- Subscribe event di `OnEnable`, unsubscribe di `OnDisable`.
- Rigidbody2D pakai `linearVelocity` (bukan `velocity`, deprecated di Unity 6).
- UI text pakai TextMeshPro; event UI via `UnityEngine.EventSystems`; input via
  asset `Assets/Settings/InputSystem_Actions.inputactions`
  (`Player/Move`, `Player/Attack`) — jangan buat asset baru.
- Komunikasi antar-sistem lewat event bus `WpgGame.Core.GameEvents`,
  bukan referensi langsung antar manager.
- Hormati `GameManager.State` (`Playing`/`Paused`/`LevelUp`/`GameOver`):
  gameplay (gerak, tembak, spawn) freeze saat state bukan `Playing`.

## Skills proyek

- `wpg3-architecture` — peta arsitektur & alur runtime, untuk semua role.
- `wpg3-gameplay-combat` — pola Player/Combat/Enemy (Prog 1).
- `wpg3-progression-systems` — pola EXP/upgrade/gold (Prog 2).
- `wpg3-ui-workflow` — pola HUD/popup/game-over (Prog 3).
- `wpg3-release` — integrasi, validasi run penuh, build (lead).
