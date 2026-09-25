# WPG_3 — Prototipe Game 2D Top-Down Roguelike (Archero-like)

> **Aturan git dasar tetap berlaku** — selalu `git pull` sebelum mulai, dan `git push` setelah commit kecil. Pesan commit format: `[<role>] <aksi> <target>`.

## Ringkasan Proyek

Game top-down shooter survival roguelike untuk **PC (keyboard + mouse)** dan **Mobile (touch)**.

- **Engine**: Unity **6000.6.0f1** + URP 2D + Input System + TextMeshPro
- **Target build**: Android (touch) + Windows Standalone
- **Orientasi**: Landscape 16:9
- **Judul kerja**: WPG_3 (placeholder lama: "Bhagas Naksir Dipta")
- **Aset**: Sebagian besar placeholder shape 2D; art final menyusul
- **Scene**: `Main Menu.unity` (pilih hero) + `Main.unity` (gameplay, sengaja minimal — runtime bootstrap)

Dokumen terkait: `AGENTS.md` (aturan kerja + konvensi), `Assets/Scripts/API_CONTRACTS.md` (kontrak API — baca dulu sebelum ngoding), `RUNTIME_MAP.md` (peta hierarchy play-mode → sumber kode), `docs/superpowers/specs/2026-09-25-wpg3-runtime-traceability-design.md` (spec traceability).

## Struktur Folder (aktual)

```
Assets/
  Scenes/Main Menu.unity        # Pilih hero (MenuUI + CharacterSelect)
  Scenes/Main.unity             # Gameplay (sengaja minimal — runtime bootstrap)
  Settings/                     # URP asset + InputSystem_Actions (jangan buat asset baru)
  Scripts/
    WPG_3.Scripts.asmdef        # Asmdef runtime (namespace WpgGame.*)
    API_CONTRACTS.md            # KONTRAK API — BACA DULU SEBELUM NGODING
    Core/                       # GameManager, GameEvents, SceneSetup, PrefabPool, CameraFollow, RuntimeSpawnTag
    Player/                     # PlayerController, PlayerStats, AutoAimShooter, HeroSkill, CharacterData, GameplaySetup
    Combat/                     # HealthSystem, Projectile
    Enemy/                      # EnemyAI, EnemySpawner
    Progression/                # LevelUpSystem, UpgradeSystem, UpgradeOption, ExpGem, RewardRouter
    Economy/                    # GoldManager, GoldCoin
    UI/                         # HUD, LevelUpPopup, GameOverScreen, CharacterSelect*, MainMenuController, FloatingObject
  Editor/                       # WPG_3.Editor.asmdef + SceneBootstrap, CharacterSelectBootstrap, RuntimeSourceLogger, Builder tools
  Data/Characters/              # 4 hero (Aelindra unlocked, Bram/Circe/Dain locked)
  Data/UpgradeOptions/          # ScriptableObject upgrade samples
  Prefabs/                      # Player, Enemy, Projectile, GameManager, EventSystem, Systems
  Prefabs/UI/                   # HUD, LevelUpPopup, GameOverScreen, StatRow, ParaRow
```

## Tim & Peran (3 Programmer)

| # | Peran | Folder Eksklusif | Tanggung Jawab |
|---|---|---|---|
| 1 | **Gameplay & Combat** | `Player/`, `Combat/`, `Enemy/` | Player gerak + auto-shoot, projectile, musuh, spawner |
| 2 | **Progression & Systems** | `Progression/`, `Economy/`, `Core/` (kecuali yang sudah ditulis lead) | GameManager extensions, EXP, level-up, upgrade, gold |
| 3 | **UI, Input & Cross-Platform** | `UI/` | HUD, virtual joystick, popup level-up, game over, pakai Input Actions bawaan |

**Lead** (project owner): setup project, scene bootstrap, integrasi, build, validasi.

## Alur Kerja

1. **`git pull`** dulu.
2. **Baca** `Assets/Scripts/API_CONTRACTS.md` — pahami signature publik yang sudah disepakati.
3. **Kerjakan** di folder eksklusif kamu. Jangan masuk folder programmer lain kecuali baca referensi.
4. **Commit kecil** dengan format `[<role>] <aksi>`. Contoh: `[gameplay] tambah PlayerController`.
5. **Push**, lalu `git pull` lagi untuk pastikan tidak ada konflik.
6. Kalau ada perubahan API yang menyentuh programmer lain, kirim pesan dulu.

## Cara Menjalankan

1. Buka **Unity Hub** → Add project → pilih folder `D:\Me\Unity_Project\WPG_3`.
2. Pastikan **Editor version: 6000.6.0f1**.
3. Klik **Open** dan tunggu Unity selesai compile.
4. Buka scene `Assets/Scenes/Main Menu.unity` untuk alur pilih hero, atau `Assets/Scenes/Main.unity` untuk langsung gameplay.
5. Klik **Play** untuk tes.

## Runtime vs Edit-mode (penting!)

Scene sengaja minimal. Saat Play, objek dibuat otomatis via kode:

- `SceneSetup.cs` → `[GameManager]`, `[EventSystem]`, `[Main Camera]`
- `GameplaySetup.cs` → `Player`, `EnemyTemplate`, `ProjectileTemplate`, `[EnemySpawner]`
- `PrefabPool.cs` → `[Pool] ...` + musuh/proyektil hidup
- `CharacterSelectController.cs` → `Card_*`, `Row_*` (dari prefab `StatRow`/`ParaRow`)
- `LevelUpPopup.cs` → tombol pilihan upgrade

**Isi `DetailContent`/grid SELALU kosong di edit-mode** — baris lahir dari kode + prefab, musnah saat Stop. Jangan edit manual di play-mode (tidak tersimpan).

Cara melacak objek asing saat Play:

1. Klik objek di Hierarchy → Inspector → komponen `RuntimeSpawnTag` → baca `SourceScript` / `PrefabPath`.
2. Atau jalankan `Tools/WPG_3/Log Runtime Source` → baca Console.
3. Ubah sumbernya (kode/prefab di `RUNTIME_MAP.md`), bukan objek play-mode-nya.
4. Validasi: `Tools/WPG_3/Validate Slice` → `[SliceCheck] ... PASS`, scene tersimpan bersih (panel nonaktif, tanpa sisa objek runtime).

## Tooling Editor

- `Tools/WPG_3/Bootstrap Main Scene` — pastikan `Main.unity` punya GameManager.
- `Tools/WPG_3/Bootstrap Gameplay Objects` — buat Player/template/spawner di scene aktif (edit-mode).
- `Tools/WPG_3/Build Character Select UI` — bangun hierarchy Character Select (idempoten).
- `Tools/WPG_3/Log Runtime Source` — petakan hierarchy terlihat → sumber kode (read-only).
- `Tools/WPG_3/Validate Slice` (+ `Tab/Go/Main`) — cek menu 4 kartu, 20/3/1 baris, alur Main.

## Catatan Penting

- Scene auto-bootstrap `GameManager`, `EventSystem`, dan `Main Camera` lewat `SceneSetup.cs` (runtime). Tidak perlu drag prefab manual.
- `InputSystem_Actions.inputactions` sudah ter-setup: `Player/Move`, `Player/Attack`, `Player/Skill1|2|3` (= 1/2/3), `Player/Previous|Next` (= Q/E), `UI/Submit`, `UI/Cancel`. Pakai ini — jangan buat asset baru.
- Hero terpilih: `PlayerPrefs "wpg3_hero"` + `GameplaySetup.PendingCharacter` (referensi langsung antar-scene, tanpa folder Resources).
- Jangan hand-edit YAML `.unity`/`.prefab`: drive Editor via `unity-cli` atau skrip bootstrap (`Tools/WPG_3/...`).

## Lisensi

MIT — lihat `LICENSE`.
