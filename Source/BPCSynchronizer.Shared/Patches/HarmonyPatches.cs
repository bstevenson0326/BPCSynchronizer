using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace BPCSynchronizer.Patches
{
    [StaticConstructorOnStartup]
    internal static class HarmonyPatches
    {
        private static Texture2D _syncIcon;
        private static readonly Dictionary<Type, FieldInfo> CachedLabelFields = new Dictionary<Type, FieldInfo>();

        static HarmonyPatches()
        {
            var harmony = new Harmony("com.hawqeye19.bpcsync");

            // Patch for tab label display
            MethodInfo labelCapGetter = AccessTools.PropertyGetter(typeof(Def), "LabelCap");
            if (labelCapGetter != null)
            {
                harmony.Patch(
                    labelCapGetter,
                    postfix: new HarmonyMethod(typeof(HarmonyPatches), nameof(AppendPolicyToTabLabel))
                );
            }
            else
            {
                Log.Warning("[BPCSync] Failed to find Def.LabelCap getter.");
            }

            // Patch for adding sync button to PlaySettings
            MethodInfo playSettingsMethod = AccessTools.Method(typeof(PlaySettings), "DoPlaySettingsGlobalControls");
            if (playSettingsMethod != null)
            {
                harmony.Patch(
                    playSettingsMethod,
                    postfix: new HarmonyMethod(typeof(HarmonyPatches), nameof(DoPlaySettingsGlobalControls_Postfix))
                );
            }
            else
            {
                Log.Warning("[BPCSync] Failed to find PlaySettings.DoPlaySettingsGlobalControls.");
            }

            // Delay UINotIncluded patch until mod types are loaded
            LongEventHandler.ExecuteWhenFinished(() =>
            {
                try
                {
                    Type type = AccessTools.TypeByName("UINotIncluded.Widget.Workers.Button_Worker");
                    if (type != null)
                    {
                        MethodInfo method = AccessTools.Method(type, "OnGUI", new[] { typeof(Rect) });
                        if (method != null)
                        {
                            MethodInfo postfix = typeof(UINotIncludedPolicyLabelPatch).GetMethod("Postfix", BindingFlags.Static | BindingFlags.Public);
                            if (postfix != null)
                            {
                                var harmonyDelayed = new Harmony("com.hawqeye19.bpcsync-uini");
                                harmonyDelayed.Patch(method, postfix: new HarmonyMethod(postfix));
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.Error("[BPCSync] Error patching UINI: " + ex.Message);
                }
            });

            LoadStatePatch.Apply(harmony);

            Log.Message("[BPCSync] Initialized.");
        }

        internal static void DoPlaySettingsGlobalControls_Postfix(WidgetRow row, bool worldView)
        {
            if (worldView || row == null)
            {
                return;
            }

            if (_syncIcon == null)
            {
                _syncIcon = ContentFinder<Texture2D>.Get("UI/Buttons/PolicyIcon", false)
                             ?? TexButton.Copy;
            }

            if (row.ButtonIcon(_syncIcon, "BPCSynchronizer.SelectPolicy".Translate(), GenUI.MouseoverColor))
            {
                SoundDefOf.Click.PlayOneShotOnCamera();
                BpcPolicyUI.ShowPolicySelector();
            }
        }

        private static void AppendPolicyToTabLabel(ref TaggedString __result, Def __instance)
        {
            if (!BPCSyncMod.Settings.showLabels)
            {
                return;
            }

            if (__instance is MainButtonDef mainButtonDef)
            {
                string manager = null;
                string tabWindowClassName = mainButtonDef.tabWindowClass?.Name;

                if (tabWindowClassName == "MainTabWindow_Assign")
                {
                    manager = "AssignManager";
                }
                else if (tabWindowClassName == "MainTabWindow_Animals")
                {
                    manager = "AnimalManager";
                }
                else if (tabWindowClassName == "MainTabWindow_Schedule")
                {
                    manager = "ScheduleManager";
                }
                else if (tabWindowClassName == "MainTabWindow_Work")
                {
                    manager = "WorkManager";
                }
                else if (tabWindowClassName == "MainTabWindow_Weapons")
                {
                    manager = "WeaponsManager";
                }
                else if (tabWindowClassName == "MainTabWindow_Mechs")
                {
                    manager = "MechManager";
                }

                if (manager == null)
                {
                    return;
                }

                try
                {
                    object policy = BpcSyncCommon.GetActivePolicy(manager);
                    if (policy == null)
                    {
                        return;
                    }

                    Type policyType = policy.GetType();
                    if (!CachedLabelFields.TryGetValue(policyType, out FieldInfo labelField))
                    {
                        labelField = policyType.GetField("label", BindingFlags.Public | BindingFlags.Instance);
                        if (labelField != null)
                        {
                            CachedLabelFields[policyType] = labelField;
                        }
                    }

                    string label = labelField?.GetValue(policy) as string;
                    if (string.IsNullOrEmpty(label))
                    {
                        return;
                    }

                    if (label.Equals("BPCSynchronizer.AutoPolicyName".Translate(), StringComparison.OrdinalIgnoreCase))
                    {
                        return; // Don't show anything for "Default"
                    }

                    string display = BPCSyncMod.Settings.showFullLabel ? label : label.Substring(0, 1);
                    __result += $" ({display})";
                }
                catch (Exception ex)
                {
                    Log.Warning($"[BPCSync] Failed to append label for {manager}: {ex.Message}");
                }
            }
        }

        private static class LoadStatePatch
        {
            public static void Apply(Harmony harmony)
            {
                foreach (string typeName in BpcPolicyHelper.GetAvailableManagerTypeNames())
                {
                    Type managerType = BpcSyncCommon.GetManagerType(typeName);
                    if (managerType == null)
                    {
                        continue;
                    }

                    Type policyType = BpcSyncCommon.GetManagerType("BetterPawnControl.Policy");
                    if (policyType == null)
                    {
                        continue;
                    }

                    // Patch LoadState(Policy)
                    MethodInfo singleArg = managerType.GetMethod(
                        "LoadState",
                        BindingFlags.NonPublic | BindingFlags.Static,
                        null,
                        new[] { policyType },
                        null
                    );

                    if (singleArg != null)
                    {
                        harmony.Patch(
                            singleArg,
                            postfix: new HarmonyMethod(typeof(LoadStatePatch), nameof(AfterLoadState))
                        );

                        //Log.Message($"[BPCSync] Patched LoadState(Policy) on {typeName}");
                    }

                    // Patch LoadState(List<Link>, List<Pawn>, Policy)
                    MethodInfo multiArg = managerType.GetMethods(BindingFlags.NonPublic | BindingFlags.Static)
                        .FirstOrDefault(m =>
                        {
                            if (m.Name != "LoadState")
                            {
                                return false;
                            }

                            ParameterInfo[] p = m.GetParameters();
                            return p.Length == 3
                                   && typeof(System.Collections.IEnumerable).IsAssignableFrom(p[0].ParameterType)
                                   && p[1].ParameterType == typeof(List<Pawn>)
                                   && p[2].ParameterType == policyType;
                        });

                    if (multiArg != null)
                    {
                        harmony.Patch(
                            multiArg,
                            postfix: new HarmonyMethod(typeof(LoadStatePatch), nameof(AfterLoadState))
                        );

                        //Log.Message($"[BPCSync] Patched LoadState(List<>, List<Pawn>, Policy) on {typeName}");
                    }

                    if (singleArg == null && multiArg == null)
                    {
                        Log.Warning($"[BPCSync] No LoadState overloads found on {typeName}");
                    }
                }
            }

            public static void AfterLoadState()
            {
                try
                {
                    UINotIncludedPolicyLabelPatch.ClearCache();
                }
                catch (Exception ex)
                {
                    Log.Warning($"[BPCSync] LoadState Postfix exception: {ex}");
                }
            }
        }
    }
}
