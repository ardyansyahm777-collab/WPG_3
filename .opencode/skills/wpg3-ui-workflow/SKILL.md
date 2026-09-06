---
name: wpg3-ui-workflow
description: Pola UI WPG_3 (HUD, popup level-up, game over, input touch). Use when bekerja di UI/ atau soal HUD, popup upgrade, game over screen, dan virtual joystick.
---

# WPG_3 UI Workflow

Folder eksklusif Prog 3: `Assets/Scripts/UI/`.
Baca `API_CONTRACTS.md` bagian 12–13. Semua teks pakai TextMeshPro;
`EventSystem` (dengan `InputSystemUIInputModule`) sudah di-bootstrap.

## Pola

- **HUD** (`HUDController.Bind(playerHealth, levelSystem, gold)`):
  subscribe `OnHealthChanged` → HP bar; `OnExpChanged` → EXP bar +
  `CurrentLevel`; `OnGoldChanged` → teks gold. Unsubscribe di `OnDisable`.
  Dilarang panggil method internal sistem lain — hanya event.
- **Popup level-up**: subscribe `UpgradeSystem.OnLevelUp` → baca
  `UpgradeSystem.LastRoll` → tampilkan sebagai N tombol → klik:
  `ChooseUpgrade(opt)` + `GameManager.SetState(Playing)`. Saat popup
  terbuka state = `LevelUp` sehingga player/shooter/spawner freeze otomatis.
- **Game over**: subscribe `GameEvents.OnGameOver` → tampilkan layar +
  tombol restart → `GameManager.Restart()`.
- **Input**: pakai asset bawaan — `Player/Move` (Vector2,
  KeyboardMouse + Touch), `Player/Attack` (opsional, auto-shoot default),
  `UI/Submit`/`UI/Cancel` untuk navigasi popup. Jangan buat asset baru.
  Canvas landscape 16:9; virtual joystick (mobile) bind ke `Player/Move`.

## Checklist

- [ ] Semua teks TMP; navigasi popup bisa via Submit/Cancel
- [ ] Subscribe/unsubscribe event di `OnEnable`/`OnDisable`
- [ ] Popup kembalikan state ke `Playing`; game-over panggil `Restart()`
- [ ] Commit `[ui] <aksi> <target>`
