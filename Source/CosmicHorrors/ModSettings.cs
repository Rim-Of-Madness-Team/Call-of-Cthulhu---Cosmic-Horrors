using UnityEngine;
using Verse;

namespace CosmicHorror
{

    public class ModMain : Mod
    {
        Settings settings;

        public ModMain(ModContentPack content) : base(content)
        {
            this.settings = GetSettings<Settings>();
            ModInfo.cosmicHorrorRaidDelay = this.settings.cosmicHorrorEventsDelay;
        }

        public override string SettingsCategory() => "Call of Cthulhu - Cosmic Horrors";

        public override void DoSettingsWindowContents(Rect inRect)
        {
            Widgets.TextFieldNumericLabeled<int>(inRect.TopHalf().TopHalf(), "cosmicHorrorEventsDelayObject".Translate(), ref this.settings.cosmicHorrorEventsDelay, ref this.settings.cosmicHorrorEventsDelayBuffer, 0, 999999);

            this.settings.Write();
        }
    }

    public class Settings : ModSettings
    {
        public int cosmicHorrorEventsDelay = 0;
        public string cosmicHorrorEventsDelayBuffer;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref this.cosmicHorrorEventsDelay, "cosmicHorrorEventDelay", 0);
        }
    }
}
