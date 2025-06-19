using System;
using System.Collections.Generic;
using System.Reflection;
using RimWorld;
using UnityEngine;
using Verse;

namespace BPCSynchronizer.Patches
{
    public static class UINotIncludedPolicyLabelPatch
    {
        private static readonly Dictionary<string, string> LabelCache = new Dictionary<string, string>();
        private static readonly Dictionary<string, string> ManagerCache = new Dictionary<string, string>();

        public static void Postfix(Rect rect, object __instance)
        {
            try
            {
                if (!BPCSyncMod.Settings.showLabels)
                {
                    return;
                }

                FieldInfo configField = __instance.GetType().GetField("config", BindingFlags.Public | BindingFlags.Instance);
                if (configField == null)
                {
                    return;
                }

                object config = configField.GetValue(__instance);
                if (config == null || config.GetType().Name != "ButtonConfig")
                {
                    return;
                }

                Type configType = config.GetType();
                FieldInfo defNameField = configType.GetField("defName", BindingFlags.Public | BindingFlags.Instance);
                string defName = defNameField?.GetValue(config) as string;
                if (string.IsNullOrEmpty(defName))
                {
                    return;
                }

                if (!LabelCache.TryGetValue(defName, out string label))
                {
                    MainButtonDef def = DefDatabase<MainButtonDef>.GetNamedSilentFail(defName);
                    if (def == null || def.tabWindowClass == null)
                    {
                        return;
                    }

                    if (!ManagerCache.TryGetValue(defName, out string manager))
                    {
                        string tabClass = def.tabWindowClass.Name;
                        if (tabClass == "MainTabWindow_Assign")
                        {
                            manager = "AssignManager";
                        }
                        else if (tabClass == "MainTabWindow_Animals")
                        {
                            manager = "AnimalManager";
                        }
                        else if (tabClass == "MainTabWindow_Schedule")
                        {
                            manager = "ScheduleManager";
                        }
                        else if (tabClass == "MainTabWindow_Work")
                        {
                            manager = "WorkManager";
                        }
                        else if (tabClass == "MainTabWindow_Weapons")
                        {
                            manager = "WeaponsManager";
                        }
                        else if (tabClass == "MainTabWindow_Mechs")
                        {
                            manager = "MechManager";
                        }
                        else
                        {
                            return;
                        }

                        ManagerCache[defName] = manager;
                    }

                    object policy = BpcSyncCommon.GetActivePolicy(manager);
                    if (policy == null)
                    {
                        return;
                    }

                    FieldInfo labelField = policy.GetType().GetField("label", BindingFlags.Public | BindingFlags.Instance);
                    label = labelField?.GetValue(policy) as string;
                    if (string.IsNullOrEmpty(label))
                    {
                        return;
                    }

                    LabelCache[defName] = label;
                }

                if (label == "BPCSynchronizer.AutoPolicyName".Translate())
                {
                    return;
                }

                string displayText = BPCSyncMod.Settings.showFullLabel ? label : label.Substring(0, 1);

                float offsetX = BPCSyncMod.Settings.labelOffsetX;
                float offsetY = BPCSyncMod.Settings.labelOffsetY;

                var labelRect = new Rect(
                    rect.x + (rect.width / 2f) - 50f + offsetX,  // start centered, then offset
                    rect.y + offsetY,
                    100f,  // fixed width centered box
                    18f
                );

                Text.Anchor = TextAnchor.UpperCenter;
                Text.Font = GameFont.Tiny;

                Color oldColor = GUI.color;
                GUI.color = BPCSyncMod.Settings.enableColorChangeWithUINI
                    ? BPCSyncMod.Settings.labelColorWithUINI
                    : Color.white;

                Widgets.Label(labelRect, "(" + displayText + ")");

                GUI.color = oldColor;
                Text.Anchor = TextAnchor.UpperLeft;
                Text.Font = GameFont.Small;
            }
            catch (Exception ex)
            {
                Log.Error("[BPCSync/UINI] Postfix exception: " + ex);
            }
        }

        /// <summary>
        /// Clears cached labels — call this from ApplyPolicyByLabelIndividually if label display needs to refresh.
        /// </summary>
        internal static void ClearCache()
        {
            LabelCache.Clear();
        }
    }
}
