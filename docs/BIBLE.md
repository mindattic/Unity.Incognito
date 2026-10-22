---
codex: 1
project: Unity.Incognito
layer: bible
status: living
updated: 2026-10-21
---

# Incognito — rebuild bible

The brief for the Unity rebuild. Every mechanic here is grounded in docs/RECONSTRUCTION.md — read
that first for the "what the original actually did" evidence; this file is the "what we're
building" target, free to make GM8-era implementation details (hard-coded pixel offsets, stubbed
commands) cleaner without changing the feel.

## Pitch

A 2D side-on point-and-click stealth adventure. Shadowrun-flavored cyberpunk setting, Room 23
(Nintendo DS)-inspired pixel art, and a camera that stays deliberately close to the player instead
of the usual wide adventure-game establishing shot — the game's whole premise is that intimacy.

## Pillars

1. **Close camera, not a wide shot.** The view pins near the screen edge opposite the player's
   facing (room to walk into, not room already covered), pans vertically with the mouse. See
   RECONSTRUCTION §2.
2. **The cursor is the character.** The mouse isn't a disembodied pointer — the player (and every
   NPC) has a visible, parallax-tracking eyeball that reacts to where the player is looking, and
   the OS cursor itself is drawn as an eye icon. See RECONSTRUCTION §3.
3. **Stealth is social, not mechanical.** No cover system — suspicion rises from proximity and
   mutual eye contact, and falls when you look away or hide behind the console. See
   RECONSTRUCTION §4.
4. **Movement reads as alive.** A deliberate triangle-wave bob on every walk cycle and every arm
   swing — never a static idle pose while moving. See RECONSTRUCTION §1.

## Systems (status tracked per-story in docs/USER_STORIES.md)

| System | Reconstruction ref | Ticket |
|---|---|---|
| Player locomotion (walk bob, sprint, facing) | §1 | U.INC-T3 |
| Close-framing camera | §2 | U.INC-T4 |
| Eye-tracking cursor (pupil parallax + blink + mode sprites) | §3 | U.INC-T5 |
| Cursor mode cycling (look/talk) | §3 | U.INC-T6 |
| Hacking/console overlay (pointer-mode click) | §5 | U.INC-T7 |
| NPC patrol + suspicion meter | §4 | U.INC-T8 |
| Weapon equip/aim/fire | §1 | U.INC-T9 |
| Typed command console | §6 | U.INC-T10 |
| Rooms, parallax props, audio | §7, §8 | U.INC-T11 |

## Decisions beyond the original (not yet made — flag when a ticket hits these)

- What happens at 100% suspicion (the original never wired a consequence — RECONSTRUCTION, Open
  questions).
- Whether `list all`/`reboot`/`overwrite` console commands get real implementations or stay flavor
  stubs.

## Decisions made

- **Close camera view size / vertical pan (U.INC-T4).** The reference view is the original
  1920x1080 (orthographic size = 540 original px at the placeholder 1px = 1/32 unit scale); the
  horizontal framing reads the live view width from the camera, so other aspect ratios keep the
  same lead-room rule instead of needing a fixed window. The original's `y = mouse_y` was a no-op
  (its room was exactly view-height), so the rebuild makes it a direct, instant, bounded pan: mouse
  height on screen maps linearly onto ±135 original px (one eighth of the view height) around the
  camera's resting height. See `Assets/Incognito/Scripts/CameraRig/CloseFramingCameraState.cs`.

## Laws

See AGENTS.md for the non-negotiables (reconstruction is ground truth, data over hard-coding where
the original did, no unreviewable editor-only state, done = verified by play, one ticket per
commit).
