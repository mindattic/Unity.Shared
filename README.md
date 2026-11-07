# Unity.Shared
Shared Unity package for every MindAttic game: player profiles with one versioned settings.json each, a shared `--headless` mode for automated testing, and the common helpers the games would otherwise copy.

![C#](https://img.shields.io/badge/C%23-9.0-239120) ![Unity](https://img.shields.io/badge/Unity-6000.0%2B-black) ![.NET Standard](https://img.shields.io/badge/.NET%20Standard-2.1-512BD4) ![License](https://img.shields.io/badge/license-MIT-blue) ![Status](https://img.shields.io/badge/status-1.1.0-green)

```text
<persistentDataPath>/Profiles/
├── current.txt            "Ryan"  (the profile in use)
├── Ryan/
│   ├── settings.json      format mindattic.settings v1: common + game blocks
│   └── Saves/             the game's own save files
├── Default/ ...
└── .trash/Old~20261021T101500Z/   deleted profiles, moved aside, never erased
```

## Why

- Every MindAttic game keeps players, settings and saves the same way, so a player's name, colour, volumes and language look the same from game to game.
- One line gets the player's identity: `Profiles.Current.Settings.Common.DisplayName`.
- A damaged or hand-edited settings file never crashes a game: bad files are moved aside and defaults used.
- Fields a newer game version wrote survive an older one loading and saving the file.
- The engine-free core runs in plain `dotnet test`, so game logic that reads settings can be tested without Unity.

## Features

- **Profiles**: list, current, select, create, rename (case-only too), delete to `.trash`; one `Profile` object per profile so settings changes are seen everywhere.
- **Settings**: a typed common block (music and SFX volume, mutes, reduce motion, language, display name, colour) and a free-form game block, both keeping unknown fields.
- **Safe files**: atomic writes (temp file, then replace), UTF-8 without BOM, LF, damaged files renamed `settings.json.bad-<timestamp>`.
- **JSON without a dependency**: a small strict JSON reader/writer that keeps key order and exact number text, behind an injectable `IJsonCodec`.
- **Root override** for tests and automation: `Profiles.UseRoot(path)`, the `MINDATTIC_PROFILES_ROOT` environment variable or a `-profiles-root <path>` command-line switch.
- **Session mute** for tests and automation: the `MINDATTIC_CLI_MUTED=1` environment variable or a `-muted` command-line switch (read before the first scene loads, like the root override) mutes all audio for that run only. `CommonSettings` then reports music and SFX muted at volume 0, but settings.json is never written: the player's saved volumes and mutes are untouched. `Profiles.SessionMuted` turns it on or off from code.
- **Headless mode** for scripted and automated testing, off by default: `--headless` (or `-headless`) or `MINDATTIC_HEADLESS=1` runs the real game muted, in the background, vSync off, and logs one `MINDATTIC_HEADLESS_READY <productName>` line; `Headless.IsActive` / `Headless.WhenActive(...)` let a game add its own opt-ins. `tools/Start-Headless.ps1` launches a built player that way with no window. See [Headless mode](#headless-mode).

## Quick start

Add the package to the game's `Packages/manifest.json` (local package; this repo cloned next to the game's repo):

```json
"com.mindattic.unity.shared": "file:../../Unity.Shared/unity/com.mindattic.unity.shared"
```

Then, anywhere after the game starts (the package roots itself at `Application.persistentDataPath` before the first scene loads):

```csharp
using Unity.Shared;        // Unity colour helpers
using Unity.Shared.Core;   // Profiles, settings

var common = Profiles.Current.Settings.Common;
string name = common.DisplayName;          // the profile name until the player sets one
Color tint = common.GetUnityColor();       // a palette colour picked from the name until the player picks one

common.DisplayName = "Ryan";
common.SetUnityColor(Color.cyan);          // or common.Color = RgbColor.Parse("#00FFFF")
common.MusicVolume = 0.4f;
Profiles.Current.SaveSettings();           // atomic write of settings.json
```

## The standard: mindattic.settings v1

Every MindAttic Unity game follows this layout and format.

### Folders

| Path | What it is |
|---|---|
| `<persistentDataPath>/Profiles/current.txt` | The name of the profile in use (one line). |
| `Profiles/<Name>/settings.json` | That profile's one settings file. |
| `Profiles/<Name>/Saves/` | The game's own save files; their format is the game's business. |
| `Profiles/.trash/<Name>~<UTC yyyyMMddTHHmmssZ>` | A deleted profile, moved here whole (HOUSE-LAW-2). |

If `current.txt` is missing or names a profile that is not there, the first profile by name is used; if there are none, a `Default` profile is made. Either way it is written to `current.txt`.

### Profile names

A name must make one folder on every platform: not blank, at most 64 characters, no leading or trailing spaces, not starting or ending with `.`, none of `/ \ : * ? " < > |` or control characters, and not a Windows device name (`CON`, `PRN`, `AUX`, `NUL`, `COM1`-`COM9`, `LPT1`-`LPT9`, with or without an extension). Names are unique ignoring case. These are Unity.SpaceGod's (formerly UnitySpace) `ProfileStore.IsValidName` rules plus the dot, device-name and control-character rules.

### settings.json

```json
{
  "format": "mindattic.settings",
  "version": 1,
  "common": {
    "musicVolume": 0.6,
    "sfxVolume": 0.85,
    "muteMusic": false,
    "muteSfx": false,
    "reduceMotion": false,
    "language": "en",
    "displayName": "Ryan",
    "color": "#3498DB"
  },
  "game": { "anything": "the game owns" }
}
```

| Common field | Type | Default | Notes |
|---|---|---|---|
| `musicVolume` | number 0-1 | 0.6 | Clamped to 0-1. |
| `sfxVolume` | number 0-1 | 0.85 | Clamped to 0-1. |
| `muteMusic` | bool | false | |
| `muteSfx` | bool | false | |
| `reduceMotion` | bool | false | Less screen shake and motion. |
| `language` | string | `"en"` | IETF tag. |
| `displayName` | string, optional | the profile name | The name other players see. Trimmed, at most 32 characters. Absent means "use the profile name", which follows a rename. |
| `color` | `"#RRGGBB"`, optional | a palette colour from the profile name | The player's colour. Absent means one of 8 palette colours chosen from the name (FNV-1a), stable across runs. |

Rules a reader and writer must keep:

1. Unknown fields at the top level, in `common` and in `game` are kept and written back unchanged.
2. A missing file, missing field or wrong-typed field reads as its default.
3. A file that is not JSON, not an object, or has a `format` other than `mindattic.settings` is renamed `settings.json.bad-<UTC yyyyMMddTHHmmssfffZ>` and defaults are used. Loading never throws.
4. Writes are atomic: `settings.json.tmp` is written and flushed, then replaces `settings.json` in one step.
5. Files are UTF-8 without BOM, two-space indented, LF line endings.
6. A file with a newer `version` is still read, and keeps its version number when saved.

## API

All in `Unity.Shared.Core` (engine-free) unless noted.

| Call | What it does |
|---|---|
| `Profiles.Current` | The profile in use. |
| `Profiles.Current.Settings.Common.DisplayName` / `.Color` | The player's identity, read or set. |
| `Profiles.Current.SaveSettings()` / `TrySaveSettings()` | Write settings.json (throws / returns false on a disk error). |
| `Profiles.Current.Settings.Game` | The game's `JsonObject`: `GetFloat("k", def)`, `Set("k", v)`. |
| `Profiles.Store.List()`, `.Select(n)`, `.Create(n)`, `.Rename(a, b)`, `.Delete(n)`, `.Exists(n)`, `.Get(n)` | Profile management. |
| `ProfileStore.IsValidName(n)` | The name rules above. |
| `Profiles.UseRoot(path)` / `UseDataPath(path)` / `Use(store)` / `Reset()` | Point the facade somewhere (tests, tools). |
| `new ProfileStore(root, codec)` | A store without the static facade. |
| `common.GetUnityColor()` / `SetUnityColor(c)`, `rgb.ToUnityColor()` | Unity colour conversions (`Unity.Shared`). |
| `Headless.IsActive` (get/set), `Headless.WhenActive(cb)`, `Headless.Activated`, `Headless.Reset()` | Headless mode (see [Headless mode](#headless-mode)). |
| `Headless.Requested(args, envValue)`, `Headless.ReadyLine(name)` | The switch/variable parsing and the ready line. |
| `HeadlessBootstrap.Apply()` | The package's headless effects (`Unity.Shared`); called for you. |

## Headless mode

One optional launch mode, the same in every MindAttic Unity and Unreal game (Unreal.Shared keeps the same contract): run the real game with no window, drive it from a script, query it. With neither the switch nor the variable set, nothing at all changes.

**Turning it on.** The command-line switch `--headless` (also `-headless`, any case) or the environment variable `MINDATTIC_HEADLESS=1` (`true`/`yes`/`on` also work). It is resolved once, before the first scene loads.

**What the package does when it is on** (`HeadlessBootstrap`; nothing game-specific):

- turns on the session mute (`Profiles.SessionMuted`; settings.json is never written, as with `-muted`);
- sets `Application.runInBackground = true` and `QualitySettings.vSyncCount = 0`;
- logs exactly one line, `MINDATTIC_HEADLESS_READY <Application.productName>`, without a stack trace, for scripts to wait on.

**No window is the launcher's job.** Unity picks its window and renderer before any script runs, so the player also needs Unity's own `-batchmode -nographics`. `tools/Start-Headless.ps1` passes them:

```powershell
# from a game's repo, with Unity.Shared cloned next to it
& ..\Unity.Shared\tools\Start-Headless.ps1 -Exe Build\Game.exe -Wait -TimeoutSeconds 120 -profiles-root D:\tmp\Profiles
```

It runs `Game.exe --headless -batchmode -nographics -logFile <path> [your args]`, prints the log path and PID, and returns `{ ProcessId, LogFile, ReadyLine }`. `-LogFile` chooses the log (default `%TEMP%\<exe>-headless-<UTC>.log`). `-Wait` waits for the ready line: exit 1 on timeout (`-StopOnTimeout` also stops the player), exit 2 if the player quits first. Every other argument is passed through to the player.

CI that launches the exe directly passes the same flags itself, then waits for a log line starting with `MINDATTIC_HEADLESS_READY`:

```text
Game.exe --headless -batchmode -nographics -logFile build/headless.log
```

**A game's own opt-ins** (skipping the main menu, scripted input) stay in the game, using `Unity.Shared.Core`:

```csharp
using Unity.Shared.Core;

Headless.WhenActive(() => skipMainMenu = true);   // runs now if headless, or once it is turned on later
if (Headless.IsActive) { /* ... */ }              // ask at any time
Headless.Activated += OnHeadless;                  // raw event: fires once when it turns on (usually before game code runs)
Headless.IsActive = true;                          // override from code (tests, tools); Headless.Reset() forgets it
```

## JSON: why the package carries its own

Games already ship their own Newtonsoft.Json (Unity.GridGame's GridGameGaiden player bundles `Newtonsoft.Json.dll`), so a second copy here would clash, and System.Text.Json is not part of Unity's .NET Standard 2.1 profile. Unity's `JsonUtility` drops unknown fields and needs the engine. So `Unity.Shared.Core` has `MiniJson`, about 300 lines of strict RFC 8259 reading and writing into an ordered `JsonObject` that keeps exact number text. It sits behind `IJsonCodec`: a game that wants Newtonsoft passes its own codec (`new ProfileStore(root, myCodec)` or `Profiles.UseDataPath(path, myCodec)`) that converts to and from `JsonObject`.

## Naming

The repo was MindAttic.Unity, then MindAttic.Unity.Shared, and is now Unity.Shared (MAU-T6). It follows Unity.Multiplayer's convention: package `com.mindattic.unity.shared` under `unity/`, engine-free assembly and namespace `Unity.Shared.Core` (`noEngineReferences`, builds as netstandard2.1 / C# 9), runtime assembly and namespace `Unity.Shared`. The ticket's older names (`com.mindattic.unity`, `MindAttic.Unity.Core`, `MindAttic.Unity.Runtime`) are retired.

## Project layout

```text
unity/com.mindattic.unity.shared/
  package.json
  Runtime/Unity.Shared.asmdef          ProfilesBootstrap (persistentDataPath), HeadlessBootstrap, Unity colour helpers
  Runtime/Core/Unity.Shared.Core.asmdef engine-free
  Runtime/Core/Headless.cs              the --headless / MINDATTIC_HEADLESS switch
  Runtime/Core/Json/                    MiniJson, JsonObject, IJsonCodec
  Runtime/Core/Profiles/                Profiles, ProfileStore, Profile, ProfileSettings, CommonSettings, RgbColor, AtomicFile
tests/Core.Build/                       compiles Runtime/Core as netstandard2.1, warnings as errors
tests/Core.Tests/                       NUnit tests of the format, the profile store and the headless switch
tools/Start-Headless.ps1                launches a built player headless, with no window
```

## Testing

```bash
dotnet test tests/Core.Tests
```

It builds `Runtime/Core` as Unity does (netstandard2.1, C# 9) first, then runs the NUnit suite: JSON round-trips, unknown-field preservation, defaults, damaged files, atomic writes, name rules, current/select/create/rename/delete-to-trash, the root overrides, and the headless switch parsing and override.

## Documentation

- [docs/USER_STORIES.md](docs/USER_STORIES.md): the ticket log.
- [AGENTS.md](AGENTS.md): agent instructions.
- [assets/README.md](assets/README.md): the standard Unity app icon every MindAttic port should use.

## License

MIT, see [LICENSE](LICENSE).

Part of [MindAttic](https://mindattic.com) — see more projects at [github.com/mindattic](https://github.com/mindattic). Sibling: [Unity.Multiplayer](https://github.com/mindattic/Unity.Multiplayer).
