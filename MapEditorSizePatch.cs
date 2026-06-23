using System.Reflection;
using HarmonyLib;
using Mafi;
using Mafi.Unity.MapEditor;

namespace CustomMod
{
    [HarmonyPatch(typeof(MapEditor.MapSizeConfig), nameof(MapEditor.MapSizeConfig.ObjectWasEdited))]
    public static class MapEditorSizePatch
    {
        private const int MAX_DIM = 32768;
        private const long MAX_AREA = 67108864L;

        private static readonly FieldInfo OldSizeField = typeof(MapEditor.MapSizeConfig)
            .GetField("m_oldSize", BindingFlags.Instance | BindingFlags.NonPublic);

        static void Prefix(MapEditor.MapSizeConfig __instance, out RelTile2i __state)
        {
            __state = __instance.MapSize;
            Mafi.Log.Info($"CustomMod: MapSizePatch Prefix, MapSize={__state}");
        }

        static void Postfix(MapEditor.MapSizeConfig __instance, RelTile2i __state)
        {
            Mafi.Log.Info($"CustomMod: MapSizePatch Postfix, original input={__state}, after clamp={__instance.MapSize}");
            var current = __instance.MapSize;
            int x = __state.X.CeilToMultipleOf(256).Clamp(256, MAX_DIM);
            int y = __state.Y.CeilToMultipleOf(256).Clamp(256, MAX_DIM);

            if ((long)x * y > MAX_AREA)
            {
                var oldSize = (RelTile2i)OldSizeField.GetValue(__instance);
                if (x != oldSize.X)
                    x = (int)(MAX_AREA / y) / 256 * 256;
                else
                    y = (int)(MAX_AREA / x) / 256 * 256;
            }

            var newSize = new RelTile2i(x, y);
            if (newSize != current)
            {
                __instance.MapSize = newSize;
                OldSizeField.SetValue(__instance, newSize);
            }
        }
    }
}
