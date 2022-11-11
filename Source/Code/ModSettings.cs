using UnityEngine;
using Verse;

namespace CosmicHorror
{
    public class ModMain : Mod
    {
        Settings settings;

        public ModMain(ModContentPack content) : base(content: content)
        {
            settings = GetSettings<Settings>();
            ModInfo.cosmicHorrorRaidDelay = settings.cosmicHorrorEventsDelay;
        }

        public override string SettingsCategory() => "Call of Cthulhu - Cosmic Horrors";

        public override void DoSettingsWindowContents(Rect inRect)
        {
            Widgets.TextFieldNumericLabeled<int>(rect: inRect.TopHalf().TopHalf(),
                label: "cosmicHorrorEventsDelayObject".Translate(), val: ref settings.cosmicHorrorEventsDelay,
                buffer: ref settings.cosmicHorrorEventsDelayBuffer, min: 0, max: 999999);

            settings.Write();
        }
    }

    public class Settings : ModSettings
    {
        public int cosmicHorrorEventsDelay = 0;
        public string cosmicHorrorEventsDelayBuffer;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(value: ref cosmicHorrorEventsDelay, label: "cosmicHorrorEventDelay",
                defaultValue: 0);
        }
    }
}