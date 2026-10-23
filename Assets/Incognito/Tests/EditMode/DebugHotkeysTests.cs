using Incognito.World;
using NUnit.Framework;
using UnityEngine;

namespace Incognito.Tests
{
    public class DebugHotkeysTests
    {
        [Test]
        public void The_F12_readout_lists_room_player_x_and_mouse()
        {
            Assert.AreEqual("room: Street00\nplayerX: 1234\nmouse: 10, 20",
                DebugHotkeys.FormatReadout("Street00", 1234.2f, new Vector2(10f, 20f)));
        }
    }
}
