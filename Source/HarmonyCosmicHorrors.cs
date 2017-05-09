using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Harmony;
using Verse;
using UnityEngine;

namespace CosmicHorror
{
    [StaticConstructorOnStartup]
    static class HarmonyPatches
    {

        static HarmonyPatches()
        {
            HarmonyInstance harmony = HarmonyInstance.Create("rimworld.jecrell.cthulhu.horrors");
            harmony.Patch(AccessTools.Method(typeof(HediffSet), "CalculatePain"), new HarmonyMethod(typeof(HarmonyPatches), "CalculatePain_PreFix"), null);
        }

        // Verse.HediffSet
        public static bool CalculatePain_PreFix(HediffSet __instance, ref float __result)
        {
            CosmicHorrorPawn p = __instance.pawn as CosmicHorrorPawn;
            if (p != null)
            {
                if (p.Dead)
                {
                    __result = 0f;
                    return false;
                }
                float num = 0f;
                for (int i = 0; i < __instance.hediffs.Count; i++)
                {
                    num += __instance.hediffs[i].PainOffset;
                }
                float num2 = num / p.HealthScale;
                for (int j = 0; j < __instance.hediffs.Count; j++)
                {
                    num2 *= __instance.hediffs[j].PainFactor;
                    num2 *= p.PawnExtension.painFactor;
                }
                __result = Mathf.Clamp(num2, 0f, 1f);
                return false;
            }
            return true;
        }

    }
}