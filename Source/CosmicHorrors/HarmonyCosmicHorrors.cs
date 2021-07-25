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
            var harmony = new Harmony("rimworld.cosmic_Horrors");
            var type = typeof(HarmonyPatches);
            harmony.Patch(AccessTools.Method(typeof(AttackTargetFinder), nameof(AttackTargetFinder.BestAttackTarget)),
                new HarmonyMethod(type, nameof(BestAttackTargetPrefix)), null);
            harmony.Patch(AccessTools.Method(typeof(JobDriver_PredatorHunt), "CheckWarnPlayer"),
                new HarmonyMethod(type, nameof(CheckWarnPlayer_Prefix)), null);
            //harmony.Patch(AccessTools.Constructor(AccessTools.TypeByName("Wound"), new Type[] { typeof(Pawn) }), null, null, new HarmonyMethod(typeof(HarmonyPatches), nameof(WoundConstructorTranspiler)));
            harmony.Patch(AccessTools.Method(typeof(ThingSelectionUtility), nameof(ThingSelectionUtility.SelectableByMapClick)), null, new HarmonyMethod(type, nameof(SelectableByMapClickPostfix)));
            harmony.Patch(AccessTools.Method(typeof(HediffSet), "CalculatePain"), new HarmonyMethod(type, "CalculatePain_PreFix"), null);
            harmony.Patch(AccessTools.Method(typeof(TrashUtility), "TrashJob"), new HarmonyMethod(type, "TrashJob_Prefix"), null);
            harmony.Patch(AccessTools.Method(typeof(CollectionsMassCalculator), "CapacityTransferables"), new HarmonyMethod(type, "CapacityTransferables_PreFix"), null);
            harmony.Patch(AccessTools.Method(typeof(LordToil_AssaultColony), "UpdateAllDuties"), null, 
                new HarmonyMethod(type, "UpdateAllDuties_PostFix"), null);
            harmony.Patch(AccessTools.Method(typeof(PawnApparelGenerator), nameof(PawnApparelGenerator.GenerateStartingApparelFor)),
                new HarmonyMethod(type, nameof(GenerateStartingApparelFor_PreFix)), null, null);
        }

        //JobDriver_PredatorHunt
        public static bool CheckWarnPlayer_Prefix(JobDriver_PredatorHunt __instance)
        {
            if (__instance?.pawn?.def != null &&
                __instance.pawn.def.HasModExtension<CosmicHorror.PawnExtension>()
                && !__instance.pawn.def.GetModExtension<CosmicHorror.PawnExtension>().huntAlert)
            {
                Traverse.Create(__instance).Field("notifiedPlayerAttacking").SetValue(true);
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
                if (__instance.lord.ownedPawns[i] is CosmicHorrorPawn cPawn && cPawn.def.defName != "ROM_Shoggoth")
                    cPawn.mindState.duty = new PawnDuty(DefDatabase<DutyDef>.GetNamed("ROM_AssaultAndKidnap"));
            }

        }


        // RimWorld.CollectionsMassCalculator
        public static bool CapacityTransferables_PreFix(List<TransferableOneWay> transferables, ref float __result)
        {
            Cthulhu.Utility.DebugReport("Detour Called: CollectionMassCalc");
            //List<ThingCount> tmpThingCounts
            bool detour = false;
            for (int i = 0; i < transferables.Count; i++)
            {
                if (transferables[i].HasAnyThing)
                {
                    if (transferables[i].AnyThing.def.defName == "ROM_DarkYoung") { detour = true; break; }
                }
            }
            if (detour)
            {
                ((List<ThingCount>)AccessTools.Field(typeof(CollectionsMassCalculator), "tmpThingCounts").GetValue(null)).Clear();
                for (int i = 0; i < transferables.Count; i++)
                {
                    if (transferables[i].HasAnyThing)
                    {
                        if (transferables[i].AnyThing is Pawn ||
                            transferables[i].AnyThing.def.defName == "ROM_DarkYoung")
                        {
                            TransferableUtility.TransferNoSplit(transferables[i].things, transferables[i].CountToTransfer, delegate (Thing originalThing, int toTake)
                            {
                                ((List<ThingCount>)AccessTools.Field(typeof(CollectionsMassCalculator), "tmpThingCounts").GetValue(null)).Add(new ThingCount(originalThing, toTake));

                            }, false, false);
                        }
                    }
                }
                float result = CollectionsMassCalculator.Capacity(((List<ThingCount>)AccessTools.Field(typeof(CollectionsMassCalculator), "tmpThingCounts").GetValue(null)));
                ((List<ThingCount>)AccessTools.Field(typeof(CollectionsMassCalculator), "tmpThingCounts").GetValue(null)).Clear();
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
                Job job3 = new Job(JobDefOf.AttackMelee, t);
                AccessTools.Method(typeof(TrashUtility), "FinalizeTrashJob").Invoke(null, new object[] { job3 });
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
                __result = Mathf.Clamp(num2, 0f, 1f);
                return false;
            }
            return true;
        }

        static void SelectableByMapClickPostfix(Thing t, ref bool __result)
        {
            if (__result)
                __result = !(t is CosmicHorrorPawn cosmicPawn) || !cosmicPawn.IsInvisible;
        }

        //static IEnumerable<CodeInstruction> WoundConstructorTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator il)
        //{
        //    FieldInfo pawnDefInfo = AccessTools.Field(typeof(Thing), nameof(Thing.def));
        //    FieldInfo defNameInfo = AccessTools.Field(typeof(Def), nameof(Def.defName));
        //    MethodInfo startsWithInfo = AccessTools.Method(typeof(String), nameof(String.StartsWith), new Type[] { typeof(string) });
        //    bool didIt = false;
        //    List<CodeInstruction> instructionList = instructions.ToList();
        //    for (int i = 0; i < instructionList.Count; i++)
        //    {
        //        CodeInstruction instruction = instructionList[i];

        //        if (!didIt && instruction.opcode == OpCodes.Bne_Un)
        //        {
        //            Label label = il.DefineLabel();
        //            instructionList[i + 1].labels = new List<Label>() { label };
        //            yield return new CodeInstruction(OpCodes.Beq_S, label);
        //            yield return new CodeInstruction(OpCodes.Ldarg_1);
        //            yield return new CodeInstruction(OpCodes.Ldfld, pawnDefInfo);
        //            yield return new CodeInstruction(OpCodes.Ldfld, defNameInfo);
        //            yield return new CodeInstruction(OpCodes.Ldstr, "ROM_");
        //            yield return new CodeInstruction(OpCodes.Callvirt, startsWithInfo);
        //            instruction.opcode = OpCodes.Brfalse_S;
        //            didIt = true;
        //        }
        //        yield return instruction;
        //    }
        //}

        static void BestAttackTargetPrefix(ref Predicate<Thing> validator)
        {
            Predicate<Thing> validatorCopy = validator;
            validator = new Predicate<Thing>(delegate (Thing t)
            {
                return (validatorCopy == null || validatorCopy(t)) && (!(t is CosmicHorrorPawn cosmicPawn) || !cosmicPawn.IsInvisible);
            });
        }

        public static bool IsTameable(PawnKindDef kindDef)
        {
            if (kindDef.defName == "ROM_ChthonianLarva" ||
                kindDef.defName == "ROM_DarkYoung")
                return true;
            return false;
        }

        public static IEnumerable<CosmicHorrorPawn> SpawnHorrorsOfCountAt(PawnKindDef kindDef, IntVec3 at, Map map, int count, Faction fac = null, bool berserk = false, bool target = false)
        {
            List<CosmicHorrorPawn> pawns = new List<CosmicHorrorPawn>();
            for (int i = 1; i <= count; i++)
            {
                if ((from cell in GenAdj.CellsAdjacent8Way(new TargetInfo(at, map))
                     where at.Walkable(map) && !at.Fogged(map) && at.InBounds(map)
                     select cell).TryRandomElement(out at))
                {
                    CosmicHorrorPawn pawn = null;
                    if (fac != Faction.OfPlayer)
                    {
                        PawnGenerationRequest request = new PawnGenerationRequest(kindDef, fac, PawnGenerationContext.NonPlayer, map.Tile);
                        pawn = (CosmicHorrorPawn)GenSpawn.Spawn(PawnGenerator.GeneratePawn(request), at, map);
                    }
                    else
                    {
                        pawn = (CosmicHorrorPawn)GenSpawn.Spawn(PawnGenerator.GeneratePawn(kindDef, fac), at, map);
                    }

                    pawns.Add(pawn);
                    if (berserk) pawn.mindState.mentalStateHandler.TryStartMentalState(MentalStateDefOf.Berserk);
                }
            }
            return pawns.AsEnumerable<CosmicHorrorPawn>();
        }
    }
}