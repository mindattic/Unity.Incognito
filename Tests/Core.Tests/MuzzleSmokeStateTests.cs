// NUnit 4 moved the classic Assert; the tests were written against it (Unity Test Framework).
using Assert = NUnit.Framework.Legacy.ClassicAssert;
using Incognito.Settings;
using Incognito.Weapon;
using NUnit.Framework;

namespace Incognito.Tests
{
    public class MuzzleSmokeStateTests
    {
        [Test]
        public void Spawns_one_puff_per_tuned_particle_count()
        {
            var tuning = new WeaponTuning { MuzzleSmokeParticles = 4 };
            var state = new MuzzleSmokeState(tuning: tuning);

            Assert.AreEqual(4, state.Puffs.Count);
            Assert.IsFalse(state.IsDone);
        }

        [Test]
        public void Zero_particles_is_immediately_done()
        {
            var state = new MuzzleSmokeState(tuning: new WeaponTuning { MuzzleSmokeParticles = 0 });
            Assert.IsTrue(state.IsDone);
        }

        [Test]
        public void Each_puff_starts_at_the_muzzle_with_no_age()
        {
            var state = new MuzzleSmokeState(tuning: new WeaponTuning { MuzzleSmokeParticles = 1 });
            MuzzleSmokeState.Puff puff = state.Puffs[0];

            Assert.AreEqual(0f, puff.X);
            Assert.AreEqual(0f, puff.Y);
            Assert.AreEqual(0, puff.Age);
            Assert.AreEqual(1f, state.Alpha(puff), "just spawned: fully opaque");
        }

        [Test]
        public void Step_drifts_and_rises_a_puff_by_its_own_roll()
        {
            var tuning = new WeaponTuning { MuzzleSmokeParticles = 1 };
            var state = new MuzzleSmokeState((min, max) => min, tuning); // pin every roll to its range minimum

            state.Step(3);

            MuzzleSmokeState.Puff puff = state.Puffs[0];
            Assert.AreEqual(tuning.MuzzleSmokeDriftMin * 3f, puff.X, 1e-4f);
            Assert.AreEqual(tuning.MuzzleSmokeRiseMin * 3f, puff.Y, 1e-4f);
            Assert.AreEqual(3, puff.Age);
        }

        [Test]
        public void Alpha_fades_linearly_to_zero_over_its_lifetime()
        {
            var tuning = new WeaponTuning { MuzzleSmokeParticles = 1, MuzzleSmokeLifeSteps = 10 };
            var state = new MuzzleSmokeState(tuning: tuning);

            state.Step(5);
            Assert.AreEqual(0.5f, state.Alpha(state.Puffs[0]), 1e-4f);
        }

        [Test]
        public void A_puff_is_removed_once_it_reaches_its_lifetime()
        {
            var tuning = new WeaponTuning { MuzzleSmokeParticles = 1, MuzzleSmokeLifeSteps = 5 };
            var state = new MuzzleSmokeState(tuning: tuning);

            state.Step(4);
            Assert.AreEqual(1, state.Puffs.Count);
            Assert.IsFalse(state.IsDone);

            state.Step(1);
            Assert.AreEqual(0, state.Puffs.Count);
            Assert.IsTrue(state.IsDone, "every puff has faded out");
        }

        [Test]
        public void Multiple_puffs_all_expire_together_since_they_share_one_lifetime()
        {
            var tuning = new WeaponTuning { MuzzleSmokeParticles = 3, MuzzleSmokeLifeSteps = 6 };
            var state = new MuzzleSmokeState(tuning: tuning);

            state.Step(6);
            Assert.IsTrue(state.IsDone);
        }
    }
}
