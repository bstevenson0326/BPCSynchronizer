using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace BPCSynchronizer
{
    public class BPCSyncMod : Mod
    {
        public static BPCSyncSettings Settings;
        private static readonly Dictionary<string, Color> PresetColorMap = new Dictionary<string, Color>
        {
            { "White",       Color.white },
            { "Black",       Color.black },
            { "Red",         new Color(1f, 0f, 0f) },
            { "Orange",      new Color(1f, 0.5f, 0f) },
            { "Yellow",      new Color(1f, 1f, 0f) },
            { "Green",       new Color(0f, 1f, 0f) },
            { "Lime",        new Color(0.5f, 1f, 0f) },
            { "Mint",        new Color(0f, 1f, 0.5f) },
            { "Turquoise",   new Color(0.25f, 0.88f, 0.82f) },
            { "Cyan",        new Color(0f, 1f, 1f) },
            { "Sky Blue",    new Color(0f, 0.5f, 1f) },
            { "Blue",        new Color(0f, 0f, 1f) },
            { "Indigo",      new Color(0.29f, 0f, 0.51f) },
            { "Violet",      new Color(0.58f, 0f, 0.83f) },
            { "Magenta",     new Color(1f, 0f, 1f) },
            { "Pink",        new Color(1f, 0.6f, 0.8f) },
            { "Gold",        new Color(1f, 0.84f, 0f) },
            { "Brown",       new Color(0.6f, 0.4f, 0.2f) },
            { "Dark Brown",  new Color(0.4f, 0.2f, 0f) },
            { "Light Gray",  new Color(0.75f, 0.75f, 0.75f) },
            { "Medium Gray", new Color(0.5f, 0.5f, 0.5f) },
            { "Dark Gray",   new Color(0.25f, 0.25f, 0.25f) }
        };

        public BPCSyncMod(ModContentPack content) : base(content)
        {
            Settings = GetSettings<BPCSyncSettings>();
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            var listing = new Listing_Standard();
            listing.Begin(inRect);

            listing.CheckboxLabeled("BPCSynchronizer.ShowPolicyLabel_Settings".Translate(), ref Settings.showLabels, "BPCSynchronizer.ShowPolicyLabelTooltip_Settings".Translate());
            listing.CheckboxLabeled("BPCSynchronizer.ShowFullLabel_Settings".Translate(), ref Settings.showFullLabel, "BPCSynchronizer.ShowFullLabelTooltip_Settings".Translate());

            listing.Gap();
            listing.GapLine();
            listing.Gap();

            // UI Not Included Settings Section
            Text.Font = GameFont.Medium;
            listing.Label("BPCSynchronizer.UINotIncludedModName_Settings".Translate());
            Text.Font = GameFont.Small;

            listing.CheckboxLabeled("BPCSynchronizer.LabelColorUINotIncluded_Settings".Translate(), ref Settings.enableColorChangeWithUINI, "BPCSynchronizer.LabelColorUINotIncludedTooltip_Settings".Translate());
            listing.Gap();
            if (Settings.enableColorChangeWithUINI)
            {
                Settings.labelColorWithUINI = ColorSelector(listing, Settings.labelColorWithUINI);
            }

            listing.Label("BPCSynchronizer.LabelXOffset_Settings".Translate(Settings.labelOffsetX), tooltip: "BPCSynchronizer.LabelXOffsetTooltip_Settings".Translate());
            Settings.labelOffsetX = listing.Slider(Settings.labelOffsetX, -100f, 100f);

            listing.Label("BPCSynchronizer.LabelYOffset_Settings".Translate(Settings.labelOffsetY), tooltip: "BPCSynchronizer.LabelYOffsetTooltip_Settings".Translate());
            Settings.labelOffsetY = listing.Slider(Settings.labelOffsetY, -50f, 50f);

            listing.End();
        }

        public override string SettingsCategory() => "BPC Synchronizer";

        private static Color ColorSelector(Listing_Standard listing, Color currentColor)
        {
            listing.Label("BPCSynchronizer.LabelColor_Settings".Translate());

            float buttonSize = 24f;
            float spacing = 6f;
            float startX = listing.GetRect(0f).xMin;
            float y = listing.CurHeight;

            int col = 0;
            int row = 0;
            int maxPerRow = 11;
            Color selectedColor = currentColor;

            foreach (KeyValuePair<string, Color> kvp in PresetColorMap)
            {
                string name = kvp.Key;
                Color color = kvp.Value;

                float x = startX + col * (buttonSize + spacing);
                float yPos = y + spacing + row * (buttonSize + spacing);
                var colorRect = new Rect(x, yPos, buttonSize, buttonSize);

                GUI.color = color;
                Widgets.DrawBoxSolid(colorRect, color);

                if (Mouse.IsOver(colorRect))
                {
                    TooltipHandler.TipRegion(colorRect, name);
                }

                if (Widgets.ButtonInvisible(colorRect))
                {
                    selectedColor = color;
                }

                if (Mathf.Approximately(color.r, currentColor.r) &&
                    Mathf.Approximately(color.g, currentColor.g) &&
                    Mathf.Approximately(color.b, currentColor.b))
                {
                    GUI.color = Color.white;
                    var iconRect = new Rect(
                        colorRect.x + (colorRect.width - 12f) / 2f,
                        colorRect.y + (colorRect.height - 12f) / 2f,
                        12f, 12f
                    );

                    Texture2D checkmark = ContentFinder<Texture2D>.Get("UI/Widgets/CheckOn", true);
                    GUI.DrawTexture(iconRect, checkmark);
                }

                col++;
                if (col >= maxPerRow)
                {
                    col = 0;
                    row++;
                }
            }

            GUI.color = Color.white;
            listing.Gap((row + 1) * (buttonSize + spacing));
            return selectedColor;
        }
    }
}