---
codex: 1
project: Unity.Incognito
layer: research
status: living
updated: 2026-10-22
---

# Incognito — reverse-engineering notes

Source: `R:\Projects\_PRE_2022\_PRE_2021\_PRE_2020\_PRE_2017\gmlIncognito\09-05-13_2.19am`
(GameMaker 8, `.gmx` project format, dated 2013-05-09 by the folder name). Read-only reference —
never edit it. Everything below is read directly out of its `objects/*.object.gmx` and
`scripts/*.gml` files, not inferred; file names are given so a future session can re-check the
original. The project is called "Incognito" in `Incognito.project.gmx`; the player object is
`obj_walker`, so the design doc below calls them "the player."

Pitch, from the owner: an experimental point-and-click adventure inspired by Shadowrun, Room 23's
(Nintendo DS) art style, and the idea that a tight, close-in camera makes for a more intimate game.

## 1. Player locomotion — `objects/obj_walker.object.gmx`

Not a literal `sin()` call — it's a **hand-rolled triangle wave**, which is what reads to the eye
as "sinewave movement":

```
walkUp = true; walkAmount = 5.0; walkSpeed = 0.3; walkY = 0;
// per step, while mode == "walking":
if (walkUp) { if (walkY < walkAmount) walkY += walkSpeed; else { walkUp = false; play_footstep(...); } }
else        { if (walkY > -walkAmount) walkY -= walkSpeed; else walkUp = true; }
x += dx;
y = round(initialY + walkY);
```

`walkY` ramps linearly between `-walkAmount` and `+walkAmount` at `walkSpeed` per step, flipping
direction (and firing a footstep sound) at each extreme — a bob, not a bounce. `initialY` is fixed
at `room_height - sprite_height/2` (the player's feet never leave the street).

State machine: `mode` is `"idle" | "walking" | "stopping"`. Left/right key-down (events 65/68)
sets `mode = "walking"`, flips `facing`/`image_xscale`, and accelerates `dx` toward
`walk_speed` (double speed while holding Shift, ramped by `+= 0.1`/`-= 0.1` per step rather than
snapping). Key-up (events 65/68 release) sets `mode = "stopping"`, which applies `dx *= 0.9`
friction until `abs(dx) < 1`, then `mode = "idle"`.

**Aiming** is a separate state (`aimmode`: `"off" -> "equip" -> "aiming"`), toggled by right-click
events (56/57) with a 5-frame alarm between equip and aiming. While `aimmode == "aiming"`,
`mode` is forced `"idle"` (no walking while aiming) and the arm objects swap sprites
(`spr_arm_left_aim1`, `spr_arm_right_aim`/`spr_arm_right_fire`). Firing calls `shoot_bullet()`
(`scripts/shoot_bullet.gml`).

**Arm swing** — `objects/obj_arm.object.gmx` — is the *other* oscillator in the game, also a
triangle wave, not trig:

```
if (swing_forward) { image_angle -= rot_speed; angle += rot_speed; if (angle > swing_range) { angle = 0; swing_forward = false; } }
else                { image_angle += rot_speed; angle += rot_speed; if (angle > swing_range) { angle = 0; swing_forward = true; } }
```
Only runs while `player.mode == "walking"` and not aiming; relaxes back toward 0 when idle. While
aiming, `image_angle` is driven directly by mouse Y (`(mouse_y - room_height/2) / 30`, mirrored by
facing) — the arm points at the cursor.

**Unity port note:** reproduce both oscillators as an explicit ping-pong ramp (`Mathf.PingPong` or
a hand-rolled increment, matching the *linear* feel) rather than `Mathf.Sin` — a true sine reads
as subtly different (eases at the extremes) from what the original actually does.

## 2. Camera — `objects/obj_camera.object.gmx`

This is the "close, intimate" framing the pitch describes. The camera doesn't center the player —
it pins the view near the edge **in the direction the player is facing**, so there's always more
room ahead of the player than behind:

```
if (player.x >= view_wview/2 + player_width && player.x < room_width - view_wview/2 - player_width) {
    if (player.facing == left)  x = player.x - view_wview/2 + player_width;
    else                        x = player.x + view_wview/2 - player_width;
} else {
    x = player.x; // clamp near room edges
}
y = mouse_y; // vertical pan follows the mouse directly
```

(`player_width` is cached once in the Create event from `spr_player`, 512px wide.) Vertical framing
is tied straight to `mouse_y`, uncapped in this snippet — the rebuild should decide a sensible
vertical clamp/deadzone since the original room is a flat 1080px-tall strip with the player's feet
fixed.

`obj_camera` is only the *target*; the actual view follows it through GM8's view settings in
`rooms/street00.room.gmx` (view 0: `objName="obj_camera" wview="1920" hview="1080" hborder="910"
vborder="32" hspeed="20" vspeed="-1"`). So the view keeps the camera object within a 50px
deadzone either side of centre (960 - 910), moves at most 20px per step horizontally (a facing flip
is a ~0.75s swing across 896px, not a cut), and GM clamps it to the room. Because `hview` equals
the room height, the view can never move vertically: `y = mouse_y` had no visible effect in the
original. The rebuild's vertical pan is therefore a design decision (BIBLE.md, Decisions made).

View object is swapped to the cursor (`view_object[0] = cursor`) when a console/hacking overlay is
"inverted" on (see §5) — the camera handoff that makes the HUD scan feel like you're now looking
*through* the UI rather than the world.

## 3. Eye-tracking cursor — `objects/obj_eye.object.gmx`, inlined copies in `obj_walker` and
`objects/obj_npc.object.gmx`, and the literal OS cursor object `objects/obj_mouse.object.gmx`

Two separate things share the name "eye":

- **The character's eyeball** (drawn in-world, at the player/NPC's eye socket) — a
  `spr_eyewhite` + `spr_eye_ball` (pupil) + `spr_eyelid_left`/`spr_eyelid_right` stack. Socket
  position flips with facing (`offsetX` swaps between a left-facing and right-facing constant).
  Pupil position is **mouse-relative parallax**, not literally mouse-locked:
  ```
  pupiloffsetX = 7 + ((mouse_x - player.x) / 160);
  pupilX = round(x + pupiloffsetX);
  pupilY = round(y - 3 + (mouse_y / 160));
  ```
  Dividing by 160 is what keeps the pupil inside the socket instead of flying off with the real
  cursor — it's a small, damped offset, not 1:1 tracking. While aiming, `pupiloffsetX` is instead
  snapped to a fixed in/out-facing value (no tracking) because the character is staring down the
  sights. **Blinking** is a randomized idle timer: each step, `irandom_range(0, eyelid_delay) == 0`
  (eyelid_delay = 160) has a 1-in-160 chance to start a blink; once started, `eyelidDY` ramps the
  eyelid sprite down over the eye then back up (closing/opening), gated by an `eyelid_closing`
  flag and an `eyelid_direction` state machine — the same shape as the walk bob, reused for blinks.
  NPCs (`obj_npc`) run an identical copy of this logic for their own eyes, plus a suspicion-percent
  label drawn above their head (see §4).

- **The OS mouse cursor** (`obj_mouse`, sprite `spr_cursor_eye` by default — the *cursor itself is
  drawn as an eye/eyeball icon*, separate from the in-world character eyeballs above). It tracks
  real mouse position 1:1 (`x = mouse_x; y = mouse_y;`) and swaps its own sprite by `mode`:
  `"look"` -> `spr_cursor_eye`, `"talk"` -> `spr_cursor_talk`, `"pointer"` -> `spr_cursor_pointer`,
  or `spr_cursor_reticle` whenever `player.aimmode == "aiming"` (aim reticle overrides mode). Mouse
  wheel up/down (events 54/53) toggles `mode` between `"look"` and `"talk"` only — `"pointer"` is
  set elsewhere (by an NPC left-click, see §5) and is not reachable by scrolling.

**Unity port note:** this is two distinct eye systems — a diegetic in-world eyeball glued to each
character's face (parallax pupil + blink) and a cursor-replacement icon shaped like an eye. Both
should exist; don't collapse them into one object.

## 4. NPCs and the suspicion/stealth meter — `objects/obj_npc.object.gmx`

NPCs reuse the player's walk-bob and eye-tracking verbatim, plus:

- **Patrol**: walk toward `waypoint_x` at a fixed `walk_speed`, flipping `facing`/`image_xscale`
  toward the target (`a_near_b(x, waypoint_x, 20)` — `scripts/a_near_b.gml` — gates "arrived").
  Details (re-read for U.INC-T8): `walk_speed = 5` is a global (`scripts/globals.gml`); the NPC
  starts `"idle"` facing right and a 2-step `alarm[0]` flips it to `"walking"`. There is exactly
  one waypoint, `waypoint_x = previous_waypoint_x = 12000` — past street00's 11520px end — and the
  street00 instance stands at x = 2400, so the original NPC simply walks right off the street.
  Arrival does nothing: `hspeed` is left as is, so an NPC that did reach its waypoint would
  overshoot and jitter back and forth around it. The `"stopping"` branch (`hspeed *= 0.9` until
  `< 1`) exists but nothing in `obj_npc` ever enters it.
- **Suspicion** (a visible `%` label drawn over the NPC's head via `draw_text_outline`) is a
  per-step accumulator, `suspicion += suspicion_rate`, where `suspicion_rate` is recomputed every
  step from three independent cues (comment headers are the original author's own, kept verbatim):
  ```
  // How Close?
  if   (distance(player, npc) <= view_wview/2) suspicion_rate = 0.2;
  elif (distance(player, npc) <= view_wview)   suspicion_rate = 0.1;
  else                                          suspicion_rate -= 0.1;

  // Are they facing each other?
  if (facing_each_other) {
      // is Player looking down? (i.e. console/menu open, mouse_y > room_height/2)
      if (looking_down) suspicion_rate -= 0.2;
      else              suspicion_rate += 0.1;
  } else {
      suspicion_rate -= 0.1;
  }
  ```
  So: being close and in view *and* facing each other while the player *isn't* "looking down"
  (hasn't opened the console/HUD) raises suspicion fastest; looking away, being far, or having a
  menu open all bleed it off. This is the Shadowrun-flavored social-stealth system — no cover
  mechanic, just proximity + mutual eye contact + whether you looked like you were doing something
  else. `suspicion` itself is never clamped or consumed in what's wired up here (no detected/alert
  state fires) — the rebuild should decide thresholds and consequences; the original only draws
  the running percentage.

  Three literal-source details the summary above flattens (re-read for U.INC-T8):
  `distance` is `point_distance(player.x, 0, x, 0)` — horizontal only. The far branch is
  `suspicion_rate -= 0.1` with no reset, so out of range the rate keeps falling every step (the
  facing adjustment also accumulates there) and suspicion drains faster the longer you stay away;
  the near/mid branches assign, so stepping back into range resets it. And `facing_each_other` is
  only `npc.facing` opposite `player.facing` — position is never checked, so standing back to back
  also counts. `suspicion` is unclamped and can go negative.
- **Interaction** depends on the cursor's `mode` at click time (collision/left-click event,
  `eventtype="6" enumb="4"`): `"talk"` sets `mode = "talking"` and walks the NPC to stand next to
  the player; `"look"` calls `type_text(mouse_x, mouse_y, description)` (the NPC's floating
  description string, e.g. *"A young woman with tell tale red eyes and a meandering gait. #Bless
  the young and stoned."* — `#` is this engine's inline newline); `"pointer"` opens the
  hacking/console overlay targeted at that NPC (see §5) and switches the active camera view to it.
  NPCs also carry a `sheep_id` (e.g. `"0097"`) used by the `list sheep` console command (§6) —
  "sheep" is the in-fiction slang for a targetable civilian.

## 5. Hacking / console overlay — `objects/obj_stealth.object.gmx` (despite the file name, this is
the HUD-fade/"scanning" overlay, not a stealth mechanic — naming drift in the original project),
`objects/obj_console.object.gmx`, `objects/obj_gui_console.object.gmx`, `objects/obj_program.object.gmx`-family

A full-room black overlay (`spr_linewall`, scaled to room size) that cross-fades (`image_alpha +=
/-= fadespeed` at 0.025/step) between `"off" | "fadein" | "fadeout" | "scanning"` modes. Clicking
an NPC in pointer mode (§4) sets `mode = "scanning"`, which on its first pass brings three
`obj_program` icons and an `obj_icon_pistol` "onscreen" and settles the overlay at 50% alpha — a
translucent cyberpunk dashboard laid over the paused scene, themed like you've turned the NPC into
a terminal. `inverted` (toggled by a key event in `obj_walker`, 81) is the "full console" variant:
it switches `view_object[0]` to the cursor itself (camera now follows the UI, not the world),
forces cursor `mode = "pointer"`, and fades `gui_console` in/out — this is the state where you're
no longer looking at the street, you're looking at the terminal.

## 6. Typed command console — `scripts/exec_command.gml`, `scripts/command_list.gml`,
`scripts/split_string.gml`/`string_split.gml`, `objects/obj_command_prompt.object.gmx`,
`objects/obj_typewriter.object.gmx`

A literal typed-command terminal, lower-cased and space-split (`split_string`), dispatched by a
switch statement:

- `help` — lists `list` and `reboot`/`overwrite` (the latter two are referenced in the help text
  but have no case in the switch yet — stubs in the original).
  `list` — delegates to `command_list.gml`, itself a nested switch: `list all` (stub, "TODO: list
  all"), `list sheep` (enumerates every `obj_npc` instance's `sheep_id` as `SHP#<id>`).
- `clear` — empties the output.
- anything else — `"Command not recognized"`.

Output strings use `#` as an explicit line break (consistent with NPC `description` strings) and
`##` for a blank line. `obj_command_prompt` itself draws the output in one go (its output fade,
`alarm[1]`, is commented out, so output stays until replaced or `clear`ed); `sfx_type` plays per
keystroke (not while Shift is held). The character-by-character teletype is
`obj_typewriter`/`scripts/type_text.gml`, used for the `look` descriptions (§4), not the console.
The prompt is a 580x200 panel that eases in from the left (`x += 1 + ceil(|x| * 0.1)` per step),
slides out at 20px/step, and blinks a `_` i-beam every 30 steps.

## 7. Rooms and world — `rooms/*.room.gmx`, `objects/obj_room*.object.gmx`, `objects/obj_cloud*`,
`objects/obj_rain_emitter.object.gmx`, `objects/obj_prop_*`

Seven rooms: `titlescreen`, `startup`, `street00`, `street01`, `shop`, `gamescreen`, `room2`.
`street00` is a single wide side-scrolling strip — 11520×1080 px at 60 steps/sec — confirming the
whole game plays out on one long street rather than discrete screens. Parallax/atmosphere layer:
four `obj_cloud1..4` (a `obj_cloud_parent` drives shared drift behavior), `obj_rain_emitter` +
`obj_raindrop` for weather, `obj_lamplight01` for pooled light under streetlamps, and
`obj_prop_lamppost`/`obj_prop_mailbox`/`obj_prop_back`/`obj_prop_front` for foreground/background
dressing (front props render in front of the player, back props behind — a cheap depth trick worth
keeping as sorting layers in Unity). Numbered backgrounds (`bg_street00_00`..`_05`,
`bg_street01_00`..`_07`) are the per-layer parallax art slices.

## 8. Audio inventory — `sound/*.sound.gmx`

Footsteps: 16 `sfx_step_concrete##` variants cycled by `scripts/play_footstep.gml` (called once per
walk-bob cycle, §1) for non-repetitive footfall. `sfx_whoosh` on facing-flip, `sfx_holster`/
`sfx_reload` for the weapon, `sfx_door_open`, `sfx_fire_cannon`, `sfx_type` per teletyped character
(§6), and `whispers`/`whispers_new` (ambient). Eight music tracks (`music_constancy2/3`,
`music_darknessiscoming`, `music_dubferal`, `music_gagool`, `music_interloper`, `music_mta`,
`music_oppressivegloom`, `music_tearoots`, `music_theme`) selected by `scripts/play_random_music.gml`.
Most assets have a legacy and a `_new` variant (re-recorded/normalized pass); prefer the `_new`
ones when porting.

## Open questions the source doesn't answer

- No wired-up consequence for suspicion reaching 100% (no alert/detected state in the objects
  read) — the rebuild needs to design what happens, since the original only displays the number.
- `help`'s `reboot`/`overwrite` commands and `list all` are stubs with no implementation to port —
  treat as design-your-own, not reconstruction. (Decided in U.INC-T10 — see BIBLE.md "Decisions
  made".)
- No save/menu/inventory objects were found — this was a short vertical-slice prototype, not a
  full game; the rebuild's scope (per the ticket breakdown) covers the slice that exists.
