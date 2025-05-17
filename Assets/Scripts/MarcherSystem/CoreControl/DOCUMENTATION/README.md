# Marcher System → CoreControl

This module manages the creation, control, animation, and lifecycle of individual marchers. It is the foundational logic layer for interacting with marchers in the field editor and performance playback environments.

---

## 🎯 Purpose

- Spawns and initializes marcher GameObjects
- Confirms, deletes, and tracks marcher positions per set/count
- Handles performance-time interpolation and animation
- Communicates with UI, metronome, and selection systems

---

## 🧩 Key Scripts & Responsibilities

| Script | Responsibility |
|--------|----------------|
| **MarcherFactory.cs** | Instantiates marcher prefabs, assigns names and identities, and stores references |
| **MarcherManager.cs** | Rebuilds all marcher data at load, restores from JSON, or applies fallback formation |
| **MarcherController.cs** | Animates marcher motion across counts using tempo-driven interpolation |
| **MarcherLifecycleService.cs** | Central service to confirm, delete, and spawn marchers with full data/UI sync |

---

## 🔄 Lifecycle Flow

1. **Scene Load** triggers `EnsembleSessionLoader.OnReady`
2. `MarcherManager` rebuilds all marchers via `MarcherFactory.Build()`
3. Marcher positions are restored from JSON or arranged using fallback logic
4. On interaction (`Spacebar`, `H`, etc.):
   - `MarcherLifecycleService.ConfirmDot()` stores data and refreshes visuals
5. `MarcherController` animates each marcher during performance playback using timing map and interpolation

---

## 📶 Event & Data Flow

- `OnMarchersReady`: triggered after marchers are rebuilt
- `MarcherLifecycleService` dispatches:
  - `.ConfirmDot()` → saves dot → updates paths, UI, count bars
  - `.DeleteDot()` → removes dot → triggers visual reset
  - `.SpawnNewMarchers()` → creates new marcher and injects fallback positions
- `MarcherController` reads from `RuntimeCacheSO.SetTimingMap` and `MarcherPositionsManager.countPositions`

---

## 🧪 JSON/Positioning Notes

- Set 0, Count 0 is treated as the default "start" position
- All marchers must have at least one confirmed position (`march` or `hold`) at Set 0, Count 0
- Positions are stored as:
  ```json
  countPositions[set][count] = {
    "pos": [x, y, z],
    "type": "march" | "hold" | "inferred"
  }
