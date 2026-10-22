---
codex: 1
project: Unity.Incognito
code: U.INC
layer: stories
status: living
updated: 2026-10-21
---
<!-- GENERATED from MindAttic.Tickets/data/Tickets.db by `tickets db render`. Do not edit: this file is rewritten from the database; change stories and tickets with the tickets CLI. -->

# Unity.Incognito — User Stories
> ✅ done (shipped & tested) · 🟡 partial · ⬜ planned. Every ✅ cites the test.

## Priority backlog
_Nothing recorded yet._


## Epic A — Reverse-engineer and rebuild Incognito (2013 GameMaker prototype) in Unity
- **U.INC-US-A1 ⬜** As a player, I walk left/right with a gentle triangle-wave vertical bob (ramping between a min/max offset, not a literal sine) that quickens while sprinting, so movement feels alive instead of robotic.
- **U.INC-US-A2 ⬜** As a player, the camera holds my character pinned near the screen edge opposite my facing direction (leading room ahead of me) while panning vertically with the mouse, so the view stays close and intimate instead of centering me.
- **U.INC-US-A3 ⬜** As a player, my cursor doubles as my character's visible eye: the pupil tracks the mouse with a parallax offset and blinks on a randomized idle timer, and the on-screen cursor sprite itself changes to reflect look/talk/pointer/aim modes.
- **U.INC-US-A4 ⬜** As a player, scrolling the mouse wheel cycles the cursor between look mode (hover an NPC to read a floating description) and talk mode (click an NPC to walk over and start a conversation).
- **U.INC-US-A5 ⬜** As a player, clicking an NPC in pointer mode opens a hacking/console overlay on that NPC (a Shadowrun-style 'matrix' panel), separate from the look/talk interactions.
- **U.INC-US-A6 ⬜** As a player, NPCs patrol between waypoints with the same idle bob animation and raise a visible suspicion meter driven by my proximity, whether we're facing each other, and whether I'm looking down (console/menu open), so stealth has Shadowrun-flavored social detection.
- **U.INC-US-A7 ⬜** As a player, I can draw/holster a weapon through an equip animation, aim with the arm pivoting toward the mouse, and fire a bullet that travels and can hit an NPC.
- **U.INC-US-A8 ⬜** As a player, I can open a typed command console (help/list/reboot/overwrite/clear) that queries nearby objects by type, echoing the original terminal/hacking mini-game.
- **U.INC-US-A9 ⬜** As a player, the street/shop/title rooms are rebuilt as Unity scenes with the original parallax clouds, rain, and lamplight props, playing the original footstep/whoosh/door/type sound effects and music tracks.

## Tickets
> Permanent log of every change and fix — this repo's ticket system. Rows are append-only: never
> delete or reword one; only move its Status (open → closed / wont-do) and fill in Ref. Status is
> a word, not a glyph, so tickets don't count toward the story index. Keep this section last.

| ID | Date | Type | Status | Summary | Ref |
|---|---|---|---|---|---|
| U.INC-T1 | 2026-10-21 | docs | closed | Start the permanent ticket log in the ticket database | — |
| U.INC-T2 | 2026-10-21 | chore | closed | Bootstrap Unity 6 project, workspace docs, and reverse-engineering notes from the 2013 GameMaker source | eae442c |
| U.INC-T3 | 2026-10-21 | feature | closed | Port player locomotion: triangle-wave walk bob, sprint acceleration/friction, and facing flip | 923bc95 |
| U.INC-T4 | 2026-10-21 | feature | open | Port the close-framing camera: edge-pinned horizontal lock by facing with mouse-driven vertical pan | U.INC-US-A2 |
| U.INC-T5 | 2026-10-21 | feature | open | Port the eyeball cursor: pupil parallax tracking, randomized blink, and mode-based cursor sprite swap | U.INC-US-A3 |
| U.INC-T6 | 2026-10-21 | feature | open | Port cursor mode cycling: mouse-wheel toggle between look (hover description) and talk (walk-to NPC, start conversation) | U.INC-US-A4 |
| U.INC-T7 | 2026-10-21 | feature | open | Port the hacking console overlay triggered by pointer-mode click on an NPC | U.INC-US-A5 |
| U.INC-T8 | 2026-10-21 | feature | open | Port NPC patrol and the Shadowrun-style suspicion meter (proximity, mutual facing, player-looking-down) | U.INC-US-A6 |
| U.INC-T9 | 2026-10-21 | feature | open | Port the weapon equip/aim/fire state machine with arm-pivot aiming and a traveling bullet | U.INC-US-A7 |
| U.INC-T10 | 2026-10-21 | feature | open | Port the typed command console (help/list/reboot/overwrite/clear) for querying nearby objects | U.INC-US-A8 |
| U.INC-T11 | 2026-10-21 | feature | open | Port street00/street01/shop/titlescreen rooms as Unity scenes with parallax props, SFX, and music | U.INC-US-A9 |
