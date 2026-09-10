# WPG_3 — Prototipe Game 2D Top-Down Roguelike (Archero-like)

> **Aturan git dasar tetap berlaku** — selalu `git pull` sebelum mulai, dan `git push` setelah commit kecil. Pesan commit format: `[<role>] <aksi> <target>`.

## Ringkasan Proyek

Game top-down shooter survival roguelike untuk **PC (keyboard + mouse)** dan **Mobile (touch)**.

- **Engine**: Unity 6.6.0f1 + URP 2D + Input System + TextMeshPro
- **Target build**: Android (touch) + Windows Standalone
- **Orientasi**: Landscape 16:9
- **Judul sementara**: "Bhagas Naksir Dipta" (placeholder)
- **Aset**: Semua placeholder shape 2D untuk saat ini; art menyusul

## Struktur Folder

```
Assets/
  Scenes/Main.unity          # Scene utama (sudah jadi entry scene)
  Settings/                  # URP asset + Input System actions (jangan diedit kecuali perlu)
  Scripts/
    WPG_3.Scripts.asmdef     # Asmdef runtime
    Core/                    # GameManager, GameEvents, SceneSetup (SINGLE SOURCE OF TRUTH)
    Player/                  # PlayerController, AutoAimShooter, PlayerStats  (Prog 1)
    Combat/                  # HealthSystem, Projectile                        (Prog 1)
    Enemy/                   # EnemyAI, EnemySpawner                           (Prog 1)
    Progression/             # LevelUpSystem, UpgradeSystem, UpgradeOption     (Prog 2)
    Economy/                 # GoldManager                                      (Prog 2)
    UI/                      # HUD, LevelUpPopup, GameOverScreen, VirtualJoystick (Prog 3)
    API_CONTRACTS.md         # KONTRAK API — BACA DULU SEBELUM NGODING
  Editor/                    # WPG_3.Editor.asmdef + SceneBootstrap.cs
  Data/UpgradeOptions/       # ScriptableObject upgrade samples
  Prefabs/                   # Prefab player, enemy, projectile, UI
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
4. Buka scene `Assets/Scenes/Main.unity`.
5. Klik **Play** untuk tes.

## Catatan Penting

- Scene sudah auto-bootstrap `GameManager`, `EventSystem`, dan `Main Camera` lewat `SceneSetup.cs` (runtime). Tidak perlu drag prefab manual.
- Untuk setup tambahan (mis. menambah `GameManager` component di scene), bisa pakai menu **Tools > WPG_3 > Bootstrap Main Scene**.
- `InputSystem_Actions.inputactions` sudah ter-setup dengan action `Player/Move` (Vector2) dan `Player/Attack` (Button). Pakai ini — jangan buat asset baru.

## Lisensi

MIT — lihat `LICENSE`.
