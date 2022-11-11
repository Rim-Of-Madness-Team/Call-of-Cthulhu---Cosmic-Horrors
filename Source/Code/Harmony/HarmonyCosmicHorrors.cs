using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;
using HarmonyLib;
using Verse.AI;
using System;
using UnityEngine;
using System.Reflection.Emit;
using System.Reflection;

namespace CosmicHorror
{
    [StaticConstructorOnStartup]
    static class HarmonyPatches
    {
        static HarmonyPatches()
        {
            var harmony = new Harmony(id: "rimworld.cosmic_Horrors");
            Harmony.DEBUG = true;
            var type = typeof(HarmonyPatches);
            harmony.Patch(
                original: AccessTools.Method(type: typeof(AttackTargetFinder),
                    name: nameof(AttackTargetFinder.BestAttackTarget)),
                prefix: new HarmonyMethod(methodType: type, methodName: nameof(BestAttackTargetPrefix)), postfix: null);
            harmony.Patch(original: AccessTools.Method(type: typeof(JobDriver_PredatorHunt), name: "CheckWarnPlayer"),
                prefix: new HarmonyMethod(methodType: type, methodName: nameof(CheckWarnPlayer_Prefix)), postfix: null);
            //harmony.Patch(AccessTools.Constructor(AccessTools.TypeByName("Wound"), new Type[] { typeof(Pawn) }), null, null, new HarmonyMethod(typeof(HarmonyPatches), nameof(WoundConstructorTranspiler)));
            harmony.Patch(
                original: AccessTools.Method(type: typeof(ThingSelectionUtility),
                    name: nameof(ThingSelectionUtility.SelectableByMapClick)),
                prefix: null,
                postfix: new HarmonyMethod(methodType: type, methodName: nameof(SelectableByMapClickPostfix)));
            harmony.Patch(original: AccessTools.Method(type: typeof(HediffSet), name: "CalculatePain"),
                prefix: new HarmonyMethod(methodType: type, methodName: "CalculatePain_PreFix"), postfix: null);
            harmony.Patch(original: AccessTools.Method(type: typeof(TrashUtility), name: "TrashJob"),
                prefix: new HarmonyMethod(methodType: type, methodName: "TrashJob_Prefix"), postfix: null);
            harmony.Patch(
                original: AccessTools.Method(type: typeof(CollectionsMassCalculator), name: "CapacityTransferables"),
                prefix: new HarmonyMethod(methodType: type, methodName: "CapacityTransferables_PreFix"), postfix: null);
            harmony.Patch(original: AccessTools.Method(type: typeof(LordToil_AssaultColony), name: "UpdateAllDuties"),
                prefix: null,
                postfix: new HarmonyMethod(methodType: type, methodName: "UpdateAllDuties_PostFix"), transpiler: null);
            harmony.Patch(
                original: AccessTools.Method(type: typeof(PawnApparelGenerator),
                    name: nameof(PawnApparelGenerator.GenerateStartingApparelFor)),
                prefix: new HarmonyMethod(methodType: type, methodName: nameof(GenerateStartingApparelFor_PreFix)),
                postfix: null, transpiler: null);
        }

        //JobDriver_PredatorHunt
        public static bool CheckWarnPlayer_Prefix(JobDriver_PredatorHunt __instance)
        {
            if (__instance?.pawn?.def != null &&
                __instance.pawn.def.HasModExtension<PawnExtension>()
                && !__instance.pawn.def.GetModExtension<PawnExtension>().huntAlert)
            {
                Traverse.Create(root: __instance).Field(name: "notifiedPlayerAttacking").SetValue(value: true);
                return false;
            }

            return true;
        }

        public static bool GenerateStartingApparelFor_PreFix(Pawn pawn, PawnGenerationRequest request)
        {
            PawnExtension pawnEx = pawn?.def?.GetModExtension<PawnExtension>();
            if (pawnEx != null)
            {
                if (!pawnEx.generateApparel) return false;
            }

            return true;
        }

        // RimWorld.LordToil_AssaultColony
        public static void UpdateAllDuties_PostFix(LordToil_AssaultColony __instance)
        {
            for (int i = 0; i < __instance.lord.ownedPawns.Count; i++)
            {
                if (__instance.lord.ownedPawns[index: i] is CosmicHorrorPawn cPawn &&
                    cPawn.def.defName != "ROM_Shoggoth")
                    cPawn.mindState.duty =
                        new PawnDuty(def: DefDatabase<DutyDef>.GetNamed(defName: "ROM_AssaultAndKidnap"));
            }
        }


        // RimWorld.CollectionsMassCalculator
        public static bool CapacityTransferables_PreFix(List<TransferableOneWay> transferables, ref float __result)
        {
            Utility.DebugReport(x: "Detour Called: CollectionMassCalc");
            //List<ThingCount> tmpThingCounts
            bool detour = false;
            for (int i = 0; i < transferables.Count; i++)
            {
                if (transferables[index: i].HasAnyThing)
                {
                    if (transferables[index: i].AnyThing.def.defName == "ROM_DarkYoung")
                    {
                        detour = true;
                        break;
                    }
                }
            }

            if (detour)
            {
                ((List<ThingCount>)AccessTools.Field(type: typeof(CollectionsMassCalculator), name: "tmpThingCounts")
                    .GetValue(obj: null)).Clear();
                for (int i = 0; i < transferables.Count; i++)
                {
                    if (transferables[index: i].HasAnyThing)
                    {
                        if (transferables[index: i].AnyThing is Pawn ||
                            transferables[index: i].AnyThing.def.defName == "ROM_DarkYoung")
                        {
                            TransferableUtility.TransferNoSplit(things: transferables[index: i].things,
                                count: transferables[index: i].CountToTransfer,
                                transfer: delegate(Thing originalThing, int toTake)
                                {
                                    ((List<ThingCount>)AccessTools
                                            .Field(type: typeof(CollectionsMassCalculator), name: "tmpThingCounts")
                                            .GetValue(obj: null))
                                        .Add(item: new ThingCount(thing: originalThing, count: toTake));
                                }, removeIfTakingEntireThing: false, errorIfNotEnoughThings: false);
                        }
                    }
                }

                float result = CollectionsMassCalculator.Capacity(thingCounts: ((List<ThingCount>)AccessTools
                    .Field(type: typeof(CollectionsMassCalculator), name: "tmpThingCounts").GetValue(obj: null)));
                ((List<ThingCount>)AccessTools.Field(type: typeof(CollectionsMassCalculator), name: "tmpThingCounts")
                    .GetValue(obj: null)).Clear();
                __result = result;
                return false;
            }

            return true;
        }

        // RimWorld.TrashUtility
        public static bool TrashJob_Prefix(Pawn pawn, Thing t, bool allowPunchingInert, ref Job __result)
        {
            if (pawn is CosmicHorrorPawn)
            {
                Job job3 = new Job(def: JobDefOf.AttackMelee, targetA: t);
                AccessTools.Method(type: typeof(TrashUtility), name: "FinalizeTrashJob")
                    .Invoke(obj: null, parameters: new object[] { job3 });
                __result = job3;
                return false;
            }

            return true;
        }

        // Verse.HediffSet
        static bool CalculatePain_PreFix(HediffSet __instance, ref float __result)
        {
            if (__instance?.pawn is CosmicHorrorPawn p)
            {
                if (p?.Dead ?? false)
                {
                    __result = 0f;
                    return false;
                }

                float num = 0f;
                float num2 = 0f;
                if (__instance.hediffs != null && __instance.hediffs.Count > 0)
                {
                    foreach (Hediff hediff in __instance.hediffs)
                    {
                        num += hediff?.PainOffset ?? 0;
                    }

                    num2 = num / p?.HealthScale ?? 1;
                    num2 *= p?.PawnExtension?.painFactor ?? 1;
                    foreach (Hediff hedifft in __instance.hediffs)
                    {
                        num2 *= hedifft?.PainFactor ?? 1;
                    }
                }

                __result = Mathf.Clamp(value: num2, min: 0f, max: 1f);
                return false;
            }

            return true;
        }

        static void SelectableByMapClickPostfix(Thing t, ref bool __result)
        {
            if (__result)
                __result = !(t is CosmicHorrorPawn cosmicPawn) || !cosmicPawn.IsInvisible;
        }

        public static void BestAttackTargetPrefix(IAttackTargetSearcher searcher, TargetScanFlags flags,
            Predicate<Thing> validator, float minDist, float maxDist,
            IntVec3 locus, float maxTravelRadiusFromLocus,
            bool canBashDoors = false, bool canTakeTargetsCloserThanEffectiveMinRange = true,
            bool canBashFences = false)
        {
            Predicate<Thing> validatorCopy = validator;
            validator = new Predicate<Thing>(delegate(Thing t)
            {
                return (validatorCopy == null || validatorCopy(obj: t))
                       && (!(t is CosmicHorrorPawn cosmicPawn) || !cosmicPawn.IsInvisible);
            });
        }
    }
}