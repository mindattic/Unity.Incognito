# Unity.Incognito — agent entrypoint

Read the shared protocol at ..\mindattic-agent-standard\AGENTS.md, then this file, then
docs/RECONSTRUCTION.md and docs/BIBLE.md.

## What this is

A clean Unity 6 rebuild of *Incognito*, a 2013 GameMaker 8 point-and-click stealth prototype
(original project: `R:\Projects\_PRE_2022\_PRE_2021\_PRE_2020\_PRE_2017\gmlIncognito\09-05-13_2.19am`
— read-only source material, never edit it). Shadowrun-flavored cyberpunk, Room 23 (DS) inspired
pixel art, a deliberately close/intimate 2D side-on camera, and a mouse cursor that doubles as the
player character's tracking eyeball.

`docs/RECONSTRUCTION.md` is the reverse-engineering record: every mechanic below is read directly
out of the original `.gml`/`.gmx` source, not guessed. Treat it as the ground truth for what the
original game *did*; `docs/BIBLE.md` is the brief for what this rebuild *will do* (same mechanics,
Unity-native implementation, free to clean up incidental GM8 quirks).

## Laws (do not break)

1. **The reconstruction is the source of truth.** Before implementing any ticket, re-read the
   relevant section of docs/RECONSTRUCTION.md and the original `.gml`/`.object.gmx` file it cites.
   Don't invent mechanics the original didn't have; don't silently drop ones it did.
2. **Content stays data where the original made it data** (room layout, NPC dialogue/description
   strings, suspicion tuning numbers) so later rooms/NPCs are authored, not coded.
3. **No editor-only state that can't be reproduced from code or data.** Prefabs/scenes are fine,
   but gameplay logic (movement, camera, eye tracking, suspicion) lives in C#, not in
   Inspector-only wiring nobody can diff.
4. **Done = verified by play.** Before claiming a ticket complete, actually run the scene (Unity
   Editor play mode or a build) and confirm the behavior against docs/RECONSTRUCTION.md, not just
   "it compiles."
5. **One ticket, one story, one commit**, per the workspace standard. Work the tickets in
   dependency order (`tickets ticket list --repo Unity.Incognito`, `tickets ticket show <id>` for
   prerequisites).
6. Commit at verified milestones only; end messages with the session attribution lines.

## Map

- `docs/RECONSTRUCTION.md` — the reverse-engineered mechanics: player locomotion, camera framing,
  eye/cursor tracking, NPC suspicion, weapon state machine, command console, room/asset inventory.
- `docs/BIBLE.md` — the rebuild's design brief, derived from the reconstruction.
- `docs/USER_STORIES.md` — generated from the ticket database (`tickets db render`); don't hand-edit.
- Original source for reference only: `R:\...\gmlIncognito\09-05-13_2.19am\{objects,scripts,rooms,sprites,sound,background}`.

## Rebuild standard (owner 2026-10-23)

This repo upgrades an old prototype to Unity. The gameplay is kept exactly; the code is not. Everything is
rebuilt as cutting-edge, best-practice Unity code design and organisation. Unity.SpaceGod and Unity.GridGame
are the reference architectures: read their AGENTS.md, README and docs/BIBLE.md before designing anything here.

1. **REA the original in its totality first.** Every object, script, room, sprite, sound and data file is
   reverse-engineered and recorded in docs/RECONSTRUCTION.md before it is rebuilt. Gameplay matches the original;
   where the rebuild deviates, the BIBLE says so and why.
   **Broken things are fixed.** Where the original's source is plainly broken (a random that is not random, a draw
   call commented out, a feature that can never be reached, sound that ships switched off), the rebuild plays the way
   the game was clearly meant to, not the way the bug made it, and the BIBLE records each one as a fixed defect.
   Everything that is not broken matches the original exactly: never substitute or approximate a working behaviour.
2. **Core is pure C#.** The rules live in an engine-free Core assembly (`noEngineReferences`, no `UnityEngine`),
   tested with `dotnet test` outside Unity. State changes only through commands that produce events;
   presentation reads state and plays events, and never mutates the game.
3. **Assemblies have hard boundaries.** Core, Runtime (MonoBehaviours, presentation), Editor and Tests are separate
   asmdefs with one-way references.
4. **Content and tuning are data.** Levels, items, enemies, text and every tuning number live in data files or a
   settings registry, validated on load, not hard-coded in behaviour.
5. **Built in code, not by hand in the Editor.** Scenes, UI and objects are composed in code (or generated), so
   nothing depends on Inspector wiring the owner has to click together.
6. **Modern Unity only.** Unity 6, URP behind a pipeline-agnostic layer, the Input System, UI Toolkit or the shared
   MindAttic components, Unity.Shared profiles and settings, multiplayer-ready seams (commands carrying a player id,
   one authority, no global current-player singleton).
7. **Done means verified by play, not asserted.** Unity 6000.4.3f1 runs headless here, so every ticket that touches
   Unity-side code or assets runs a headless import/compile of its own worktree (`-batchmode -quit -projectPath <worktree>`,
   never touching an editor the owner has open), runs the EditMode (and, where relevant, PlayMode) tests it touches, and
   commits any `.meta` files Unity writes, before it lands. Landing Unity code that was never compiled is not allowed. Core tests green, a build that launches, and the feature seen
   working (screenshots or a scripted run) before a ticket closes.
8. **Finite.** Once every story from the reconstruction is built, the repo is a complete, polished proof of concept
   of the original, and is done.
