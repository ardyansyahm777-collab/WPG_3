---
name: wpg3-progression-systems
description: Pola Progression & Economy WPG_3 (EXP, level-up, upgrade roll/choose, gold). Use when bekerja di Progression/, Economy/, atau soal EXP, UpgradeOption ScriptableObject, dan gold.
---

# WPG_3 Progression & Systems

Folder Prog 2: `Assets/Scripts/Progression/`, `Economy/`, `Core/`
(hanya bagian yang belum ditulis lead).
Baca `API_CONTRACTS.md` bagian 7–11 sebelum mengubah signature publik.

## Alur level-up

`GameEvents.RaiseExperienceGained(n)` → `LevelUpSystem.AddExperience`
(kurva `ExpToNextLevel = 50 * level`; satu add bisa multi-level) → tiap
level: `OnLevelChanged` + `GameEvents.RaiseLevelUp` →
`UpgradeSystem`: `RollChoices()` (acak tanpa duplikat,
`ChoicesPerLevelUp = 3`) → event `OnLevelUp` (UI baca `LastRoll` dan
tampilkan popup) → UI panggil `ChooseUpgrade(opt)` →
`PlayerStats.ApplyUpgrade(Type, Value)` → `OnUpgradeApplied`.

## UpgradeOption (ScriptableObject)

`Assets/Data/UpgradeOptions/`: field `Id/DisplayName/Description/Icon/Type`
+ `Value` (additive). 5 asset bawaan: Damage, MoveSpeed, FireRate, MaxHealth,
MultiShot. Asset baru via `CreateAssetMenu WPG_3/Upgrade Option` atau tool
`Tools > WPG_3`. `Value` MultiShot dibulatkan ke int.

## Gold

`GoldManager` singleton: `Add`/`TrySpend`, broadcast lokal `OnGoldChanged` +
global `RaiseGoldChanged`. Jangan referensi `GoldManager` langsung dari
`Enemy/` — distribusi reward lewat event bus.

## Checklist

- [ ] `ApplyUpgrade` aman: FireRate ≥ 0.1, MultiShot ≥ 1; MaxHealth tambah
  `MaxHP` + `Heal` senilai kenaikan
- [ ] `ExpToNextLevel` tidak pernah 0 (hindari infinite level-up loop)
- [ ] UI hanya baca `LastRoll`/event, tidak panggil internal sistem lain
- [ ] Commit `[progression] <aksi> <target>`
