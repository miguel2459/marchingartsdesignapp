# Changelog — Marcher System / CoreControl

All notable changes to the CoreControl module of the Marcher System will be documented in this file.

---

## [v2.0.0] — CoreControl AAA Refactor
### Released: 2025-05-14

### ✨ Added
- Full responsibility segmentation across:
  - `MarcherFactory.cs` (spawning)
  - `MarcherManager.cs` (loading/bootstrapping)
  - `MarcherController.cs` (animation)
  - `MarcherLifecycleService.cs` (confirmation, deletion, spawning)
- Commented debug logging for marcher creation, animation, and lifecycle flow
- Centralized UI and visual refresh logic in `MarcherLifecycleService`
- Configurable fallback shape setting for default formations (`ShapeType.Box`)

### 🛠️ Changed
- `MarcherManager` rebuild flow made rerunnable (`EditorRebuild()` via context menu)
- Position restoration logic uses latest confirmed dot across sets
- `MarcherController` restart logic to support mid-animation overrides
- Count interpolation driven by tempo curve per set

### 🧼 Cleaned
- Removed direct access to `countPositions` in favor of `LoadPositions()`
- Added `IReadOnlyList` wrappers for safe public access to marcher lists
- Guarded against null prefab components in `MarcherFactory`

---

## [Unreleased]
- Hold position (H) fallback logic
- Marcher-to-marcher spacing analysis and diagnostics
- Curved path animation layer
