# Unity.Incognito

A Unity 6 rebuild of *Incognito*, a 2013 GameMaker point-and-click stealth prototype: a
Shadowrun-flavored cyberpunk street, a camera held deliberately close instead of a wide adventure-
game establishing shot, and a mouse cursor that doubles as the player character's own tracking
eyeball.

[![Unity](https://img.shields.io/badge/Unity-6000.4.3f1-black)](ProjectSettings/ProjectVersion.txt) [![Status](https://img.shields.io/badge/status-reconstruction-orange)](docs/BIBLE.md)

There is no build yet — this repo currently holds the Unity project scaffold and the reverse-
engineering record the rebuild is being built from.

## Why

The original prototype (GameMaker 8, 2013) tried a few things worth rebuilding properly:

- **An intimate camera.** Instead of centering the player, the view pins near the screen edge
  opposite the player's facing direction, so there's always room ahead and the frame feels close
  rather than panoramic.
- **A cursor that's also a character.** The player's eyeball tracks the mouse with a damped
  parallax offset and blinks on its own, and the OS cursor itself is drawn as an eye icon that
  changes with context (look / talk / aim).
- **Hand-rolled "sinewave" movement.** A triangle-wave bob on every walk cycle and arm swing —
  not a static walk pose, and not literal trig, but reads as alive.
- **Social stealth.** No cover mechanic — NPC suspicion rises from proximity and mutual eye
  contact, and falls when you look away.

See [`docs/RECONSTRUCTION.md`](docs/RECONSTRUCTION.md) for the full mechanic-by-mechanic record
pulled from the original `.gml`/`.gmx` source, and [`docs/BIBLE.md`](docs/BIBLE.md) for the
rebuild's brief. Work is tracked as tickets under epic `U.INC-A` — see
[`docs/USER_STORIES.md`](docs/USER_STORIES.md).

## Status

Reconstruction-complete, implementation not yet started. Every system below is an open, approved
ticket (`tickets ticket list --repo Unity.Incognito`), ready for a session or autopilot to pick up
in dependency order:

- Player locomotion (walk bob, sprint, facing)
- Close-framing camera
- Eye-tracking cursor (pupil parallax + blink + mode sprites)
- Cursor mode cycling (look / talk)
- Hacking/console overlay
- NPC patrol + suspicion meter
- Weapon equip / aim / fire
- Typed command console
- Rooms, parallax props, audio

## Original source

`R:\Projects\_PRE_2022\_PRE_2021\_PRE_2020\_PRE_2017\gmlIncognito\09-05-13_2.19am` — read-only
reference, never edited by this project.
