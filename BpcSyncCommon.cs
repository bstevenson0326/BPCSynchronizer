using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Verse;

namespace BPCSynchronizer
{
    internal static class BpcSyncCommon
    {
        private static readonly Dictionary<string, Type> CachedManagerTypes = new Dictionary<string, Type>();
        private static readonly Dictionary<string, MethodInfo> CachedGetPolicyMethods = new Dictionary<string, MethodInfo>();

        internal static bool GetBetterPawnControlAssembly(out Assembly asm)
        {
            asm = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.GetName().Name == "BetterPawnControl");
            if (asm == null)
            {
                Log.Error("[BPCSync] BetterPawnControl assembly not found.");
                return false;
            }

            return true;
        }

        internal static Type GetManagerType(string typeName)
        {
            if (CachedManagerTypes.TryGetValue(typeName, out Type cachedType))
            {
                return cachedType;
            }

            if (!GetBetterPawnControlAssembly(out Assembly asm))
            {
                return null;
            }

            Type type = asm.GetType(typeName);
            if (type != null)
            {
                CachedManagerTypes[typeName] = type;
            }

            return type;
        }

        internal static object GetActivePolicy(string managerClassName)
        {
            Type managerType = BpcSyncCommon.GetManagerType("BetterPawnControl." + managerClassName);
            if (managerType == null)
            {
                return null;
            }

            if (!CachedGetPolicyMethods.TryGetValue(managerClassName, out MethodInfo getPolicyMethod))
            {
                Type current = managerType;
                while (current != null)
                {
                    getPolicyMethod = current.GetMethod(
                        "GetActivePolicy",
                        BindingFlags.NonPublic | BindingFlags.Static,
                        null,
                        new[] { typeof(int) },
                        null
                    );

                    if (getPolicyMethod != null)
                    {
                        CachedGetPolicyMethods[managerClassName] = getPolicyMethod;
                        break;
                    }

                    current = current.BaseType;
                }

                if (getPolicyMethod == null)
                {
                    Log.Warning($"[BPCSync] Could not find GetActivePolicy(int) on: {managerClassName}");
                    return null;
                }
            }

            object policy = getPolicyMethod.Invoke(null, new object[] { Find.CurrentMap.uniqueID });
            return policy;
        }
    }
}
