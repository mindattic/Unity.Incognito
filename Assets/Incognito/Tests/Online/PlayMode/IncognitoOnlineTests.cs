// Incognito's online play (U.INC-T43) over a real connection, on Unity.Multiplayer's test kit: every test gets a
// real host or dedicated server and real clients over Unity Transport on a free loopback port.
//   Unity -batchmode -projectPath . -runTests -testPlatform PlayMode -assemblyNames Incognito.Tests.Online
using System.Collections;
using System.Linq;
using Incognito.Online;
using Incognito.Settings;
using Incognito.World;
using NUnit.Framework;
using Unity.Multiplayer;
using Unity.Multiplayer.Core;
using Unity.Multiplayer.Testing;
using UnityEngine;
using UnityEngine.TestTools;

namespace Incognito.Tests.Online
{
    internal static class IncognitoPrefabs
    {
        public static GameObject Player
        {
            get
            {
                var prefab = Resources.Load<GameObject>(IncognitoNetPlayer.PrefabResource);
                Assert.That(prefab, Is.Not.Null, "Resources/" + IncognitoNetPlayer.PrefabResource + ".prefab is missing; run Incognito > Build Online Scene");
                return prefab;
            }
        }
    }

    /// <summary>
    /// A host (which is also a player) and one client: each player carries its seat, the client's input goes
    /// up as [ToServer] calls and runs as Core commands on the host's one world, and every peer draws the
    /// state that comes back.
    /// </summary>
    public class IncognitoNetPlayerTests : NetSessionTest
    {
        public IncognitoNetPlayerTests() : base(Topology.Host, clients: 1) { }

        protected override void OnSessionsCreated()
        {
            SetPlayerPrefab(IncognitoPrefabs.Player);
            Client.Identity = new PlayerIdentity("ann-token", "Ann", new PlayerColor(230, 80, 60));
        }

        [UnityTest]
        public IEnumerator Each_player_is_seated_and_only_the_server_holds_the_world()
        {
            yield return StartAndJoinAll();
            var hostOnHost = WaitForSpawn<IncognitoNetPlayer>(Server, owner: Server.LocalClientId);
            yield return hostOnHost;
            var annOnAnn = WaitForSpawn<IncognitoNetPlayer>(Client, owner: Client.LocalClientId);
            yield return annOnAnn;

            yield return WaitForSynced(() => hostOnHost.Result.Seat, 0, "the host's seat");
            yield return WaitForSynced(() => annOnAnn.Result.Seat, 1, "Ann's seat, on her own client");
            Assert.That(hostOnHost.Result.PlayerId, Is.EqualTo(new PlayerId(1)), "the host plays as local play's P1");
            Assert.That(annOnAnn.Result.PlayerId, Is.EqualTo(new PlayerId(2)));

            var authority = IncognitoAuthority.Find(Server);
            Assert.That(authority, Is.Not.Null, "the server holds the authority");
            Assert.That(IncognitoAuthority.Find(Client), Is.Null, "a client never does");
            Assert.That(authority.World.Players.Select(p => p.Id), Is.EquivalentTo(new[] { new PlayerId(1), new PlayerId(2) }));

            // Each seat starts where OnlineTuning puts it, and the client sees the same.
            var online = GameSettings.Tuning.Online;
            yield return WaitUntil(() => Mathf.Approximately(annOnAnn.Result.X, online.StartFor(authority.Room, 1)),
                () => "Ann's client shows her at " + annOnAnn.Result.X);
            Assert.That(hostOnHost.Result.X, Is.EqualTo(online.StartFor(authority.Room, 0)));

            yield return WaitUntil(() => annOnAnn.Result.DisplayName == "Ann", () => "Ann's client shows her as " + annOnAnn.Result.DisplayName);
            Assert.That(annOnAnn.Result.Color, Is.EqualTo(new Color32(230, 80, 60, 255)));
        }

        [UnityTest]
        public IEnumerator A_clients_input_walks_and_arms_its_own_player_on_the_server_and_no_one_elses()
        {
            yield return StartAndJoinAll();
            var annOnAnn = WaitForSpawn<IncognitoNetPlayer>(Client, owner: Client.LocalClientId);
            yield return annOnAnn;
            var hostOnHost = WaitForSpawn<IncognitoNetPlayer>(Server, owner: Server.LocalClientId);
            yield return hostOnHost;
            yield return WaitForSynced(() => annOnAnn.Result.Seat, 1, "Ann's seat");

            float annStart = annOnAnn.Result.X;
            float hostStart = hostOnHost.Result.X;
            annOnAnn.Result.SendControls(OnlineControls.MoveLeftBit, 0f, 1920f); // her A key
            yield return WaitUntil(() => annOnAnn.Result.X < annStart - 50f,
                () => "Ann's own client never saw her walk; she is at " + annOnAnn.Result.X);
            annOnAnn.Result.SendControls(0, 0f, 1920f);
            Assert.That(hostOnHost.Result.X, Is.EqualTo(hostStart), "the host did not move");

            annOnAnn.Result.ToggleGunKey(); // her Z key
            yield return WaitUntil(() => annOnAnn.Result.GunOut, "Ann's client never saw her draw");
            var world = IncognitoAuthority.Find(Server).World;
            Assert.That(world.FindPlayer(new PlayerId(2)).Weapon.Mode, Is.Not.EqualTo(Incognito.Weapon.PlayerWeaponState.AimMode.Off),
                "the draw ran on the server's world");
            Assert.That(world.FindPlayer(new PlayerId(1)).Weapon.Mode, Is.EqualTo(Incognito.Weapon.PlayerWeaponState.AimMode.Off));
        }

        [UnityTest]
        public IEnumerator The_server_makes_a_cheating_clients_numbers_safe()
        {
            yield return StartAndJoinAll();
            var annOnAnn = WaitForSpawn<IncognitoNetPlayer>(Client, owner: Client.LocalClientId);
            yield return annOnAnn;
            yield return WaitForSynced(() => annOnAnn.Result.Seat, 1, "Ann's seat");

            annOnAnn.Result.SendControls(OnlineControls.LookingDownBit, float.NaN, -1f);
            var world = IncognitoAuthority.Find(Server).World;
            yield return WaitUntil(() => world.FindPlayer(new PlayerId(2)).Controls.LookingDown, "the controls never arrived");
            PlayerControls got = world.FindPlayer(new PlayerId(2)).Controls;
            Assert.That(got.MouseBelowCentre, Is.EqualTo(0f));
            Assert.That(got.ViewWidth, Is.EqualTo(1920f));
        }
    }

    /// <summary>The nightly smoke without Docker: a dedicated server and two bots (IncognitoBot) join by code, walk and draw.</summary>
    public class IncognitoBotTests : NetBotTest
    {
        public IncognitoBotTests() : base(bots: 2) { }

        protected override float BotTimeoutSeconds => 60f;

        protected override void OnSessionsCreated() => SetPlayerPrefab(IncognitoPrefabs.Player);

        [UnityTest, Timeout(120000)]
        public IEnumerator Two_bots_join_by_code_each_in_its_own_seat_and_play()
        {
            yield return RunBots(role => new IncognitoBot(), "A", "B");

            foreach (NetBotRunner bot in Bots)
                Assert.That(bot.PassMessage, Does.Contain("drew the gun"), bot.Role);

            var world = IncognitoAuthority.Find(Server).World;
            Assert.That(world.Players.Count, Is.EqualTo(2), "a dedicated server seats only the bots");
            Assert.That(world.Players.All(p => p.Weapon.Mode != Incognito.Weapon.PlayerWeaponState.AimMode.Off), "the server drew both guns");
        }
    }
}
