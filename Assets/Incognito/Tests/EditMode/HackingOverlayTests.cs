using Incognito.CameraRig;
using Incognito.Eye;
using Incognito.Hacking;
using NUnit.Framework;

namespace Incognito.Tests
{
    public class HackingOverlayTests
    {
        private sealed class FakeNpc { }

        private const float Dt = 1f / 60f;

        // --- Console toggle (obj_walker key 81) ---

        [Test]
        public void Toggle_on_forces_pointer_and_fades_the_backdrop_in()
        {
            var session = new HackingSession<FakeNpc>();

            Assert.AreEqual(EyeCursorMode.Pointer, session.ToggleInverted(EyeCursorMode.Talk));
            Assert.IsTrue(session.Inverted);
            Assert.AreEqual(OverlayFade.Mode.FadeIn, session.Backdrop.Current);
        }

        [Test]
        public void Toggle_off_restores_the_mode_in_use_before_it_went_on()
        {
            var session = new HackingSession<FakeNpc>();
            session.ToggleInverted(EyeCursorMode.Talk);

            Assert.AreEqual(EyeCursorMode.Talk, session.ToggleInverted(EyeCursorMode.Pointer));
            Assert.IsFalse(session.Inverted);
            Assert.AreEqual(OverlayFade.Mode.FadeOut, session.Backdrop.Current);
        }

        // --- Pointer click on an NPC (obj_npc's pointer branch) ---

        [Test]
        public void Pointer_click_is_ignored_outside_the_console_view()
        {
            var session = new HackingSession<FakeNpc>();
            Assert.IsFalse(session.PointerClicked(new FakeNpc()));
            Assert.IsNull(session.Target);
            Assert.IsFalse(session.Program.Visible);
        }

        [Test]
        public void Pointer_click_hacks_the_npc_and_raises_the_active_program_icon()
        {
            var session = new HackingSession<FakeNpc>();
            var npc = new FakeNpc();
            session.ToggleInverted(EyeCursorMode.Look);

            Assert.IsTrue(session.PointerClicked(npc));
            Assert.AreSame(npc, session.Target);
            Assert.IsTrue(session.ProgramActive);
            Assert.AreEqual(ProgramIconSlide.Mode.Onscreen, session.Program.Current);
        }

        [Test]
        public void Pointer_click_on_another_npc_retargets_the_hack()
        {
            var session = new HackingSession<FakeNpc>();
            var first = new FakeNpc();
            var second = new FakeNpc();
            session.ToggleInverted(EyeCursorMode.Look);
            session.PointerClicked(first);

            Assert.IsTrue(session.PointerClicked(second));
            Assert.AreSame(second, session.Target);
        }

        [Test]
        public void Closing_the_console_ends_the_hack_but_leaves_the_icon_up()
        {
            var session = new HackingSession<FakeNpc>();
            session.ToggleInverted(EyeCursorMode.Look);
            session.PointerClicked(new FakeNpc());
            RunSteps(session, 60);

            session.ConsoleClosed();

            Assert.IsNull(session.Target);
            Assert.IsFalse(session.ProgramActive);
            Assert.IsTrue(session.Program.Visible);
        }

        [Test]
        public void Program_icon_closes_an_open_console_and_reopens_on_the_last_npc()
        {
            var session = new HackingSession<FakeNpc>();
            var npc = new FakeNpc();
            session.ToggleInverted(EyeCursorMode.Look);
            session.PointerClicked(npc);

            Assert.IsNull(session.ProgramClicked(consoleOpen: true));
            Assert.IsNull(session.Target);

            Assert.AreSame(npc, session.ProgramClicked(consoleOpen: false));
            Assert.AreSame(npc, session.Target);
        }

        [Test]
        public void Toggle_off_ends_the_hack_and_sinks_the_icon()
        {
            var session = new HackingSession<FakeNpc>();
            session.ToggleInverted(EyeCursorMode.Look);
            session.PointerClicked(new FakeNpc());
            RunSteps(session, 60);

            session.ToggleInverted(EyeCursorMode.Pointer);
            Assert.IsNull(session.Target);
            Assert.IsNull(session.ProgramClicked(consoleOpen: false));

            RunSteps(session, 60);
            Assert.IsFalse(session.Program.Visible);
            Assert.AreEqual(0f, session.Backdrop.Alpha);
            Assert.AreEqual(OverlayFade.Mode.Off, session.Backdrop.Current);
        }

        // --- obj_gui_console fade ---

        [Test]
        public void Backdrop_fades_in_at_0_025_per_step_to_full()
        {
            var fade = new OverlayFade();
            fade.FadeIn();
            fade.StepOnce();
            Assert.AreEqual(0.025f, fade.Alpha, 1e-5f);

            for (int i = 0; i < 100; i++) fade.StepOnce();
            Assert.AreEqual(1f, fade.Alpha);
        }

        [Test]
        public void Backdrop_fades_out_to_off()
        {
            var fade = new OverlayFade();
            fade.FadeIn();
            for (int i = 0; i < 50; i++) fade.StepOnce();
            fade.FadeOut();
            for (int i = 0; i < 20; i++) fade.StepOnce();
            Assert.AreEqual(OverlayFade.Mode.FadeOut, fade.Current);
            Assert.AreEqual(0.5f, fade.Alpha, 1e-3f);

            for (int i = 0; i < 25; i++) fade.StepOnce();
            Assert.AreEqual(OverlayFade.Mode.Off, fade.Current);
            Assert.AreEqual(0f, fade.Alpha);
        }

        // --- obj_program slide ---

        [Test]
        public void Program_icon_eases_up_from_below_the_screen()
        {
            var slide = new ProgramIconSlide(86f);
            Assert.AreEqual(1080f, slide.Y);
            Assert.IsFalse(slide.Visible);

            slide.SlideIn();
            slide.StepOnce();
            // y -= 1 + |y| * 0.01
            Assert.AreEqual(1080f - 1f - 10.8f, slide.Y, 1e-3f);

            for (int i = 0; i < 20; i++) slide.StepOnce();
            Assert.AreEqual(994f, slide.Y);
            Assert.AreEqual(ProgramIconSlide.Mode.Idle, slide.Current);
        }

        [Test]
        public void Program_icon_drops_10px_per_step_back_to_rest()
        {
            var slide = new ProgramIconSlide(86f);
            slide.SlideIn();
            for (int i = 0; i < 20; i++) slide.StepOnce();

            slide.SlideOut();
            slide.StepOnce();
            Assert.AreEqual(1004f, slide.Y, 1e-3f);

            for (int i = 0; i < 20; i++) slide.StepOnce();
            Assert.AreEqual(1080f, slide.Y);
            Assert.IsFalse(slide.Visible);
        }

        // --- obj_console CPU readout ---

        [Test]
        public void Cpu_readout_matches_obj_console()
        {
            Assert.AreEqual("CPU: 100/100%", CpuReadout.Format(100, 0));
            Assert.AreEqual("CPU: 100/100%", CpuReadout.Format(100, 1));
            Assert.AreEqual("CPU: 33/100%", CpuReadout.Format(100, 3));
        }

        // --- Camera handoff (view_object = cursor / NPC) ---

        private static CloseFramingCameraState NewCamera(float centerX)
        {
            var state = new CloseFramingCameraState { ViewWidth = 1920f, RoomMinX = 0f, RoomMaxX = 11520f };
            // Centre the view by snapping onto a player standing there near a room end-free spot.
            state.StepToward(1f, centerX, 0f, 0f, 0.5f);
            return state;
        }

        [Test]
        public void Cursor_follow_edge_scrolls_at_10px_per_step_within_32px_of_an_edge()
        {
            var cam = NewCamera(5000f);
            Assert.AreEqual(5000f, cam.CenterX, 1e-3f);

            // Mouse on the far right edge: 960px right of centre, 32px border, 10px/step.
            cam.StepToward(Dt, cam.CursorX(1f), 960f - 32f, 10f, 0.5f);
            Assert.AreEqual(5010f, cam.CenterX, 1e-3f);

            // Mouse in the middle of the screen: no scroll.
            cam.StepToward(Dt, cam.CursorX(0.5f), 960f - 32f, 10f, 0.5f);
            Assert.AreEqual(5010f, cam.CenterX, 1e-3f);

            // Just inside the left border: moves only as far as needed.
            float fiveInside = (32f - 5f) / 1920f; // 27px from the left edge
            cam.StepToward(Dt, cam.CursorX(fiveInside), 960f - 32f, 10f, 0.5f);
            Assert.AreEqual(5005f, cam.CenterX, 1e-3f);
        }

        [Test]
        public void Npc_follow_centres_the_view_on_it_at_10px_per_step()
        {
            var cam = NewCamera(5000f);
            cam.StepToward(Dt, 5100f, 0f, 10f, 0.5f);
            Assert.AreEqual(5010f, cam.CenterX, 1e-3f);

            for (int i = 0; i < 20; i++) cam.StepToward(Dt, 5100f, 0f, 10f, 0.5f);
            Assert.AreEqual(5100f, cam.CenterX, 1e-3f);
        }

        private static void RunSteps(HackingSession<FakeNpc> session, int steps)
        {
            for (int i = 0; i < steps; i++)
            {
                session.Backdrop.StepOnce();
                session.Program.StepOnce();
            }
        }
    }
}
