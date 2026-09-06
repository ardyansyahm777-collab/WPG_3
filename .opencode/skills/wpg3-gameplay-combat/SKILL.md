---
name: wpg3-gameplay-combat
description: Pola Gameplay & Combat WPG_3 (movement, auto-aim shooting, projectile, enemy AI, spawner). Use when bekerja di Player/, Combat/, Enemy/ atau soal gerak player, tembakan, damage, dan spawn musuh.
---

# WPG_3 Gameplay & Combat

Folder eksklusif Prog 1: `Assets/Scripts/Player/`, `Combat/`, `Enemy/`.
Baca `API_CONTRACTS.md` bagian 1–6 sebelum mengubah signature publik.

## Pola inti

- **Gerak** (`PlayerController`): baca `Player/Move` (Vector2) dari asset
  `InputSystem_Actions` → `Rigidbody2D.linearVelocity = input * MoveSpeed`.
  Clamp magnitude 1 (diagonal WASD) tapi pertahankan analog parsial joystick.
  Nol-kan velocity saat `GameManager.State != Playing`.
- **Stats** (`PlayerStats`): satu-satunya sumber kebenaran
  (`MoveSpeed/FireRate/Damage/MultiShot`). Semua perubahan lewat
  `ApplyUpgrade` + broadcast `OnStatsChanged`.
- **Tembak** (`AutoAimShooter`): cari `EnemyAI` terdekat yang hidup dalam
  `AimRadius` (abaikan di bawah `MinAimDistance`); interval `1 / FireRate`;
  sebar `MultiShot` proyektil via `SpreadRadians`.
- **Proyektil** (`Projectile`): `Rigidbody2D` kinematic + collider trigger.
  `Launch(dir)` set velocity; filter tabrakan via `HitMask` (layer
  `PG_Enemy`); abaikan player & proyektil lain; hancurkan diri saat kena
  musuh, `Lifetime` habis, atau lewat `MaxRange`.
- **Musuh** (`EnemyAI`): kejar `PlayerController` via `linearVelocity`;
  contact damage + `ContactInterval`; saat mati invoke `OnEnemyDefeated` →
  `GameEvents.RaiseEnemyKilled(gameObject)` → `Destroy`. Broadcast kill
  adalah sumber reward/EXP — jangan dihapus.
- **Spawn** (`EnemySpawner`): wave (`SpawnInterval`/`WaveSize`) di cincin
  `SpawnRadiusMin/Max` sekitar player; hanya spawn saat state `Playing`;
  `AutoStart` default true.

## Fisika & layer

Layer `PG_Player` / `PG_Enemy` / `PG_Projectile` (dijamin
`GameplayEditorSetup` + `GameplaySetup`); tag `Player`. Player dynamic +
freeze rotation; proyektil kinematic + trigger.

## Checklist

- [ ] Kontrak API 1–6 tidak berubah tanpa diskusi tim
- [ ] Subscribe/unsubscribe event di `OnEnable`/`OnDisable`
- [ ] Tidak ada pencarian object per-frame di kode baru
- [ ] Musuh mati selalu broadcast `OnEnemyKilled`
- [ ] Commit `[gameplay] <aksi> <target>`
