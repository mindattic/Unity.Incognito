// NUnit 4 moved the classic Assert; the tests were written against it (Unity Test Framework).
using Assert = NUnit.Framework.Legacy.ClassicAssert;
using CollectionAssert = NUnit.Framework.Legacy.CollectionAssert;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Incognito.Content;
using Incognito.Npc;
using Incognito.Settings;
using NUnit.Framework;
using Unity.Shared.Core.Json;

namespace Incognito.Tests
{
    /// <summary>
    /// U.INC-T25 review: hand-written <c>FromJson</c> methods list their fields by hand, unlike
    /// UnityEngine.JsonUtility's automatic field mapping, so a field one of them forgets (today or
    /// in a later edit) would silently keep its default instead of failing loudly. This walks every
    /// public field of every type with a <c>FromJson</c> by reflection, sets each one to a value
    /// that cannot be mistaken for its default (or for another field's sentinel, so a field-swap
    /// bug is caught too), parses, and asserts every field actually changed.
    /// </summary>
    public static class FieldCoverage
    {
        private const float FloatSentinelBase = 900000f;
        private const int IntSentinelBase = 900000;

        /// <summary>
        /// Builds JSON covering every public instance field of <paramref name="type"/> (recursing
        /// into a field whose type, or array element type, has its own <c>static X FromJson(JsonObject)</c>),
        /// and hands back a check that a parsed instance has every one of those fields set to the
        /// sentinel this put in the JSON.
        /// </summary>
        public static JsonObject Build(Type type, out Action<object> verify)
        {
            var json = new JsonObject();
            object baseline = Activator.CreateInstance(type);
            var checks = new List<Action<object>>();

            foreach (FieldInfo field in type.GetFields(BindingFlags.Public | BindingFlags.Instance))
                BuildField(field, baseline, json, checks);

            verify = instance => { foreach (Action<object> check in checks) check(instance); };
            return json;
        }

        private static void BuildField(FieldInfo field, object baseline, JsonObject json, List<Action<object>> checks)
        {
            Type ft = field.FieldType;
            string label = field.DeclaringType.Name + "." + field.Name;
            // A per-field offset (not a shared constant) so two fields of the same scalar type
            // can't pass by accidentally reading each other's JSON key.
            int offset = field.Name.GetHashCode() & 0xFFFF;

            if (ft == typeof(float))
            {
                float value = FloatSentinelBase + offset;
                json.Set(field.Name, value);
                checks.Add(instance => Assert.AreEqual(value, (float)field.GetValue(instance), label));
            }
            else if (ft == typeof(int))
            {
                int value = IntSentinelBase + offset;
                json.Set(field.Name, value);
                checks.Add(instance => Assert.AreEqual(value, (int)field.GetValue(instance), label));
            }
            else if (ft == typeof(bool))
            {
                bool value = !(bool)field.GetValue(baseline);
                json.Set(field.Name, value);
                checks.Add(instance => Assert.AreEqual(value, (bool)field.GetValue(instance), label));
            }
            else if (ft == typeof(string))
            {
                string value = "sentinel-" + field.Name;
                json.Set(field.Name, value);
                checks.Add(instance => Assert.AreEqual(value, (string)field.GetValue(instance), label));
            }
            else if (ft.IsEnum)
            {
                object baselineValue = field.GetValue(baseline);
                object value = Enum.GetValues(ft).Cast<object>().First(v => !v.Equals(baselineValue));
                json.Set(field.Name, Convert.ToInt32(value));
                checks.Add(instance => Assert.AreEqual(value, field.GetValue(instance), label));
            }
            else if (ft == typeof(string[]))
            {
                var value = new[] { "sentinel-" + field.Name };
                json.Set(field.Name, new JsonArray(value.Cast<object>()));
                checks.Add(instance => CollectionAssert.AreEqual(value, (string[])field.GetValue(instance), label));
            }
            else if (ft == typeof(float[]))
            {
                var value = new[] { FloatSentinelBase + offset };
                json.Set(field.Name, new JsonArray(value.Cast<object>()));
                checks.Add(instance => CollectionAssert.AreEqual(value, (float[])field.GetValue(instance), label));
            }
            else if (ft.IsArray && HasFromJson(ft.GetElementType()))
            {
                JsonObject elementJson = Build(ft.GetElementType(), out Action<object> verifyElement);
                json.Set(field.Name, new JsonArray(new object[] { elementJson }));
                checks.Add(instance =>
                {
                    Array array = (Array)field.GetValue(instance);
                    Assert.AreEqual(1, array.Length, label + " (array length)");
                    verifyElement(array.GetValue(0));
                });
            }
            else if (HasFromJson(ft))
            {
                JsonObject nestedJson = Build(ft, out Action<object> verifyNested);
                json.Set(field.Name, nestedJson);
                checks.Add(instance => verifyNested(field.GetValue(instance)));
            }
            else
            {
                throw new NotSupportedException("FieldCoverage has no sentinel strategy for " + label + " (" + ft + ").");
            }
        }

        private static bool HasFromJson(Type type) =>
            type.GetMethod("FromJson", BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(JsonObject) }, null) != null;
    }

    public class FieldCoverageTests
    {
        [Test]
        public void SuspicionTuning_every_field_round_trips()
        {
            JsonObject json = FieldCoverage.Build(typeof(SuspicionTuning), out Action<object> verify);
            verify(SuspicionTuning.FromJson(json));
        }

        [Test]
        public void LocomotionTuning_every_field_round_trips()
        {
            JsonObject json = FieldCoverage.Build(typeof(LocomotionTuning), out Action<object> verify);
            verify(LocomotionTuning.FromJson(json));
        }

        [Test]
        public void CameraTuning_every_field_round_trips()
        {
            JsonObject json = FieldCoverage.Build(typeof(CameraTuning), out Action<object> verify);
            verify(CameraTuning.FromJson(json));
        }

        [Test]
        public void WeaponTuning_every_field_round_trips()
        {
            JsonObject json = FieldCoverage.Build(typeof(WeaponTuning), out Action<object> verify);
            verify(WeaponTuning.FromJson(json));
        }

        [Test]
        public void RainTuning_every_field_round_trips()
        {
            JsonObject json = FieldCoverage.Build(typeof(RainTuning), out Action<object> verify);
            verify(RainTuning.FromJson(json));
        }

        [Test]
        public void SettingsRegistry_every_field_round_trips()
        {
            JsonObject json = FieldCoverage.Build(typeof(SettingsRegistry), out Action<object> verify);
            verify(SettingsRegistry.FromJson(json.ToJson()));
        }

        [Test]
        public void PropSpot_every_field_round_trips()
        {
            JsonObject json = FieldCoverage.Build(typeof(PropSpot), out Action<object> verify);
            verify(PropSpot.FromJson(json));
        }

        [Test]
        public void SideExitSpec_every_field_round_trips()
        {
            JsonObject json = FieldCoverage.Build(typeof(SideExitSpec), out Action<object> verify);
            verify(SideExitSpec.FromJson(json));
        }

        [Test]
        public void DoorSpec_every_field_round_trips()
        {
            JsonObject json = FieldCoverage.Build(typeof(DoorSpec), out Action<object> verify);
            verify(DoorSpec.FromJson(json));
        }

        [Test]
        public void RoomDefinition_every_field_round_trips()
        {
            JsonObject json = FieldCoverage.Build(typeof(RoomDefinition), out Action<object> verify);
            verify(RoomDefinition.FromJson(json));
        }

        [Test]
        public void NpcDefinition_every_field_round_trips()
        {
            JsonObject json = FieldCoverage.Build(typeof(NpcDefinition), out Action<object> verify);
            verify(NpcDefinition.FromJson(json));
        }

        [Test]
        public void ConsoleText_every_field_round_trips()
        {
            JsonObject json = FieldCoverage.Build(typeof(ConsoleText), out Action<object> verify);
            verify(ConsoleText.FromJson(json));
        }

        [Test]
        public void SoundDefinition_every_field_round_trips()
        {
            JsonObject json = FieldCoverage.Build(typeof(SoundDefinition), out Action<object> verify);
            verify(SoundDefinition.FromJson(json));
        }

        [Test]
        public void ArtDefinition_every_field_round_trips()
        {
            JsonObject json = FieldCoverage.Build(typeof(ArtDefinition), out Action<object> verify);
            verify(ArtDefinition.FromJson(json));
        }

        [Test]
        public void TitleScreenSpec_every_field_round_trips()
        {
            JsonObject json = FieldCoverage.Build(typeof(TitleScreenSpec), out Action<object> verify);
            verify(TitleScreenSpec.FromJson(json));
        }

        [Test]
        public void GameContent_every_field_round_trips()
        {
            JsonObject json = FieldCoverage.Build(typeof(GameContent), out Action<object> verify);
            verify(GameContent.FromJson(json.ToJson()));
        }
    }
}
