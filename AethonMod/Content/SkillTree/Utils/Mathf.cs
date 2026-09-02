using System;

namespace AethonMod.Content.SkillTree.Utils
{
    public static class Mathf
    {
        public static float Clamp(float value, float min, float max) => value < min ? min : value > max ? max : value;
        public static float Lerp(float a, float b, float t) => a + (b - a) * t;
        public static int CeilInt(float value) => (int)Math.Ceiling(value);
        public static float Pow(float x, float y) => (float)Math.Pow(x, y);
        public static float Floor(float value) => (float)Math.Floor(value);
        public static float Round(float value) => (float)Math.Round(value);
        public static int FloorToInt(float value) => (int)Math.Floor(value);
    }
}

namespace AethonMod.Content.SkillTree.Utils
{
    public class AdditionalInfo
    {
        public static string GetStatName(object stat) { return stat?.ToString() ?? ""; }
    }
}

public static class StringExtensions
{
    public static float SafeFloatParse(this string s)
    {
        if (float.TryParse(s, out float result)) return result;
        return 0f;
    }
}
