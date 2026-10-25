---
codex: 1
project: Unity.Incognito
layer: bible
status: living
updated: 2026-10-24
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

## Decisions made

- **Close camera view size / vertical pan (U.INC-T4).** The reference view is the original
  1920x1080 (orthographic size = 540 original px at the placeholder 1px = 1/32 unit scale); the
  horizontal framing reads the live view width from the camera, so other aspect ratios keep the
  same lead-room rule instead of needing a fixed window. The original's `y = mouse_y` was a no-op
  (its room was exactly view-height), so the rebuild makes it a direct, instant, bounded pan: mouse
  height on screen maps linearly onto ±135 original px (one eighth of the view height) around the
  camera's resting height. See `Assets/Incognito/Scripts/CameraRig/CloseFramingCameraState.cs`.
- **Eyeball and eye cursor (U.INC-T5).** Two systems, as in the original: `PlayerEyeball` (obj_eye)
  and `EyeCursor` (obj_mouse), both under `Assets/Incognito/Scripts/Eye/`. The eye keeps obj_eye's
  numbers verbatim (socket -108/+93 px by facing, pupil `7 + dx/160`, `-3 + mouse_y/160`, aim snap
  -1/15, 20px lid at 2px/step, 1-in-161 blink roll). `mouse_y` is read as the cursor's height from
  the top of a 1080px screen — what it equalled in the original's view-tall room — so the T4
  vertical pan doesn't feed back into the pupil. The blink runs in whole 60Hz steps (accumulated
  from frame time) so its odds and lid speed don't change with frame rate. The eye stack renders
  behind the player sprite (obj_eye depth 1 vs. obj_walker depth 0) and shows through the art's
  eye socket; the sandbox placeholder player is therefore spr_player-sized (512x1024, origin
  256,168) with a socket hole. The cursor hides the OS pointer and is an in-world sprite at the
  original 32px size, drawn in front of everything. Original eye and cursor sprites are imported
  verbatim from the 2013 project (`Assets/Incognito/Art/Original/`) at their GM origins. Aiming is
  an `Aiming` flag on both components for U.INC-T9 to set; look/talk scrolling (U.INC-T6) and
  pointer mode (U.INC-T7) write `EyeCursor.Mode`.
- **Look/talk cursor modes (U.INC-T6).** The mouse wheel (either direction) toggles look/talk, as
  the ticket asks; the original actually toggled on a right press (RECONSTRUCTION §3), so a right
  click also toggles while the gun is holstered — with it out, right click only aims (the original
  flipped the mode under the reticle too). Pointer is never reached by toggling. A left click on an
  NPC acts by mode, as obj_npc did: look types its description (per-NPC data on `NpcInteraction`)
  at the click with obj_typewriter's timing and the original `spr_desc_border`; talk walks it over;
  pointer raises `NpcInteraction.PointerClicked` for U.INC-T7. Cleanups: clicks do nothing while
  aiming (that click is the trigger); the talker stands one body width from the player on the side
  it came from instead of walking through to the far side, then coasts to a stop and runs the
  original's unreachable stop-and-face block (it faces the player and turns the player to face it
  if the player is idle and not aiming); it resumes its route 200 steps after the player's last
  walking step whichever way they walk (the original used 100 when walking left); the description
  box fades with its text and is sized to the text rather than `string_width * 3`. See
  `Assets/Incognito/Scripts/Npc/NpcInteraction.cs`, `NpcPatrolState.cs` and
  `Assets/Incognito/Scripts/Typing/`.
- **Typed console stubs (U.INC-T10).** `help`, `list`, `list sheep`, `clear` and "Command not
  recognized" are ported verbatim (`#` line breaks become `\n`). The original's stubs get small,
  consequence-free implementations: `list camera`/`list computer` follow `list sheep`'s pattern
  (`CAM#<id>`, `CMP#<id>`), `list all` prints every type present, and `reboot <id>`/`overwrite
  <id>` resolve a target by handle (`shp#0097`) or bare id and raise an event on its
  `ConsoleTarget` — what a reboot or overwrite actually *does* is left to whichever later ticket
  needs it. "Vicinity" means within one view width (1920 original px) of the player, the same
  outer range the suspicion cue uses; the original listed every NPC in the room. The hacking
  overlay (U.INC-T7) opens it; Escape closes it, and movement keys are ignored while it is open.
  See `Assets/Incognito/Scripts/Terminal/`.
- **NPC patrol routes (U.INC-T8).** The original had one off-street waypoint and never stopped on
  arrival. The rebuild gives each NPC a data route of X waypoints walked in order and looped,
  turning on arrival (`a_near_b` ±20px); a one-waypoint route walks there and coasts to a stop
  through the original's otherwise-unreached `"stopping"` friction. Speed (5px/step), arrive range
  and the 2-step start alarm are as the original. See `Assets/Incognito/Scripts/Npc/NpcPatrolState.cs`.
- **Suspicion meter (U.INC-T8).** Rates, ranges and the accelerating out-of-range decay are the
  original's, as serialized `SuspicionTuning` data. Three cleanups: suspicion is clamped to
  0–100% (the original went negative and past 100); "facing each other" needs each to face toward
  the other (the original also counted back to back); "looking down" is the cursor in the lower
  half of the screen (the original's `mouse_y > room_height/2`, which equalled the screen since
  room and view were the same height). The range bands use the live view width. What 100% *does*
  is still open (below). See `Assets/Incognito/Scripts/Npc/SuspicionMeter.cs`.
- **Weapon (U.INC-T9).** Ported as the original had it: Z draws/holsters, holding the right button
  aims (walking locked), the left button fires a bullet that travels and shoves an NPC 100px on a
  hit (`BulletTarget`). GM quirks cleaned up rather than ported: releasing aim within the 5-step
  raise cancels it (the original's alarm still snapped the arms into the aim pose); the sprint arm
  swing (6°/0.24) applies walking either direction, not only left; the idle relax lands exactly on 0
  instead of jittering. The 10px arm "recoil" in `shoot_bullet.gml` was a no-op and is not ported.
  Aim angle stays screen-relative (cursor height in the view), as the original's was. Bullets are
  swept per frame (linecast) so the 60px/step shot can't skip a thin target. Sounds and the muzzle
  smoke are optional fields until U.INC-T11 imports the assets. `PlayerWeapon` sets the T5
  `Aiming` flag on `EyeCursor` (reticle) and `PlayerEyeball` (fixed gaze) every frame. The sandbox
  NPC carries a `BulletTarget`, and `NpcController` routes the shove into its patrol position. The
  arm and bullet placeholders carry the original sprites' sizes and origins, so pivots and the
  muzzle line up as they did in GM. See `Assets/Incognito/Scripts/Weapon/`.
- **Rooms, props and audio (U.INC-T11).** `Incognito/Build Room Scenes` (`RoomSceneBuilder`)
  writes `Scenes/Rooms/{Titlescreen,Street00,Street01,Shop}.unity` from a data table transcribed
  from the `.room.gmx` files, each on a copy of the PlayerSandbox rig (sandbox ground, markers and
  console targets stripped; the NPC kept only in street00), and puts them in the build with the
  title first. Original slices, props and sounds are imported verbatim (`Art/Original`,
  `Audio/Original`; legacy files, as the `_new` ones are missing). Ported as the original had it:
  the 1:1 tiled street slices (no parallax — there was none), the title's scrolling skyline,
  rain, lampposts with their flickering light and mailboxes in front of the player (each
  clickable for its description), the floating shop door (W within 300px, `sfx_door_open`),
  side exits with autowalk and the 0.03/step black fade, entry placement by `enter_from`, the
  room name top-left, the intro poem on entering street00, footsteps on each bob peak (one of 16 at
  random), the turn-around whoosh, weapon sounds and `sfx_type` on console keystrokes. Decisions
  beyond the original: **music actually plays** (the original's calls were commented out) — each
  room's `current_track`, looped, carried across rooms and switched only when the track changes;
  the random jukebox stays unported. The shop barrier is a real wall (the original's push was
  sign-flipped and stopped no one). Clicking a prop does nothing while aiming, as with NPCs. A
  fresh player rig per scene carries only its facing across a room change (the original's one
  persistent player also kept the gun state). Clouds and the unplaced `obj_lamplight01` /
  `obj_prop_back|front` are not ported (no art, never placed). Text is IMGUI, outlined, at
  fnt_large's size. See `Assets/Incognito/Scripts/World/` and `Scripts/Audio/`.

- **URP behind a pipeline-agnostic layer (U.INC-T24).** `com.unity.render-pipelines.universal`
  17.4.0 (the version Unity.SpaceGod and Unity.Hare also pin), set up as a 2D project: a
  `UniversalRenderPipelineAsset` (`Assets/Incognito/Settings/Incognito_URP.asset`) backed by the 2D
  renderer (`Incognito_Renderer2D.asset`, `Renderer2DData` — no 3D forward/deferred renderer), MSAA
  off and HDR/shadows off (nothing here casts a shadow or needs antialiasing against pixel art),
  assigned in both `GraphicsSettings` and every `QualitySettings` tier. The original's cheap
  front-prop/back-prop depth trick (RECONSTRUCTION.md §7) is now actual Unity sorting layers
  (`ProjectSettings/TagManager.asset`), back to front: `Background` (tiles, title/scrolling
  backdrops), `BackProps` (lamppost, its light, side/door exits), `Default` (player, NPCs, arms,
  bullet, eye stack — unchanged), `Effects` (rain), `FrontProps` (mailbox), `Cursor` (the eye
  cursor, frontmost of all, as before). `sortingOrder` still gives the fine-grained order the
  original's depths encoded; the layer is the coarse band a sprite can never cross out of
  regardless of its order. Core and gameplay code reference only `SpriteRenderer.sortingLayerName`
  (plain Unity, not URP) and never `UnityEngine.Rendering.Universal`; no asmdef gained a pipeline
  reference. See `Assets/Incognito/Editor/{Room,Sandbox}SceneBuilder.cs`,
  `Assets/Incognito/Scripts/Eye/EyeCursor.cs`, `Assets/Incognito/Scripts/World/RainEmitter.cs`.

- **Hacking overlay (U.INC-T7).** Ported from what the original actually wired (RECONSTRUCTION
  §5, re-read): Q toggles the console view, which puts the cursor in pointer mode, fades
  `spr_circuitwall1` in over the screen (0.025/step) and hands the camera to the cursor
  (edge-scrolling within 32px of a side at 10px/step). A pointer click on an NPC hacks it: the
  `spr_program_active` icon eases up at bottom-left, the typed console slides in on the original
  `spr_console_selected` panel at obj_console's layout with its "CPU: 100/100%" readout, and the
  camera centres on the NPC (following it as it walks). The dead `obj_stealth` "scanning"
  dashboard is not ported. Cleanups: Escape closes the console and ends the hack (the icon stays
  up, inactive, and the camera goes back to the cursor); clicking the icon reopens or closes the
  console — what obj_mouse_gui's commented-out code intended; Q is ignored while the console
  has the keyboard (it is a letter then); Q off also closes the console and sinks the icon (the
  original left both up) and restores the cursor mode in use before Q (the original always
  restored "look"); clicks on the icon or panel don't also click the NPC behind them. Q no longer
  opens the prompt by itself. A program-icon click plays obj_mouse_gui's `sfx_flick` (U.INC-T28). See
  `Assets/Incognito/Scripts/Hacking/`.

- **Rooms built from data at runtime, and IMGUI replaced with UI Toolkit (U.INC-T26).** The four
  committed room scenes (`Assets/Incognito/Scenes/Rooms/*.unity`) no longer bake a room's
  background slices, lampposts, mailboxes, rain or exits into the scene file: `RoomBootstrap`
  (Runtime, `Assets/Incognito/Scripts/World/RoomBootstrap.cs`) rebuilds all of it at Awake, every
  time the scene loads, from `GameSettings.Content.FindRoom(roomId)` — the same validated data the
  room/NPC/console catalog already used (U.INC-T22) — via `RoomComposer`, an engine-but-not-editor
  static class the editor's Incognito/Build Room Scenes also calls indirectly (it only resolves and
  hands over the few static art/audio references `RoomBootstrap` needs, as `RoomRuntimeAssets`; it
  never builds the room's actual layout itself any more). A scene's committed content is now just a
  copy of the sandbox player rig plus one "Room Bootstrap" object, so it can never silently drift
  from the data it describes — proven by `Tests/EditMode/RoomSceneBuilderTests.cs`, which reruns
  the builder and asserts the regenerated scenes match (content, not Unity's own per-session
  `fileID`s or its fileID-ordered serialization, both normalized away before comparing). Trade-off:
  `RoomBootstrap` runs at Awake, not `[ExecuteAlways]`, so opening a room scene in the Editor
  without pressing Play shows only the bare player rig; use Play mode, or the fully-authored
  sandbox scene, to look at a room's content without playing.

  The 7 OnGUI (IMGUI) classes — `Room`, `DoorExit`, `DescriptionTypewriter`, `CommandPrompt`,
  `HackingOverlay`, `RoomFade`, `DebugHotkeys` — now draw through `HudRoot`
  (`Assets/Incognito/Scripts/UI/HudRoot.cs`), one code-built `UIDocument`/`PanelSettings` whose
  reference resolution (1920x1080, matched by height) replaces every class's own
  `Screen.height / 1080f` scale math: elements are laid out in plain original pixels and the panel
  scales them. `OutlinedText` now styles a `Label`'s native `-unity-text-outline-*` instead of
  drawing eight offset copies by hand, and `CommandPrompt`'s input is a real `TextField` (native
  focus, caret and backspace) instead of `CommandPrompt` parsing `Event.current` itself — a
  deliberate cleanup, since the original's hand-rolled i-beam was exactly this kind of incidental
  GM-era implementation detail. World-anchored labels (`DoorExit`, `DescriptionTypewriter`) convert
  a world position to panel space with `RuntimePanelUtils.ScreenToPanel`, the same top-left-origin
  screen point every other HUD element's hit-testing already used.

- **Every original sound, as a data catalog (U.INC-T28).** game-content.json's `Sounds` lists
  every sound the rebuild imports — id (the `.sound.gmx` name), file in `Audio/Original`, the
  original's `<streamed>` flag (music and whispers stream, effects decompress on load) and where
  the original plays it — and `RoomSceneBuilder` loads every clip through it by id
  (`LoadSound`), so no sound file is named by a literal in code. Room `Music` and the new
  `TitleScreen` entry are validated against the catalog. Added: `sfx_flick`, played on a
  program-icon click as obj_mouse_gui does (obj_command_prompt's own flick call sits in a
  commented-out block, so it has no other player), and the `whispers` ambient, catalogued with
  `Wired: false` and as `TitleScreen.UnwiredTrack`, never played — titlescreen's obj_music names it
  only in `//current_track = whispers;`, so the title keeps obj_music's default
  music_oppressivegloom. That line is a choice the author commented out, not a bug, so it is not
  "fixed". `Tests/Core.Tests/OriginalAudioParityTests.cs` proves every sound a live original
  object or script plays is catalogued, wired and on disk, and every file on disk is catalogued.

- **Every original sprite and background, as a data catalog (U.INC-T29).** game-content.json's
  `Art` lists every image the rebuild imports — id (the `.sprite.gmx`/`.background.gmx` name),
  file under `Art/Original` (copied verbatim), size, GameMaker origin (`xorig`/`yorigin`, 0,0 for
  backgrounds), whether it imports compressed (painted world art) or uncompressed (pixel sprites,
  HUD), whether it tiles (only the title's `bg_city_black`) and where the original uses it — and
  `Editor/OriginalArt` imports every image through it by id, pivot at the GM origin, so neither
  `RoomSceneBuilder` nor `SandboxSceneBuilder` carries an origin or a file literal any more. Every
  room's background slice is validated against the catalog. The sweep added what live original
  objects and rooms use but the rebuild lacked: `spr_icon_pistol` and `_selected`
  (obj_icon_pistol, toggle_gun), `spr_console` (obj_console), `spr_exit_side` and `spr_barrier`
  (collision masks), `spr_brain_select` (drawn over the NPC while the console is on),
  `spr_cursor_arrow` (obj_mouse_gui's GUI cursor), `spr_stealthwall` (obj_gui_menu's dashboard
  hover backdrop), `spr_black` (obj_fade) and `bg_black` (street00's base layer; the camera already
  clears to the same black). `spr_command_prompt`/`_selected` and `spr_linewall` are imported too
  but catalogued `Live: false`: obj_command_prompt, obj_stealth and obj_fade_stealth are never
  created. Importing is not wiring: the HUD pistol is U.INC-T30, and the brain-select overlay, GUI
  arrow cursor and dashboard hover are U.INC-T39. `Tests/Core.Tests/OriginalArtParityTests.cs`
  proves every image a live original object or room uses is catalogued at its GM origin and size
  and on disk, and (where the original project is on the machine) that all 88 sprites and 21
  backgrounds are accounted for as live, imported or dead, with the transcription and the image
  bytes diffed against the original.

- **HUD pistol icon and GUI-layer clicks (U.INC-T30).** `HudPistolIcon`
  (`Assets/Incognito/Scripts/Weapon/HudPistolIcon.cs`) ports obj_icon_pistol: a 96x96 GUI icon
  40px in from the right edge (start_game.gml's `room_width - sprite_get_width(spr_icon_pistol) -
  40` on the 1920px view) that eases up from below the screen to `room_height - sprite_height`
  (984) with obj_program's own step code (`ProgramIconSlide`), drawn above the room fade (its
  depth -101 is one above obj_fade's -100: HudRoot's new `HudIcons` layer). It shows
  `spr_icon_pistol_selected` while the gun is out and `spr_icon_pistol` once holstered, read from
  the player's weapon state in the world, and a left click on it sends `ToggleGun`, the same
  command Z sends, so toggle_gun's sounds and arm poses follow. **Fixed defect:** the original
  created the icon at `y = room_height` and only obj_gui_menu's (and the never-created
  obj_stealth's) dead "scanning" branch ever set it "onscreen", so it never rose and
  obj_mouse_gui's pistol click was unreachable; the rebuild raises it when play starts and, as
  the persistent GM instance would, keeps it up in later rooms. obj_mouse_gui's click chain is
  `MouseGui.Resolve` — program icon, else console, else pistol icon — and a program icon or
  console click plays `sfx_flick`; a console click selects that console (`selected_id`), which in
  the rebuild's one-console HUD gives its input line the keyboard focus back. A click on the
  pistol icon counts as GUI, so it never also looks at or hacks an NPC behind it; firing was never
  GUI-gated in the original, so a click on it while aiming still fires, then holsters.

## Laws

See AGENTS.md for the non-negotiables (reconstruction is ground truth, data over hard-coding where
the original did, no unreviewable editor-only state, done = verified by play, one ticket per
commit).
