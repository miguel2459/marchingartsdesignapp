# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

**MADA** (Marching Arts Design App) is a 3D marching drill design tool built in Unity 6, targeting WebGL. It lets users create, edit, and play back marching show formations. The site (`mada.mprstudios.com`) is served from the `docs/` folder of the `Unity6_Dev` branch via GitHub Pages.

Company: MPR Studios. Backend: Google Cloud Functions + Google Sheets + Google Drive for show data storage.

## Build & Development

This is a Unity project — there is no CLI build command. All building is done through the Unity Editor (File → Build Settings → WebGL).

### Build pipeline

1. **Unity build output path must be set to `WebBuilds/`** (File → Build Settings → Output Path). `WebBuilds/` is gitignored — it is the raw staging area, not the deployed folder.
2. After a successful build, `Assets/Editor/PostBuildWebGLProcessor.cs` (a `[PostProcessBuild]` hook) runs automatically and:
   - Verifies `WebBuilds/Build/` contains a `.loader.js` before touching anything
   - Deletes only `docs/app/` and copies `WebBuilds/` → `docs/app/`
   - Copies `WebBuilds/favicon/` → `docs/favicon/` (replaces only that folder)
   - Writes `docs/CNAME` (`mada.mprstudios.com`)
   - Runs `BuildTools/injectBuildMetadata.ps1 -BuildDir <abs-path-to-docs/app>` to stamp version strings into `docs/app/index.html`
3. The hub page (`docs/index.html`), lessons (`docs/lessons/`), and `docs/404.html` live outside `docs/app/` and are **not touched by the build pipeline**.

### Deployed site layout

```
docs/                          ← GitHub Pages root (Unity6_Dev branch, /docs folder)
├── CNAME                      mada.mprstudios.com
├── index.html                 Hub page (links to /app/ and /lessons/)
├── 404.html
├── favicon/                   Icon assets (updated on each build)
├── app/                       MADA WebGL build (overwritten on each build)
│   ├── index.html
│   ├── Build/
│   ├── StreamingAssets/       config.json is gitignored here
│   ├── logo.png
│   └── style.css
└── lessons/
    ├── index.html
    └── charlie-brown/
        └── index.html

```

**Config file** (`Assets/StreamingAssets/config.json`) contains `googleApiKey` and `backendURL`. All copies under `**/StreamingAssets/config.json` are gitignored. In WebGL builds, config is fetched from a Cloud Function endpoint and stored in `PlayerPrefs` — the file is not read at runtime.

## Scene Architecture

Scenes are loaded additively via `SceneController` (a persistent singleton). Build indices are fixed:

| Index | Scene | Purpose |
|-------|-------|---------|
| 1 | Main | Persistent bootstrap (never unloaded) |
| 2 | StartUpScene | Welcome/splash |
| 3 | LoginScene | Auth flow |
| 4 | ShowSelectionScene | Show library |
| 5 | ShowManagerScene | Field editor + playback |

Scene switching always unloads all scenes except Main, then loads the target additively.

## Startup Flow

`AppBootstrapper` (on Main scene) orchestrates startup:
1. `ConfigLoader` fetches API key + backend URL from Google Cloud → stores in `PlayerPrefs`
2. `SessionStateValidator` checks for a valid cached session (AccountSheetID + FolderID in PlayerPrefs) and compares build version to invalidate stale caches
3. If session valid → `SessionManager.AutoLogin()` → load ShowSelectionScene (4)
4. If no session → load StartUpScene (2)

## Core Singletons (DontDestroyOnLoad)

- **`SessionManager`** — Central coordinator. Holds `UserStateSO`, `ShowStateSO`, `RuntimeCacheSO` (ScriptableObjects). Manages login/logout, show list fetching from Google Sheets, and delegates save/exit to `ShowDataManager`. Instantiates `JsonCoordinatorService` and `GoogleSheetsService` after auth.
- **`SceneController`** — Handles all scene loading/unloading.
- **`InputBridge`** — WebGL-only keyboard bridge (see below).
- **`EnsembleDirector2`** — Has a static instance; central hub for the ShowManager scene.

## ScriptableObjects as Runtime State

Three SO assets act as the shared runtime state bus:
- **`UserStateSO`** — userId, userName, folderId, accountSheetID
- **`ShowStateSO`** — current show metadata (title, field type, marcher count, set count, JSON URLs)
- **`RuntimeCacheSO`** — in-memory position cache and `SetTimingData` dictionary used for playback interpolation

## ShowManager Scene Init Sequence

`ShowSceneInitializer` orchestrates initialization as a coroutine:
1. Init `SessionBootstrapper` and `EnsembleSessionLoader`
2. Init `FieldGridManager` and `MarcherManager` (subscribe to `OnMarchersReady` before starting bootstrapper)
3. Run `SessionBootstrapper.BootStrapperStart()` coroutine (loads JSON, populates RuntimeCacheSO)
4. Wait for `OnMarchersReady` (marchers built and positioned)
5. Init `SelectedMarchers`, `SetProgressBar`, `EnsembleUIController`, `UserAccountManager`, `CameraModeManager`

## Marcher System (CoreControl)

- **`MarcherFactory`** — Spawns marcher prefabs, assigns identities
- **`MarcherManager`** — On `EnsembleSessionLoader.OnReady`, calls `MarcherFactory.Build()` then restores positions from JSON or applies fallback shape. Fires `OnMarchersReady` when done.
- **`MarcherController`** — Animates marchers during playback using tempo-driven interpolation
- **`MarcherLifecycleService`** — Confirms/deletes dots (positions), syncs UI and visuals

Position data structure: `countPositions[set][count] = { pos, type }` where type is `"march"`, `"hold"`, or `"inferred"`. Set 0, Count 0 is the mandatory start position.

## JSON / Persistence

`JsonCoordinatorService` wraps four services:
- **`JsonGenerationService`** — Serializes marcher/timing state to JSON strings
- **`JsonParserService`** — Deserializes JSON back to runtime data (async coroutines)
- **`JsonCacheService`** — Local disk cache (Application.persistentDataPath)
- **`JsonBackendService`** — Upload/download via backend REST endpoint

Cache-vs-cloud sync: checks timestamp; uses cloud version if newer, local if up-to-date.

Show data (show list, metadata) comes from **Google Sheets** via `GoogleSheetsService`. The user's root sheet (AccountSheetID) has a "Shows" tab listing all shows.

## Auth System

`BackendAuthService` implements `IAuthService` and handles Login, SignUp, GuestLogin, and password reset via POST to `backendURL`. All operations use `UnityWebRequest` coroutines with:
- Retry (up to 3×) only on `ConnectionError` / `DataProcessingError`
- No retry on `ProtocolError` (HTTP 4xx/5xx)
- `LoginSystem` namespace

`MockAuthService` exists for editor testing without a live backend.

Session persistence: successful login stores `IsLoggedIn`, `AccountSheetID`, `FolderID` in PlayerPrefs.

## WebGL Input Bridge

Text input in WebGL doesn't work natively with TMP InputFields. The solution:
- `InputBridge` (singleton) registers `HtmlInputFieldHandler` components by string ID
- When a field is focused, `InputBridge.ShowInputForField()` calls the JS function `ShowHtmlInputWithPlaceholder()` which shows an `<input>` overlay in `index.html`
- On submit/blur, JS calls `unityInstance.SendMessage("InputBridge", "ReturnFromHtmlInput", "id|value")`
- `InputBridge.ReturnFromHtmlInput()` routes the value back to the correct `HtmlInputFieldHandler`

## Key Patterns

- **Namespace**: Only `AuthSystem` uses a namespace (`LoginSystem`). Other systems use global namespace.
- **Initialization**: Most MonoBehaviours expose an explicit `XxxInit()` method called in sequence by an orchestrator rather than relying on `Start()`/`Awake()` order.
- **Coroutines + async/await mix**: Most Unity IO uses coroutines; `JsonCoordinatorService.GetJsonAsync()` uses `async Task`.
- **Debug logging**: Emoji prefixes are used consistently (`✅`, `❌`, `⚠️`, `📡`, `💾`, etc.) — maintain this convention in new code.
- **`#if UNITY_EDITOR || DEVELOPMENT_BUILD`** guards around verbose debug logs and sensitive payload logging.
- **`#if UNITY_WEBGL && !UNITY_EDITOR`** guards around all WebGL-specific code (InputBridge, config loading, WebGLInput).
