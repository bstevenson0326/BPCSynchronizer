using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace BPCSynchronizer
{
    internal static class BpcPolicyUI
    {
        internal static void ShowPolicySelector()
        {
            int totalManagers = BpcPolicyHelper.GetAvailableManagerTypes().Count;
            var labelCounts = BpcPolicyDiscovery
                .GetAllPolicyLabels()
                .Select(label => new
                {
                    Label = label,
                    Count = BpcPolicyDiscovery.CountManagersWithPolicy(label)
                })
                .OrderByDescending(x => x.Count)
                .ThenBy(x => x.Label)
                .ToList();

            if (labelCounts.Count == 0)
            {
                Messages.Message("BPCSynchronizer.NoPolicy_Message".Translate(), MessageTypeDefOf.RejectInput);
                return;
            }

            var options = new List<FloatMenuOption>();
            foreach (var entry in labelCounts)
            {
                string displayLabel = $"{entry.Label} ({entry.Count}/{totalManagers})";

                options.Add(new FloatMenuOption(displayLabel, () =>
                {
                    BpcPolicyHelper.ApplyPolicyByLabelIndividually(entry.Label);
                }));
            }

            Find.WindowStack.Add(new FloatMenu(options));
        }
    }
}
