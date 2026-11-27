---
layer: bible
status: living
---
# Unity.Shared — Project Bible

> Single source of truth for what Unity.Shared IS, is NOT, and the rules that keep it coherent.
> README says how to build and run it; this says how to think about it. The finish line lives in
> `docs/RELEASE.md`.

## 1. The one sentence {#MAU-§1}
Unity.Shared (`com.mindattic.unity.shared`) is the Unity package every MindAttic game references for the player's
profile, its one settings.json, the session mute and the shared `--headless` mode.

## 2. The product promise {#MAU-§2}
- A player's name, colour, volumes and language look the same from game to game.
- One line gets the player's identity: `Profiles.Current.Settings.Common.DisplayName`.
- A damaged or hand-edited settings file never crashes a game.
- Fields a newer game version wrote survive an older one loading and saving the file.
- Game logic that reads settings is testable in plain `dotnet test`, without Unity.

## 3. What it is NOT {#MAU-§3}
- **NOT a save system.** `Profiles/<Name>/Saves/` belongs to the game; its format is the game's business.
- **NOT a networking or UI package.** Those are Unity.Multiplayer and MindAttic.Components.
- **NOT tied to Newtonsoft or System.Text.Json.** It carries its own small JSON codec (§4).
- **NOT a reverse-engineered port.** It is MindAttic's own package; no `Unity.Rea.*` name.

## 4. Architecture canon {#MAU-§4}
```
unity/com.mindattic.unity.shared/
  Runtime/Core/   Unity.Shared.Core  (noEngineReferences, netstandard2.1 / C# 9)
                  Profiles/  Profiles, ProfileStore, Profile, ProfileSettings, CommonSettings, RgbColor, AtomicFile
                  Json/      MiniJson, JsonObject, IJsonCodec
                  Headless.cs
        ▲
  Runtime/        Unity.Shared       ProfilesBootstrap (roots the store at persistentDataPath),
                                     HeadlessBootstrap, Unity colour helpers
tests/Core.Build  compiles Runtime/Core as netstandard2.1, warnings as errors
tests/Core.Tests  NUnit: the format, the profile store, the headless switch
tools/Start-Headless.ps1  launches a built player headless with no window
```
- **Nouns:** a `ProfileStore` (a root folder) holds `Profile`s; each has one `ProfileSettings` = a typed
  `CommonSettings` block plus a free-form `game` `JsonObject`.
- **Verbs:** list / select / create / rename / delete-to-trash profiles; load and atomically save settings;
  resolve the root override, session mute and headless switch once, before the first scene loads.
- **Consumers** reference it by `file:../../Unity.Shared/unity/com.mindattic.unity.shared`: GridGame.Unity,
  TownWalker, Unity.Cursory, Unity.Eve, Unity.SpaceGod, UnityRonin and the Unity.Rea.* games.

## 5. The Laws {#MAU-§5}
Unity.Shared inherits the org-wide house rules in `../../MindAttic.HouseRules.md` (notably HOUSE-LAW-1 whole-number
versions, HOUSE-LAW-2 nothing is erased, HOUSE-LAW-13 LF only).

### MAU-LAW-1 — One settings format {#MAU-LAW-1}
Every game reads and writes `mindattic.settings` v1 as the README specifies: unknown fields at every level are kept
and written back; missing or wrong-typed fields read as defaults; a newer `version` is still read and kept.

### MAU-LAW-2 — Loading never throws {#MAU-LAW-2}
A file that is not JSON, not an object, or has another `format` is renamed `settings.json.bad-<UTC>` and defaults
are used.

### MAU-LAW-3 — Writes are atomic {#MAU-LAW-3}
`settings.json.tmp` is written and flushed, then replaces `settings.json` in one step; UTF-8 without BOM,
two-space indent, LF.

### MAU-LAW-4 — Deleted profiles go to the trash {#MAU-LAW-4}
A delete moves the folder whole to `Profiles/.trash/<Name>~<UTC>`; nothing is erased (HOUSE-LAW-2).

### MAU-LAW-5 — Core is engine-free {#MAU-LAW-5}
`Unity.Shared.Core` has no engine references and builds as netstandard2.1 / C# 9 with warnings as errors; anything
decidable without Unity lives there and is covered by `tests/Core.Tests`.

### MAU-LAW-6 — Session switches never touch the saved file {#MAU-LAW-6}
`-muted` / `MINDATTIC_CLI_MUTED=1` and `--headless` / `MINDATTIC_HEADLESS=1` change only this run; settings.json
is never written because of them. With neither set, nothing changes.

## 6. Verified state {#MAU-§6}
- ✅ Profiles, settings v1, MiniJson, atomic files (c8b90c0, MAU-T2): Core.Tests passing and a Unity 6000.4.3f1
  batchmode compile and smoke run.
- ✅ Session mute (d2f188e, MAU-T11).
- ✅ Standard app icon asset (f4b1c57, MAU-T14).
- ✅ Bughunt fixes: JSON overflow, surrogate-safe truncation, case-only rename rollback (3ce3b8a, MAU-T17).
- ✅ Headless mode and `tools/Start-Headless.ps1` (f51d7c5, MAU-T18).
- ✅ DocFX docs on GitHub Pages (b6353d0, MAU-T4).

## 7. Active frontier {#MAU-§7}
`docs/RELEASE.md`: MS-40, "Unity.Shared 2" — bughunt, a consumer compile pass, then release 2 with a changelog.

## 8. Quality bar {#MAU-§8}
A change is done when `tests/Core.Build` builds clean, `tests/Core.Tests` covers the new rule, the README's format
or API tables match the code, and no consuming game's manifest needs to change.

## 9. Glossary {#MAU-§9}
- **Profile** — one player's folder under `<persistentDataPath>/Profiles/`.
- **Common block** — the typed settings every game shares (volumes, mutes, reduce motion, language, name, colour).
- **Game block** — the free-form `game` object a game owns.
- **Headless** — a real game run muted, in the background, logging `MINDATTIC_HEADLESS_READY <productName>`.
