using Incognito.Terminal;
using Incognito.Weapon;

namespace Incognito.World
{
    /// <summary>
    /// Something that happened in the <see cref="GameWorld"/>, in order, for presentation to play
    /// (sounds, spawned bullets, the console's output, room changes). Events are the only thing the
    /// world sends out; presentation reads state and plays these and changes nothing.
    /// </summary>
    public abstract class GameEvent
    {
    }

    /// <summary>The walk bob peaked: a footstep.</summary>
    public sealed class PlayerFootstep : GameEvent
    {
        public PlayerFootstep(PlayerId player)
        {
            Player = player;
        }

        public PlayerId Player { get; }

        public override string ToString() => "PlayerFootstep " + Player;
    }

    /// <summary>The weapon made a sound (reload, holster, whoosh, shot).</summary>
    public sealed class WeaponSounded : GameEvent
    {
        public WeaponSounded(PlayerId player, PlayerWeaponState.WeaponSound sound)
        {
            Player = player;
            Sound = sound;
        }

        public PlayerId Player { get; }
        public PlayerWeaponState.WeaponSound Sound { get; }

        public override string ToString() => "WeaponSounded " + Player + " " + Sound;
    }

    /// <summary>The player fired: a bullet leaves the gun arm's pivot at <see cref="Spawn"/> (offset in original px, direction in degrees).</summary>
    public sealed class BulletFired : GameEvent
    {
        public BulletFired(PlayerId player, PlayerWeaponState.BulletSpawn spawn)
        {
            Player = player;
            Spawn = spawn;
        }

        public PlayerId Player { get; }
        public PlayerWeaponState.BulletSpawn Spawn { get; }

        public override string ToString() => "BulletFired " + Player;
    }

    /// <summary>An NPC was shoved by a bullet.</summary>
    public sealed class NpcShoved : GameEvent
    {
        public NpcShoved(NpcId npc, PlayerId shooter, float distance)
        {
            Npc = npc;
            Shooter = shooter;
            Distance = distance;
        }

        public NpcId Npc { get; }
        public PlayerId Shooter { get; }
        public float Distance { get; }

        public override string ToString() => "NpcShoved " + Npc + " " + Distance;
    }

    /// <summary>A talking NPC arrived and the conversation began.</summary>
    public sealed class ConversationStarted : GameEvent
    {
        public ConversationStarted(NpcId npc)
        {
            Npc = npc;
        }

        public NpcId Npc { get; }

        public override string ToString() => "ConversationStarted " + Npc;
    }

    /// <summary>The player was placed at a room's entrance.</summary>
    public sealed class PlayerPlaced : GameEvent
    {
        public PlayerPlaced(PlayerId player)
        {
            Player = player;
        }

        public PlayerId Player { get; }

        public override string ToString() => "PlayerPlaced " + Player;
    }

    /// <summary>The player used the door at <see cref="DoorX"/>: the room fades out to the next.</summary>
    public sealed class DoorUsed : GameEvent
    {
        public DoorUsed(PlayerId player, float doorX)
        {
            Player = player;
            DoorX = doorX;
        }

        public PlayerId Player { get; }
        public float DoorX { get; }

        public override string ToString() => "DoorUsed " + Player + " " + DoorX;
    }

    /// <summary>The player walked into a side exit's strip and is now on autowalk: the room fades out to the next.</summary>
    public sealed class SideExitReached : GameEvent
    {
        public SideExitReached(PlayerId player, int exit)
        {
            Player = player;
            Exit = exit;
        }

        public PlayerId Player { get; }

        /// <summary>The id <see cref="GameWorld.AddSideExit"/> returned.</summary>
        public int Exit { get; }

        public override string ToString() => "SideExitReached " + Player + " #" + Exit;
    }

    /// <summary>The typed console's new output for a player.</summary>
    public sealed class ConsoleOutput : GameEvent
    {
        public ConsoleOutput(PlayerId player, string text)
        {
            Player = player;
            Text = text;
        }

        public PlayerId Player { get; }
        public string Text { get; }

        public override string ToString() => "ConsoleOutput " + Player;
    }

    /// <summary><c>reboot</c> resolved a console object.</summary>
    public sealed class ConsoleObjectRebooted : GameEvent
    {
        public ConsoleObjectRebooted(PlayerId player, ConsoleObject target)
        {
            Player = player;
            Target = target;
        }

        public PlayerId Player { get; }
        public ConsoleObject Target { get; }

        public override string ToString() => "ConsoleObjectRebooted " + Target;
    }

    /// <summary><c>overwrite</c> resolved a console object.</summary>
    public sealed class ConsoleObjectOverwritten : GameEvent
    {
        public ConsoleObjectOverwritten(PlayerId player, ConsoleObject target)
        {
            Player = player;
            Target = target;
        }

        public PlayerId Player { get; }
        public ConsoleObject Target { get; }

        public override string ToString() => "ConsoleObjectOverwritten " + Target;
    }
}
