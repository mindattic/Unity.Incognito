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

**Aiming** is a separate state (`aimmode`: `"off" | "equip" | "aiming"`). Re-read for U.INC-T9
(GM mouse event numbers: 53/54 = global left/right *pressed*, 56/57 = global left/right
*released*):

- **Draw/holster** — `scripts/toggle_gun.gml`, called from the `Z` key-press event (`9`/`90`) and
  from clicking the HUD `obj_icon_pistol` (`obj_mouse_gui`). `"off"` -> `"equip"` plays
  `sfx_reload` and puts arm2 in `spr_arm_right_drawing`; anything else -> `"off"` plays
  `sfx_holster` and returns both arms to `spr_arm` (so holstering works mid-aim too).
- **Aim** — right *pressed* (54), only from `"equip"`: `aimmode = "aiming"`, `mode = "idle"`,
  `dx = 0`, `sfx_whoosh`, arm1 -> `spr_arm_left_aim1`, both arm angles zeroed, `alarm[0] = 5`.
  Five steps later the alarm (event `2`/`0`) settles arm1 -> `spr_arm_left_aim2` and arm2 ->
  `spr_arm_right_aim`. Right *released* (57) drops back to `"equip"` (arm2 drawing, arm1 rest). The
  walk-key events are gated on `aimmode != "aiming"`, so you can't walk or turn while aiming —
  aiming is *hold* right button, not a toggle.
- **Fire** — left *pressed* (53) while aiming: arm2 -> `spr_arm_right_fire` and `shoot_bullet()`;
  left *released* (56) puts arm2 back to `spr_arm_right_aim`.

`shoot_bullet.gml`: the bullet spawns 500px along arm2's angle from its pivot (mirrored when facing
left) and then 88px up, travels at `speed = 60` px/step in the arm's direction (`image_angle - 180`
facing left), plays `sfx_fire_cannon` and puffs `ef_smoke` at the muzzle. It also nudges both arms'
`x` by 10px along the shot, but that's a no-op: the arms' own step event re-pins `x` to the player
before they are drawn. `obj_bullet` (`spr_bullet`, 12x12) just destroys itself once it leaves the
room. `obj_npc`'s collision with `obj_bullet` shoves the NPC 100px away from the side the bullet
came from and destroys the bullet — no health, death or suspicion change is wired to a hit.

**Arm swing** — `objects/obj_arm.object.gmx` — is the *other* oscillator in the game, also a
triangle wave, not trig:

```
if (swing_forward) { image_angle -= rot_speed; angle += rot_speed; if (angle > swing_range) { angle = 0; swing_forward = false; } }
else                { image_angle += rot_speed; angle += rot_speed; if (angle > swing_range) { angle = 0; swing_forward = true; } }
```
Only runs while `player.mode == "walking"` and not aiming; relaxes back toward 0 when idle (and
holds still while `"stopping"` — there's no branch for it). Defaults `swing_range = 3`,
`rot_speed = 0.06`; the walk-left key sets 6/0.24 while Shift is held and 3/0.06 otherwise (the
walk-right key never touches them). `arm1` starts `swing_forward = true` and `arm2` `false`
(`scripts/start_game.gml`), so the arms swing in opposition. Both sit at `player.x ± 60` (behind
the facing direction), `player.y + 220`; arm1 draws in front of the body (depth -1), arm2 behind
(depth 1). While aiming, `image_angle` is driven directly by mouse Y (`(mouse_y - room_height/2) /
30`, mirrored by facing — at most ±18°) — the arm points at the cursor.

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
  or `spr_cursor_reticle` whenever `player.aimmode == "aiming"` (aim reticle overrides mode). A
  global **right press** (mouse event 54) toggles `mode` between `"look"` and `"talk"` only —
  `"pointer"` is set elsewhere (by an NPC left-click, see §5) and is not reachable by toggling.
  (Re-read for U.INC-T6: events 53/54 are global left/right *pressed*, not the wheel — GM's wheel
  events are 60/61 and nothing in the project uses them. obj_mouse's 53 is an empty,
  commented-out click-to-walk. The same right press also enters aim from `"equip"` in obj_walker,
  so with the gun out it aimed and silently flipped the mode underneath the reticle.)
  `last_mode` is kept so the console toggle (§5) can restore the mode after forcing `"pointer"`.

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
  Talk details (re-read for U.INC-T6): the click sets `waypoint_x = player.x + sprite_width` when
  the NPC is left of the player (else `player.x - sprite_width`) — the *far* side, so the NPC walks
  through the player. The walking branch runs for `"talking"` too (bob, head for the waypoint), and
  like patrol it never stops on arrival; the stop-and-face-each-other block (NPC faces the player,
  player turned to face the NPC) sits inside the `"stopping"` branch under `if (mode ==
  "talking")`, so it can never run. While the player holds a walk key, obj_walker re-arms every
  talking NPC's `alarm[0]` (200 steps holding right, 100 holding left); when it fires the NPC goes
  back to `"walking"` toward `previous_waypoint_x`. `type_text` destroys any typewriter already
  printing the same text, then spawns `obj_typewriter` at the click: one character per 2 steps,
  then `alpha -= 0.01` per 2 steps until gone, over a 0.5-alpha `spr_desc_border` box drawn from
  (x - 40, y - 20).
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

Re-read for U.INC-T7 — what the source actually wires, which differs from the summary above:

- `obj_stealth` is never created (no room places it, nothing calls `instance_create` on it), and
  nothing anywhere sets any overlay's `mode = "scanning"`, so the 50%-alpha `spr_linewall`
  dashboard and the three-programs-plus-pistol rise never run. `obj_gui` and `obj_gui_menu` are
  near-copies with the same dead "scanning" branch.
- **The console toggle** is obj_walker's key event 81 (Q) flipping the global `inverted`. On:
  `cursor.mode = "pointer"`, `over_gui = true`, `gui_console.mode = "fadein"`,
  `view_object[0] = cursor`, `view_hborder[0] = 32`, `view_hspeed[0] = 10` — the view now
  edge-scrolls with the cursor. Off: `cursor.mode = cursor.last_mode` (only ever set to "look", in
  obj_mouse Create), `over_gui = false`, `gui_console.mode = "fadeout"`, `view_object[0] = camera`,
  `view_hspeed[0] = 20`. This is the only way into pointer mode.
- `gui_console` (`obj_gui_console`, created by `scripts/start_game.gml`) draws `spr_circuitwall1`
  (1920x1080, ~10% alpha circuit traces) in the GUI layer, fading `image_alpha` by 0.025/step up
  to 1 on "fadein" and down to 0, then "off", on "fadeout".
- **The pointer click** on an NPC (obj_npc left-pressed, `cursor.mode == "pointer"`):
  `program[0].state = "onscreen"`, `program[0].sprite_index = spr_program_active`,
  `console[0].state = "onscreen"`, `console[0].sprite_index = spr_console_selected`,
  `gui_console.selected_object = object_index` (never read), `view_object[0] = object_index`,
  `view_hborder[0] = view_wview/2` — the view centres on the NPC.
- `program[0..2]` (`obj_program`, spr_program 113x86) are created by start_game.gml at x = 40,
  180, 320, resting just below the screen (`initialY = room_height`, +40, +80). "onscreen" eases
  up `y -= 1 + abs(y) * 0.01` per step to `endY = room_height - sprite_height`; "offscreen"
  drops 10px/step back to `initialY`. Only program[0] is ever sent onscreen.
- `console[0..2]` (`obj_console`, spr_console 580x200) are the typed command prompt again, at
  `y = room_height - 300`, input line at `y + 170`, output at `y + 35`, and a right-aligned
  `"CPU: " + round(cpu_power/active_cpus) + "/" + cpu_power + "%"` (or `cpu_power/cpu_power`
  with no active CPUs; `cpu_power = 100`) at `(inputX + 450, y + 8)`. Its slide-in/out and
  keyboard handling match obj_command_prompt (§6). It only takes keys while
  `selected_id == program_id` (`selected_id` starts at 0, so console[0] does).
- `obj_mouse_gui` handles left clicks on the GUI layer: on a program icon it plays `sfx_flick`
  (the open/close-its-console logic is commented out); on a console it selects it; on the pistol
  icon it calls `toggle_gun()`. `active_cpus` is only changed in that commented-out code, so
  obj_walker's `active_cpus == 0` movement gate never closes.

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
`bg_street01_00`..`_07`, `bg_shop_00`..`_02`) are 1920×1080 slices laid edge to edge as room
tiles (depth 1000000).

Corrections from re-reading the rooms for U.INC-T11:

- **No parallax on the streets.** The slices are tiles at one depth that scroll 1:1 with the
  view; nothing moves at a different rate. The only scrolling layer is the title screen's
  `bg_city_black` (tiled, `hspeed = -2`) under the opaque `bg_titlescreen`.
- **The clouds were never used.** `obj_cloud1..4` have no sprite and no instance in any room
  (`obj_cloud_parent` only drifts at `hspeed -1`). `obj_lamplight01` is not placed either: each
  `obj_prop_lamppost` draws `spr_lamplight01` itself, 100px above its top, blanking it on an
  `irandom_range(0,80) == 0` roll each step. `obj_prop_back`/`_front` are unplaced duplicates of
  the lamppost/mailbox; the rooms use `obj_prop_lamppost` (depth 10) and `obj_prop_mailbox`
  (depth -10), each typing its description on a left click.
- **Rain**: `obj_rain_emitter` makes 6 `obj_raindrop`s per step at a random x across the view
  and a random y up to one room above; each falls 40–50px and drifts 3–6px left per step (spr_rain,
  1×4, alpha 0.75, depth -1) and is destroyed below the room.
- **Rooms in play**: `titlescreen` (any key / left mouse → `start_game()`, Esc quits, F1 toggles
  fullscreen) → `street00` "Bleecher Street" (11520 wide; 4 lampposts, 2 mailboxes, 1 NPC, rain,
  the intro poem typed at `room_width - 650, 100`, a floating `obj_exit` door to `shop` at
  9504,480 entered with W within 300px, side exits to `street01` at both ends) ↔ `street01` "2nd
  Avenue" (15360 wide, blockout art, rain, side exits back to `street00`) and `shop` "Simon's Pawn
  and Loan" (5760 wide, blockout art, an `obj_barrier` at x 800, a right-edge exit back to
  street00 at x 9500). `obj_exit_side` sets `next_room`/`enter_from`, puts the player on
  `autowalk` and fades; `obj_room` places the player at `room_width - player_width` ("right"),
  `player_width` ("left") or `enter_x` ("x") and draws the room's name at 20,20. `startup`,
  `gamescreen` and `room2` are unused copies.

## 8. Audio inventory — `sound/*.sound.gmx`

Footsteps: 16 `sfx_step_concrete##` variants cycled by `scripts/play_footstep.gml` (called once per
walk-bob cycle, §1) for non-repetitive footfall. `sfx_whoosh` on facing-flip, `sfx_holster`/
`sfx_reload` for the weapon, `sfx_door_open`, `sfx_fire_cannon`, `sfx_type` per teletyped character
(§6), and `whispers`/`whispers_new` (ambient). Eight music tracks (`music_constancy2/3`,
`music_darknessiscoming`, `music_dubferal`, `music_gagool`, `music_interloper`, `music_mta`,
`music_oppressivegloom`, `music_tearoots`, `music_theme`) selected by `scripts/play_random_music.gml`.
Most assets have a legacy and a `_new` variant (re-recorded/normalized pass); prefer the `_new`
ones when porting — but almost none of the `_new` files survive in `sound/audio` (only
`music_gagool_new`, `sfx_flick_new`, `whispers_new`), so the port uses the legacy files.

Correction (U.INC-T11): **the original played no music.** `obj_music`'s `audio_play_music` call
and all of `play_random_music.gml` are commented out; only the per-room choice survives as data
(`current_track`: `music_oppressivegloom` for street00/street01 and, by the global default, the
title; `music_dubferal` for the shop). `sfx_type` plays on a command-prompt keystroke (not with
Shift, and not if already playing), not per teletyped character; `obj_typewriter` is silent.

## 9. Port verification — U.INC-T13

The other copy of the project, `R:\Projects\_PRE_2022\_PRE_2021\_PRE_2020\GameMaker\gmlIncognito`
(`Incognito.gmx`, saved 2018-10-28, with an `Install\Incognito.exe` build), matches the
09-05-13 snapshot above file for file except `obj_music` (its `audio_is_playing` guard is now
commented out too) and `play_random_music.gml` (the random pick is uncommented, but nothing
plays it). Neither copy plays music, so §8 holds for both.

Missed by the sections above: `obj_debug` (created by `start_game.gml`, persistent, so live in
every room) quits on Escape, toggles fullscreen on F1, toggles a debug readout (current room,
`player.x`, mouse and view coordinates) on F12. `obj_walker`'s Down arrow takes 0.1 off
`walkAmount` (the bob height); its Up arrow is commented out. `obj_titlescreen` starts the
window fullscreen.

Play-through of the Windows build (batch-built from `EditorBuildSettings`): title → any key or
click → Bleecher Street entered from the right; both side exits to 2nd Avenue and back; the
door to Simon's Pawn and Loan (W within 300px); the shop barrier at x 800; the shop's
right-edge exit back out at x 9500. Every arrival matched obj_room's placement. The console
view (Q), gun draw and aim (Z, hold right) with the reticle, rain, lamp light, mailboxes, the
intro poem and the suspicion % all showed up. The PlayMode test
`RoomTraversalPlayTests` plays the same route on the built scenes. Gaps went to tickets:
the character art is still placeholder (U.INC-T14), Escape/F1 only work on the title
(U.INC-T15), and the dev sandbox scene and `Unity.Incognito` window title ship in the build
(U.INC-T16).

## Open questions the source doesn't answer

- No wired-up consequence for suspicion reaching 100% (no alert/detected state in the objects
  read) — the rebuild needs to design what happens, since the original only displays the number.
- `help`'s `reboot`/`overwrite` commands and `list all` are stubs with no implementation to port —
  treat as design-your-own, not reconstruction. (Decided in U.INC-T10 — see BIBLE.md "Decisions
  made".)
- No save/menu/inventory objects were found — this was a short vertical-slice prototype, not a
  full game; the rebuild's scope (per the ticket breakdown) covers the slice that exists.
