using UnityEditor;
using UnityEngine;
using System;
using System.Reflection;

public static class GameViewResolutionTool
{
    private static object gameViewSizesInstance;
    private static MethodInfo getGroupMethod;
    private static Type gameViewSizeType;
    private static Type gameViewSizeTypeEnum;

    private static readonly (int width, int height, string name)[] allResolutions = new[]
    {
        // 🟦 Mobile Phones
        (750, 1334, "📱 iPhone SE Portrait"),
        (1334, 750, "📱 iPhone SE Landscape"),
        (1170, 2532, "📱 iPhone 13 Portrait"),
        (2532, 1170, "📱 iPhone 13 Landscape"),
        (1080, 2400, "📱 Android FHD Portrait"),
        (2400, 1080, "📱 Android FHD Landscape"),
        (1440, 3120, "📱 Pixel 6 Pro Portrait"),
        (3120, 1440, "📱 Pixel 6 Pro Landscape"),

        // 🟪 Tablets
        (810, 1080, "📱 iPad Portrait"),
        (1080, 810, "📱 iPad Landscape"),
        (1600, 2560, "📱 Android Tablet Portrait"),
        (2560, 1600, "📱 Android Tablet Landscape"),
        (1768, 2208, "📱 Fold Inner Display"),

        // 🖥️ Desktop Monitors
        (1280, 720, "🖥️ 720p HD"),
        (1920, 1080, "🖥️ 1080p FHD"),
        (2560, 1440, "🖥️ 1440p QHD"),
        (3840, 2160, "🖥️ 4K UHD"),
        (3440, 1440, "🖥️ Ultra-Wide QHD"),
        (5120, 2160, "🖥️ 5K Ultra-Wide")
    };

    [MenuItem("Tools/Add All Game View Resolutions")]
    public static void AddAllResolutions()
    {
        InitializeReflection();

        var fixedResEnum = Enum.Parse(gameViewSizeTypeEnum, "FixedResolution");

        foreach (GameViewSizeGroupType groupType in Enum.GetValues(typeof(GameViewSizeGroupType)))
        {
            var group = getGroupMethod.Invoke(gameViewSizesInstance, new object[] { (int)groupType });
            if (group == null) continue;

            var addCustomSize = group.GetType().GetMethod("AddCustomSize", BindingFlags.Instance | BindingFlags.Public);
            var getDisplayTexts = group.GetType().GetMethod("GetDisplayTexts", BindingFlags.Instance | BindingFlags.Public);
            var existingNames = (string[])getDisplayTexts.Invoke(group, null);

            var ctor = gameViewSizeType.GetConstructor(new[] {
                gameViewSizeTypeEnum, typeof(int), typeof(int), typeof(string)
            });

            int addedCount = 0;
            foreach (var (w, h, name) in allResolutions)
            {
                bool exists = Array.Exists(existingNames, display => display.StartsWith(name));
                if (exists) continue;

                var newSize = ctor.Invoke(new object[] { fixedResEnum, w, h, name });
                addCustomSize.Invoke(group, new object[] { newSize });
                addedCount++;
            }

            if (addedCount > 0)
                Debug.Log($"✅ [{groupType}] Added {addedCount} resolutions.");
        }

        EditorUtility.DisplayDialog("Game View Resolutions", "All mobile, tablet, and desktop resolutions have been added.\nSwitch platforms (Standalone, Android, iOS) to view them.", "OK");
    }

    private static void InitializeReflection()
    {
        var sizesType = typeof(Editor).Assembly.GetType("UnityEditor.GameViewSizes");
        var singleton = typeof(ScriptableSingleton<>).MakeGenericType(sizesType);
        var instanceProp = singleton.GetProperty("instance", BindingFlags.Public | BindingFlags.Static);
        gameViewSizesInstance = instanceProp.GetValue(null, null);

        getGroupMethod = sizesType.GetMethod("GetGroup", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        gameViewSizeType = typeof(Editor).Assembly.GetType("UnityEditor.GameViewSize");
        gameViewSizeTypeEnum = typeof(Editor).Assembly.GetType("UnityEditor.GameViewSizeType");
    }
}
