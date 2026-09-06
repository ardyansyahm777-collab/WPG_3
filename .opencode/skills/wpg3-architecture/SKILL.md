---
name: wpg3-architecture
description: Arsitektur dan konvensi proyek WPG_3 (Unity 2D top-down roguelike). Use when butuh peta folder, event bus GameEvents, state GameManager, alur runtime, atau aturan git sebelum ngoding.
---

# WPG_3 Architecture

## Ringkasan

Game 2D top-down shooter survival roguelike (Archero-like) untuk PC
(keyboard + mouse) dan mobile (touch). Unity 6000.6.x, URP 2D, Input System,
TextMeshPro. Build: Android + Windows Standalone, landscape 16:9.

## Peta folder (`Assets/Scripts/`, namespace `WpgGame.*`)

- `Core/`: `GameManager` (singleton; state `Playing/Paused/LevelUp/GameOver`;
  `SetState`, `Restart`, event `OnStateChanged`), `GameEvents` (static event
  bus + helper `Raise*` + `ClearAll` saat reload), `SceneSetup` (runtime
  bootstrap: GameManager, EventSystem + `InputSystemUIInputModule`,
  Main Camera orthographic).
- `Player/`: `PlayerController` (gerak via `Player/Move` →
  `Rigidbody2D.linearVelocity`), `PlayerStats`
  (`MoveSpeed/FireRate/Damage/MultiShot`, `ApplyUpgrade`, `OnStatsChanged`),
  `AutoAimShooter` (tembak musuh terdekat), `GameplaySetup` (runtime bootstrap
  Player + template musuh/proyektil + spawner; menu
  `Tools/WPG_3/Bootstrap Gameplay Objects`), `GameplayEditorSetup` (pastikan
  layer `PG_Player/PG_Enemy/PG_Projectile` + tag `Player`).
- `Combat/`: `HealthSystem` (`MaxHP/CurrentHP/IsDead`, `OnHealthChanged/OnDeath`,
  `TakeDamage/Heal`), `Projectile` (`Damage/Lifetime/Speed/HitMask/MaxRange`,
  `Launch`).
- `Enemy/`: `EnemyAI` (kejar player, `ContactDamage`, `OnEnemyDefeated`,
  broadcast `RaiseEnemyKilled` lalu `Destroy`), `EnemySpawner` (wave periodik
  di sekitar player, hanya spawn saat state `Playing`).
- `Progression/`: `LevelUpSystem` (EXP, kurva `ExpToNextLevel = 50 * level`,
  `AddExperience`), `UpgradeSystem` (`Pool`, `ChoicesPerLevelUp = 3`,
  `RollChoices/ChooseUpgrade/LastRoll`), `UpgradeOption` (ScriptableObject:
  `Id/DisplayName/Description/Icon/Type/Value`, additive), `UpgradeType`.
- `Economy/`: `GoldManager` (singleton, `Add/TrySpend`, `OnGoldChanged` +
  broadcast global).
- `UI/`: kontrak `HUDController.Bind(playerHealth, levelSystem, gold)`;
  HUD, popup level-up, dan game-over mengikuti kontrak API.
- `Editor/`: `SceneBootstrap`, `UpgradeOptionCreator`, `Builder`.
- Aset: 5 `UpgradeOption` di `Assets/Data/UpgradeOptions/`; prefab Player,
  Enemy, Projectile, GameManager, EventSystem di `Assets/Prefabs/`.

Dokumen penentu: `Assets/Scripts/API_CONTRACTS.md` — baca sebelum mengubah
API publik apa pun.

## Alur runtime

Boot (`SceneSetup` + `GameplaySetup`) → `Playing`: spawner spawn musuh,
shooter auto-aim → musuh mati → `OnEnemyKilled` → reward/EXP →
`LevelUpSystem` → `RaiseLevelUp` → `UpgradeSystem` roll → popup
(state `LevelUp`, gameplay freeze) → `ChooseUpgrade` → `ApplyUpgrade` →
kembali `Playing`; player mati → `GameOver` → restart bersih.

## Aturan yang selalu berlaku

- Komunikasi antar-sistem lewat `GameEvents`, bukan referensi langsung.
- Subscribe di `OnEnable`, unsubscribe di `OnDisable`.
- `linearVelocity`, TextMeshPro, `EventSystem`, Input Actions bawaan.
- Commit kecil format `[<role>] <aksi> <target>`; `git pull` dulu, push sesudah.
