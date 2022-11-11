using System;
using Verse;
using RimWorld;

namespace CosmicHorror
{
    public class IncidentWorker_PitChthonian : IncidentWorker
    {
        private const int IncidentMinimumPoints = 300;

        protected virtual int CountToSpawn => 1;

        protected override bool CanFireNowSub(IncidentParms parms)
        {
            Map map = (Map)parms.target;
            if (GenDate.DaysPassed < (ModInfo.cosmicHorrorRaidDelay + def.earliestDay))
            {
                return false;
            }

            return map.listerThings.ThingsOfDef(def: def.mechClusterBuilding).Count <= 0;
        }

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            Map map = (Map)parms.target;
            int countToSpawn = CountToSpawn;
            IntVec3 cell = IntVec3.Invalid;

            Predicate<IntVec3> validator = delegate(IntVec3 c)
            {
                if (c.Fogged(map: map))
                {
                    return false;
                }

                foreach (IntVec3 current in GenAdj.CellsOccupiedBy(center: c, rotation: Rot4.North,
                             size: def.mechClusterBuilding.size))
                {
                    if (!current.Standable(map: map))
                    {
                        bool result = false;
                        return result;
                    }

                    if (map.roofGrid.Roofed(c: current))
                    {
                        bool result = false;
                        return result;
                    }
                }

                return map.reachability.CanReachColony(c: c);
            };
            if (!CellFinderLoose.TryFindRandomNotEdgeCellWith(minEdgeDistance: 14, validator: validator, map: map,
                    result: out IntVec3 intVec))
            {
                return false;
            }

            //GenExplosion.DoExplosion(intVec, map, 3f, DamageDefOf.Flame, null, null, null, null, null, 0f, 1, false, null, 0f, 1);
            Building_PitChthonian building_CrashedShipPart =
                (Building_PitChthonian)GenSpawn.Spawn(def: def.mechClusterBuilding, loc: intVec, map: map);
            building_CrashedShipPart.SetFaction(
                newFaction: Find.FactionManager.FirstFactionOfDef(facDef: FactionDef.Named(defName: "ROM_Chthonian")),
                recruiter: null);

            cell = intVec;

            if (map == Find.CurrentMap)
            {
                Find.CameraDriver.shaker.DoShake(mag: 1f);
            }

            Find.LetterStack.ReceiveLetter(label: def.letterLabel, text: def.letterText,
                textLetterDef: def.letterDef,
                lookTargets: new TargetInfo(cell: cell, map: map, allowNullMap: false), relatedFaction: null);
            return true;
        }
    }
}