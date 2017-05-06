using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace CosmicHorror
{
    static class Utility
    {

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
