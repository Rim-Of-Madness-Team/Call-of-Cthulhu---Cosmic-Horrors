using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;
using Harmony;
using Verse.AI;
using System;

namespace CosmicHorror
{
    [StaticConstructorOnStartup]
    static class Utility
    {
        static Utility() => HarmonyInstance.Create("rimworld.cosmic_Horrors").Patch(AccessTools.Method(typeof(AttackTargetFinder), nameof(AttackTargetFinder.BestAttackTarget)),
                new HarmonyMethod(typeof(Utility), nameof(BestAttackTargetPrefix)), null);

        static void BestAttackTargetPrefix(ref Predicate<Thing> validator)
        {
            Predicate<Thing> validatorCopy = validator;
            validator = new Predicate<Thing>(delegate (Thing t)
            {
                return (validatorCopy != null ? validatorCopy(t) : true) && (t is CosmicHorrorPawn cosmicPawn ? !cosmicPawn.IsInvisible : true);
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
