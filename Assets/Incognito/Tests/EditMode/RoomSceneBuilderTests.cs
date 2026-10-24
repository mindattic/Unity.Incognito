using System.IO;
using System.Text.RegularExpressions;
using Incognito.EditorTools;
using NUnit.Framework;
using UnityEditor;

namespace Incognito.Tests
{
    /// <summary>
    /// U.INC-T26: Incognito/Build Room Scenes must be deterministic — regenerating the four
    /// committed room scenes from the current game-content.json and original art must reproduce
    /// them. Room content itself (background, props, exits) is no longer baked into these files at
    /// all (<see cref="Incognito.World.RoomBootstrap"/> rebuilds it from data at Awake), so what's
    /// left to regenerate — the sandbox rig copy plus one bootstrap component's art/audio
    /// references — can never silently drift from what the builder produces today.
    ///
    /// Compared with <see cref="Normalize"/>'d text rather than raw bytes: Unity assigns every
    /// freshly-created object a new <c>fileID</c> each time a scene with no prior file identity is
    /// written (<c>NewScene</c>'s objects, and this builder's own new "Room Bootstrap" object), so
    /// that number is session noise, not content — a <c>fileID: 0</c> null reference is kept as-is,
    /// since that distinction (nothing wired vs. something wired) is real content.
    /// </summary>
    public class RoomSceneBuilderTests
    {
        private static readonly string[] ScenePaths =
        {
            "Assets/Incognito/Scenes/Rooms/Titlescreen.unity",
            "Assets/Incognito/Scenes/Rooms/Street00.unity",
            "Assets/Incognito/Scenes/Rooms/Street01.unity",
            "Assets/Incognito/Scenes/Rooms/Shop.unity",
        };

        [Test]
        public void Regenerating_the_room_scenes_reproduces_the_committed_files()
        {
            var before = new string[ScenePaths.Length];
            for (int i = 0; i < ScenePaths.Length; i++) before[i] = File.ReadAllText(ScenePaths[i]);

            try
            {
                RoomSceneBuilder.BuildAll();

                for (int i = 0; i < ScenePaths.Length; i++)
                {
                    string after = File.ReadAllText(ScenePaths[i]);
                    Assert.AreEqual(Normalize(before[i]), Normalize(after),
                        $"{ScenePaths[i]} differs from what Incognito/Build Room Scenes generates today " +
                        "from game-content.json — regenerate and commit it.");
                }
            }
            finally
            {
                // Leave the working tree exactly as it was, whether or not the assertion held.
                for (int i = 0; i < ScenePaths.Length; i++) File.WriteAllText(ScenePaths[i], before[i]);
                AssetDatabase.Refresh();
            }
        }

        /// <summary>Collapses every non-zero <c>fileID</c> — object header anchors and references
        /// alike — to one token, so two scenes built in different Editor sessions compare equal
        /// whenever their actual content (component types, field values, asset GUIDs, hierarchy
        /// names) agrees. Unity also writes each scene's objects ordered by that same (session-random)
        /// fileID rather than by creation order, so the per-object blocks are then sorted into a
        /// stable order before comparing — only their content, never their position in the file,
        /// is what this test cares about.</summary>
        private static string Normalize(string unityYaml)
        {
            string text = Regex.Replace(unityYaml, @"fileID: -?[1-9]\d*", "fileID: SET");
            text = Regex.Replace(text, @"^(--- !u!\d+) &-?\d+", "$1 &ID", RegexOptions.Multiline);

            string[] blocks = Regex.Split(text, @"(?=^--- !u!)", RegexOptions.Multiline);
            System.Array.Sort(blocks, System.StringComparer.Ordinal);
            return string.Join("", blocks);
        }
    }
}
