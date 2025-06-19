using System;
using System.Collections.Generic;
using System.Reflection;
using Verse;

namespace BPCSynchronizer
{
    internal static class BpcPolicyDiscovery
    {
        internal static HashSet<string> GetAllPolicyLabels()
        {
            var labels = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            List<string> managerTypeNames = BpcPolicyHelper.GetAvailableManagerTypeNames();
            foreach (string typeName in managerTypeNames)
            {
                Type managerType = BpcSyncCommon.GetManagerType(typeName);
                if (managerType == null)
                {
                    continue;
                }

                MethodInfo forceInit = managerType.GetMethod("ForceInit", BindingFlags.NonPublic | BindingFlags.Static);
                forceInit?.Invoke(null, null);
            }

            foreach (string typeName in managerTypeNames)
            {
                Type managerType = BpcSyncCommon.GetManagerType(typeName);
                if (managerType != null)
                {
                    FieldInfo policiesField = GetPoliciesField(managerType);
                    if (policiesField == null)
                    {
                        Log.Warning($"[BPCSync] No inherited 'policies' field on: {typeName}");
                        continue;
                    }

                    if (!(policiesField.GetValue(null) is System.Collections.IEnumerable policies))
                    {
                        Log.Warning($"[BPCSync] 'policies' field on {typeName} is null or not enumerable.");
                        continue;
                    }

                    foreach (object policy in policies)
                    {
                        FieldInfo labelField = policy.GetType().GetField("label", BindingFlags.Public | BindingFlags.Instance);
                        if (labelField == null)
                        {
                            Log.Warning("[BPCSync] Policy object missing 'label' field.");
                            continue;
                        }

                        string label = labelField.GetValue(policy) as string;
                        if (!string.IsNullOrWhiteSpace(label))
                        {
                            labels.Add(label);
                        }
                    }
                }
                else
                {
                    continue;
                }
            }

            return labels;
        }

        private static FieldInfo GetPoliciesField(Type managerType)
        {
            while (managerType != null)
            {
                FieldInfo field = managerType.GetField("policies", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                if (field != null)
                {
                    return field;
                }

                managerType = managerType.BaseType;
            }

            return null;
        }

        internal static int CountManagersWithPolicy(string label)
        {
            int count = 0;
            if (!BpcSyncCommon.GetBetterPawnControlAssembly(out _))
            {
                return 0;
            }

            List<string> managerTypeNames = BpcPolicyHelper.GetAvailableManagerTypeNames();
            foreach (string typeName in managerTypeNames)
            {
                Type managerType = BpcSyncCommon.GetManagerType(typeName);
                FieldInfo policiesField = BpcPolicyHelper.GetFieldFromTypeOrBase(managerType, "policies");
                if (!(policiesField?.GetValue(null) is IEnumerable<object> policies))
                {
                    continue;
                }

                foreach (object policy in policies)
                {
                    FieldInfo labelField = policy.GetType().GetField("label", BindingFlags.Public | BindingFlags.Instance);
                    if (labelField?.GetValue(policy) as string == label)
                    {
                        count++;
                        break;
                    }
                }
            }

            return count;
        }
    }
}