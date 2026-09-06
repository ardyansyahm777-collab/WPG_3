---
name: wpg3-release
description: Integrasi, validasi, dan rilis WPG_3 (bootstrap, wiring scene, build). Use when bootstrap scene, wiring sistem, validasi run penuh, build Windows/Android, atau versioning.
---

# WPG_3 Release & Integration

Wewenang lead. Scene `Assets/Scenes/Main.unity` sengaja minimal (Camera,
Global Light 2D, `[EnemySpawner]`); sisanya runtime bootstrap — jangan taruh
manual object yang sudah di-bootstrap.

## Bootstrap & wiring

- `SceneSetup`: GameManager, EventSystem, Main Camera orthographic.
- `GameplaySetup`: Player, template musuh/proyektil, spawner
  (atau via menu `Tools/WPG_3/Bootstrap Gameplay Objects`).
- Sistem (LevelUp, Upgrade + isi `Pool` dari `Assets/Data/UpgradeOptions/`,
  Gold) dan UI (HUD, popup, game-over) dipastikan ada dan ter-wire oleh lead.

## Validasi run penuh (wajib sebelum versi)

Play di Editor → musuh spawn → bunuh → EXP/gold masuk → popup level-up
(gameplay freeze) → pilih upgrade → stat berubah → HP 0 → game over →
restart bersih: tanpa singleton ganda dan tanpa subscriber basi
(`GameEvents.ClearAll` saat reload).

## Build & versi

- Target: Windows Standalone + Android; cek Input Actions, layer
  (`PG_*`)/tag (`Player`), dan orientasi landscape 16:9.
- Commit kecil `[<role>] <aksi> <target>`; `git pull` dulu, push sesudah.
- Tag versi (`versi 0.x`) hanya setelah run penuh lolos.
