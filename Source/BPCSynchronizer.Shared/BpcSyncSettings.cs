using UnityEngine;
using Verse;

namespace BPCSynchronizer
{
    public class BPCSyncSettings : ModSettings
    {
        public bool showLabels = true;
        public bool showFullLabel = true;
        public bool enableColorChangeWithUINI = true;
        public Color labelColorWithUINI = Color.white;
        public float labelOffsetX = 0f;
        public float labelOffsetY = 20f;

        public override void ExposeData()
        {
            Scribe_Values.Look(ref showLabels, "showLabels", true);
            Scribe_Values.Look(ref showFullLabel, "showFullLabel", true);
            Scribe_Values.Look(ref enableColorChangeWithUINI, "enableColorChangeWithUINI", true);
            Scribe_Values.Look(ref labelColorWithUINI, "labelColorWithUINI", Color.white);
            Scribe_Values.Look(ref labelOffsetX, "labelOffsetX", 0f);
            Scribe_Values.Look(ref labelOffsetY, "labelOffsetY", 0f);
        }
    }
}