using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using HugsLib;
using HugsLib.Settings;

namespace CosmicHorror
{
    [StaticConstructorOnStartup]
    public static class HugsModOptionalCode
    {
        static HugsModOptionalCode()
        {
            // Thank god Zhentar & Erdelf
            LongEventHandler.QueueLongEvent(() =>
            {

                cosmicHorrorEventsDelay = () => 0;

                try
                {
                    ((Action)(() =>
                    {
                        //Modpack Settings
                        ModSettingsPack settings = HugsLibController.Instance.Settings.GetModSettings("CosmicHorror");

                        settings.EntryName = "CallOfCthulhuCosmicHorror".Translate();

                        object cosmicHorrorEventsDelayObject = settings.GetHandle<int>(
                            "cosmicHorrorEventsDelay",
                            "cosmicHorrorEventsDelayObject".Translate(),
                            "cosmicHorrorEventsDelayObjectDesc".Translate(),
                            0);

                        cosmicHorrorEventsDelay = () => (SettingHandle<int>)cosmicHorrorEventsDelayObject;


                    }))();
                }
                catch (TypeLoadException)
                { }
            }, "queueHugsLibCosmicHorror", false, null);

        }


        public static Func<int> cosmicHorrorEventsDelay;

    }
}
