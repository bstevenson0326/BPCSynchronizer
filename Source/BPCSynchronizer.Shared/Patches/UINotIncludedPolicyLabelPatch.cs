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
                // Step 1: Get config field
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
                    // Step 2: Resolve MainButtonDef
                    MainButtonDef def = DefDatabase<MainButtonDef>.GetNamedSilentFail(defName);
                    if (def == null || def.tabWindowClass == null)
                    {
                        return;
                    }

                    // Step 3: Resolve manager from class
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

                    // Step 4: Resolve current policy label
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

                // Optional: Skip drawing if Default
                if (label == "BPCSynchronizer.AutoPolicyName".Translate())
                {
                    return;
                }

                // Step 5: Draw label inside top right corner of button
                float margin = 4f;
                var labelRect = new Rect(rect.x, rect.y + 2f, rect.width - margin, 18f);

                Text.Anchor = TextAnchor.UpperRight;
                Text.Font = GameFont.Tiny;
                Color oldColor = GUI.color;
                GUI.color = Color.yellow;
                Widgets.Label(labelRect, "(" + label + ")");
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
