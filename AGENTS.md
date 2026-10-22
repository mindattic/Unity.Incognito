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
