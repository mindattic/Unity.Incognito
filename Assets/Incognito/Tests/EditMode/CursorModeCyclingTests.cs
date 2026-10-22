using Incognito.Eye;
using Incognito.Npc;
using Incognito.Player;
using Incognito.Typing;
using NUnit.Framework;

namespace Incognito.Tests
{
    public class CursorModeCyclingTests
    {
        [Test]
        public void Cycle_toggles_look_and_talk_and_leaves_pointer_alone()
        {
            Assert.AreEqual(EyeCursorMode.Talk, EyeCursorModes.Cycle(EyeCursorMode.Look));
            Assert.AreEqual(EyeCursorMode.Look, EyeCursorModes.Cycle(EyeCursorMode.Talk));
            Assert.AreEqual(EyeCursorMode.Pointer, EyeCursorModes.Cycle(EyeCursorMode.Pointer));
        }

        [Test]
        public void Wheel_either_way_cycles()
        {
            Assert.IsTrue(EyeCursorModes.ShouldCycle(1f, false, false));
            Assert.IsTrue(EyeCursorModes.ShouldCycle(-1f, false, false));
            Assert.IsTrue(EyeCursorModes.ShouldCycle(1f, false, true));
            Assert.IsFalse(EyeCursorModes.ShouldCycle(0f, false, false));
        }

        [Test]
        public void Right_click_cycles_only_while_the_gun_is_holstered()
        {
            Assert.IsTrue(EyeCursorModes.ShouldCycle(0f, true, weaponDrawn: false));
            Assert.IsFalse(EyeCursorModes.ShouldCycle(0f, true, weaponDrawn: true));
        }

        [Test]
        public void Npc_click_action_follows_the_cursor_mode()
        {
            Assert.AreEqual(NpcClickAction.Describe, NpcClick.ActionFor(EyeCursorMode.Look, false));
            Assert.AreEqual(NpcClickAction.Talk, NpcClick.ActionFor(EyeCursorMode.Talk, false));
            Assert.AreEqual(NpcClickAction.Pointer, NpcClick.ActionFor(EyeCursorMode.Pointer, false));
        }

        [Test]
        public void Npc_click_does_nothing_while_aiming()
        {
            Assert.AreEqual(NpcClickAction.None, NpcClick.ActionFor(EyeCursorMode.Look, true));
            Assert.AreEqual(NpcClickAction.None, NpcClick.ActionFor(EyeCursorMode.Talk, true));
        }

        [Test]
        public void Player_turns_to_face_only_while_idle()
        {
            var state = new PlayerLocomotionState();
            Assert.IsTrue(state.TurnToFace(PlayerLocomotionState.FacingDirection.Right));
            Assert.AreEqual(PlayerLocomotionState.FacingDirection.Right, state.Facing);

            state.Step(1f / 60f, moveLeft: true, moveRight: false, sprint: false);
            Assert.IsFalse(state.TurnToFace(PlayerLocomotionState.FacingDirection.Right));
            Assert.AreEqual(PlayerLocomotionState.FacingDirection.Left, state.Facing);
        }
    }

    public class NpcTalkTests
    {
        private const float Dt = 1f / 60f;

        private static NpcPatrolState Npc(float x, params float[] route)
        {
            var state = new NpcPatrolState();
            state.SetInitialPositionX(x);
            state.SetRoute(route);
            return state;
        }

        private static void Run(NpcPatrolState state, int steps)
        {
            for (int i = 0; i < steps; i++) state.Step(Dt);
        }

        [Test]
        public void Talk_click_walks_the_npc_to_stand_one_body_width_from_the_player_on_its_side()
        {
            var left = Npc(1000f, 1000f);
            left.StartTalking(playerX: 3000f, standOff: 512f);
            Assert.AreEqual(NpcPatrolState.PatrolMode.Talking, left.CurrentMode);
            Assert.AreEqual(2488f, left.TalkTarget.Value, 0.0001f);

            var right = Npc(5000f, 5000f);
            right.StartTalking(playerX: 3000f, standOff: 512f);
            Assert.AreEqual(3512f, right.TalkTarget.Value, 0.0001f);

            left.Step(Dt);
            Assert.AreEqual(1005f, left.PositionX, 0.0001f); // walk_speed 5 per step
            Assert.AreEqual(PlayerLocomotionState.FacingDirection.Right, left.Facing);
        }

        [Test]
        public void Arriving_talker_stops_faces_the_player_and_turns_the_player_to_face_it()
        {
            var state = Npc(2000f, 9000f);
            PlayerLocomotionState.FacingDirection? playerTurn = null;
            int raised = 0;
            state.ConversationStarted += f => { playerTurn = f; raised++; };

            state.StartTalking(playerX: 3000f, standOff: 512f);
            Run(state, 400);

            Assert.IsTrue(state.InConversation);
            Assert.AreEqual(0f, state.HorizontalSpeed);
            Assert.AreEqual(NpcPatrolState.PatrolMode.Talking, state.CurrentMode);
            Assert.AreEqual(2488f, state.PositionX, 20f + 50f); // arrive window + friction coast
            Assert.Less(state.PositionX, 3000f);
            Assert.AreEqual(PlayerLocomotionState.FacingDirection.Right, state.Facing);
            Assert.AreEqual(PlayerLocomotionState.FacingDirection.Left, playerTurn);
            Assert.AreEqual(1, raised);
        }

        [Test]
        public void Talker_waits_while_the_player_stands_still()
        {
            var state = Npc(2000f, 9000f);
            state.StartTalking(3000f, 512f);
            Run(state, 2000);
            Assert.AreEqual(NpcPatrolState.PatrolMode.Talking, state.CurrentMode);
        }

        [Test]
        public void Talker_resumes_its_route_200_steps_after_the_player_last_walked()
        {
            var state = Npc(2000f, 9000f);
            state.StartTalking(3000f, 512f);
            Run(state, 400);

            for (int i = 0; i < 30; i++) { state.PlayerWalked(); state.Step(Dt); }
            Run(state, 195);
            Assert.AreEqual(NpcPatrolState.PatrolMode.Talking, state.CurrentMode);

            Run(state, 10);
            Assert.AreEqual(NpcPatrolState.PatrolMode.Walking, state.CurrentMode);
            Assert.IsFalse(state.InConversation);
            Assert.AreEqual(9000f, state.CurrentWaypoint.Value);
            Assert.AreEqual(PlayerLocomotionState.FacingDirection.Right, state.Facing);
        }

        [Test]
        public void Player_walking_does_not_affect_an_npc_that_is_not_talking()
        {
            var state = Npc(2000f, 9000f);
            Run(state, 5);
            state.PlayerWalked();
            Run(state, 300);
            Assert.AreEqual(NpcPatrolState.PatrolMode.Walking, state.CurrentMode);
        }
    }

    public class TypewriterStateTests
    {
        private const float Dt = 1f / 60f;

        [Test]
        public void Types_one_character_every_two_steps()
        {
            var state = new TypewriterState("Hey");
            state.Step(Dt);
            Assert.AreEqual("", state.Shown);
            state.Step(Dt);
            Assert.AreEqual("H", state.Shown);
            state.Step(Dt); state.Step(Dt);
            Assert.AreEqual("He", state.Shown);
            Assert.IsFalse(state.FadingOut);
            Assert.AreEqual(1f, state.Alpha);
        }

        [Test]
        public void Fades_by_a_hundredth_per_tick_once_typed_then_is_done()
        {
            var state = new TypewriterState("Hey");
            for (int i = 0; i < 6; i++) state.Step(Dt);
            Assert.AreEqual("Hey", state.Shown);
            Assert.IsTrue(state.FadingOut);
            Assert.AreEqual(0.99f, state.Alpha, 0.0001f);

            for (int i = 0; i < 2 * 98; i++) state.Step(Dt);
            Assert.IsFalse(state.Done);
            for (int i = 0; i < 2 * 3; i++) state.Step(Dt);
            Assert.IsTrue(state.Done);
            Assert.AreEqual(0f, state.Alpha);
        }

        [Test]
        public void Large_frame_still_types_in_whole_ticks()
        {
            var state = new TypewriterState("abcdef");
            state.Step(6f / 60f); // three ticks in one frame
            Assert.AreEqual("abc", state.Shown);
        }
    }
}
