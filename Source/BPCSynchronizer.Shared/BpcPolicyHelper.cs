using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BPCSynchronizer.Patches;
using RimWorld;
using Verse;

namespace BPCSynchronizer
{
    internal static class BpcPolicyHelper
    {
        private static Dictionary<string, FieldInfo> _fieldCache = new Dictionary<string, FieldInfo>();
        private static List<string> _cachedManagerTypeNames;
        private static Dictionary<string, string> _cachedAvailableTypes;

        internal static FieldInfo GetFieldFromTypeOrBase(Type type, string fieldName)
        {
            if (type == null || string.IsNullOrEmpty(fieldName))
            {
                return null;
            }

            string key = $"{type.FullName}.{fieldName}";
            if (_fieldCache.TryGetValue(key, out FieldInfo cachedField))
            {
                return cachedField;
            }

            while (type != null)
            {
                FieldInfo field = type.GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                if (field != null)
                {
                    _fieldCache[key] = field;
                    return field;
                }

                type = type.BaseType;
            }

            return null;
        }

        internal static Dictionary<string, string> GetAvailableManagerTypes()
        {
            if (_cachedAvailableTypes != null)
            {
                return _cachedAvailableTypes;
            }

            var available = new Dictionary<string, string>
            {
                { "AssignManager", "assign" },
                { "AnimalManager", "animal" },
                { "ScheduleManager", "restrict" },
                { "WorkManager", "work" }
            };

            Type widgetType = BpcSyncCommon.GetManagerType("BetterPawnControl.Widget_ModsAvailable");
            if (widgetType != null)
            {
                bool TryGetFlag(string propName)
                {
                    PropertyInfo prop = widgetType.GetProperty(propName, BindingFlags.Public | BindingFlags.Static);
                    return (prop?.GetValue(null) as bool?) == true;
                }

                if (TryGetFlag("WTBAvailable"))
                {
                    available.Add("WeaponsManager", "weapons");
                }

                if (TryGetFlag("MiscRobotsAvailable"))
                {
                    available.Add("RobotManager", "robots");
                }
            }

            if (ModsConfig.BiotechActive)
            {
                available.Add("MechManager", "mech");
            }

            _cachedAvailableTypes = available;
            return available;
        }

        internal static List<string> GetAvailableManagerTypeNames()
        {
            if (_cachedManagerTypeNames != null)
            {
                return _cachedManagerTypeNames;
            }

            _cachedManagerTypeNames = GetAvailableManagerTypes()
                .Keys
                .Select(k => $"BetterPawnControl.{k}")
                .ToList();

            return _cachedManagerTypeNames;
        }

        internal static void ApplyPolicyByLabelIndividually(string label)
        {
            Dictionary<string, string> managerTypeNames = GetAvailableManagerTypes();
            int appliedCount = 0;
            int totalManagers = managerTypeNames.Count;
            var matchedCategories = new List<string>();

            foreach (KeyValuePair<string, string> kvp in managerTypeNames)
            {
                string typeName = $"BetterPawnControl.{kvp.Key}";
                string resourceTypeName = kvp.Value;
                Type managerType = BpcSyncCommon.GetManagerType(typeName);
                if (managerType != null)
                {
                    FieldInfo policiesField = GetFieldFromTypeOrBase(managerType, "policies");
                    MethodInfo loadStateMethod = managerType.GetMethod(
                        "LoadState",
                        BindingFlags.NonPublic | BindingFlags.Static,
                        null,
                        new[] { BpcSyncCommon.GetManagerType("BetterPawnControl.Policy") },  // specify exact parameter type
                        null
                    );

                    if (policiesField == null || loadStateMethod == null)
                    {
                        Log.Warning($"[BPCSync] Could not access policies or LoadState in {typeName}");
                        continue;
                    }

                    if (!(policiesField.GetValue(null) is IEnumerable<object> policies))
                    {
                        continue;
                    }

                    object matchedPolicy = null;

                    foreach (object policy in policies)
                    {
                        FieldInfo labelField = policy.GetType().GetField("label", BindingFlags.Public | BindingFlags.Instance);
                        string policyLabel = labelField?.GetValue(policy) as string;
                        if (string.Equals(policyLabel, label, StringComparison.OrdinalIgnoreCase))
                        {
                            matchedPolicy = policy;
                            break;
                        }
                    }

                    if (matchedPolicy != null)
                    {
                        loadStateMethod.Invoke(null, new[] { matchedPolicy });
                        appliedCount++;
                        matchedCategories.Add(resourceTypeName.First().ToString().ToUpper() + resourceTypeName.Substring(1));
                    }
                }
            }

            string categories = matchedCategories.Any()
                ? $" ({string.Join(", ", matchedCategories)})"
                : string.Empty;

            // ✅ Force tab label refresh by re-setting the current tab
            LongEventHandler.ExecuteWhenFinished(() =>
            {
                MainTabsRoot tabsRoot = Find.MainTabsRoot;
                if (tabsRoot != null)
                {
                    MainButtonDef originalTab = tabsRoot.OpenTab;
                    MainButtonDef fallbackTab = MainButtonDefOf.Inspect;

                    tabsRoot.SetCurrentTab(fallbackTab);
                    tabsRoot.SetCurrentTab(originalTab);
                }
            });

            UINotIncludedPolicyLabelPatch.ClearCache(); // Clear cached labels to refresh UI

            // Add the message to the game (upper left)
            Messages.Message("BPCSynchronizer.PolicyApplied_Message".Translate(label, appliedCount, totalManagers, categories), MessageTypeDefOf.TaskCompletion);
        }
    }
}

