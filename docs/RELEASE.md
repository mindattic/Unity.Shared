---
layer: release
status: living
---
# Unity.Shared — Release charter

> The one finish line this repo is working toward. Every open ticket in this repo sits under one of the epics
> below, and every epic is part of the milestone. Work that does not fit is out of scope until this milestone is
> reached (owner, 2026-11-27, MAT-T319).

## 1. What this is
The shared Unity package every MindAttic game uses (`com.mindattic.unity.shared`): player profiles with one
versioned settings.json each, safe atomic files, a dependency-free JSON codec, a session mute and the shared
`--headless` mode.

## 2. The milestone: Unity.Shared 2 {#milestone}
- **Milestone:** MS-40, "Unity.Shared 2: the stable profile, settings and headless package every game uses"
- **Published means:** a local UPM package consumed by `file:` path from the sibling checkout, with docs on
  GitHub Pages. Consumers today: GridGame.Unity, TownWalker, Unity.Cursory, Unity.Eve, Unity.SpaceGod, UnityRonin
  and the 7 Unity.Rea.* games (Chain, Hare, Incognito, Overworld, ProjectVania, QuickDuel, Ricochet).
- **The natural stopping point:** the package already does what its games use (profiles, settings v1, mute,
  headless). A finished library is a documented whole-number version (HOUSE-LAW-1; today's `1.1.0` is not one)
  that every consumer compiles against, with a changelog.

## 3. Done when (acceptance checks)
1. build — `tests/Core.Build` (netstandard2.1, warnings as errors) builds clean on main.
2. exists — `package.json` is 2.0.0 and `CHANGELOG.md` exists.
3. signoff — the consumer compile pass is clean across the games.

## 4. Epics, in priority order
| Priority | Epic (ticket) | What it delivers | Features |
|---|---|---|---|
| 900 | MAU-T25 Stable package | Bug-hunted; every consumer compiles | MAU-T27 Bug hunt (990): MAU-T24 · MAU-T28 Consumers compile (980): MAU-T31 |
| 800 | MAU-T26 Release 2 | Release 2 with changelog and docs | MAU-T29 Changelog and version (890): MAU-T32 · MAU-T30 Docs (880): MAU-T33 |

Order: MAU-T32 (cut release 2) requires MAU-T24 and MAU-T31.

## 5. Out of scope (not before this milestone)
- Renaming to Unity.Rea.Shared (MAU-T15, MAU-T20–T22) — this is MindAttic's own package, not reverse-engineered,
  and 13 manifests reference its path.
- New shared systems no game has asked for (save formats, input, localisation).

## 6. Current verified state
- Profiles and settings v1, MiniJson, atomic files (c8b90c0, MAU-T2); session mute (d2f188e, MAU-T11); the
  standard app icon asset (f4b1c57, MAU-T14); bughunt fixes (3ce3b8a, MAU-T17); headless mode and
  `tools/Start-Headless.ps1` (f51d7c5, MAU-T18); DocFX site on Pages (b6353d0, MAU-T4).
- `tests/Core.Tests` (NUnit) covers the format, the profile store and the headless switch.

## 7. Decision record
- 2026-11-27 — charter drafted from README/code/tickets (CUR-T36); docs/BIBLE.md written. Legion vote:
  **Approve**, plurality quorum, 9 of 9; no dissent.
- Triage: 5 open → done 0, duplicate 0, wont-do 4 (MAU-T15, MAU-T20–T22), kept 1 (MAU-T24); new tasks 3
  (MAU-T31 to MAU-T33).
