using System;
using Unity.Shared.Core.Json;

namespace Incognito.Content
{
    /// <summary>
    /// Small array-shaped helpers over Unity.Shared.Core's <see cref="JsonObject"/>/<see cref="JsonArray"/>
    /// (U.INC-T25), used by the content catalog and settings registry's <c>FromJson</c> methods: a
    /// missing or non-array field reads as an empty array, matching how a missing field already
    /// read as a type's default under UnityEngine.JsonUtility.
    /// </summary>
    internal static class JsonArrayHelpers
    {
        public static string[] GetStringArray(this JsonObject json, string key)
        {
            JsonArray array = json?.GetArray(key);
            if (array == null) return Array.Empty<string>();
            var result = new string[array.Count];
            for (int i = 0; i < array.Count; i++)
                result[i] = array[i] as string ?? string.Empty;
            return result;
        }

        public static float[] GetFloatArray(this JsonObject json, string key)
        {
            JsonArray array = json?.GetArray(key);
            if (array == null) return Array.Empty<float>();
            var result = new float[array.Count];
            for (int i = 0; i < array.Count; i++)
                result[i] = array[i] is JsonNumber n ? (float)n.Value : 0f;
            return result;
        }

        public static T[] GetObjectArray<T>(this JsonObject json, string key, Func<JsonObject, T> fromJson)
        {
            JsonArray array = json?.GetArray(key);
            if (array == null) return Array.Empty<T>();
            var result = new T[array.Count];
            for (int i = 0; i < array.Count; i++)
                result[i] = fromJson(array[i] as JsonObject);
            return result;
        }
    }
}
