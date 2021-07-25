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
            if (GenDate.DaysPassed < (ModInfo.cosmicHorrorRaidDelay + this.def.earliestDay))
            {
                return false;
            }

            return map.listerThings.ThingsOfDef(this.def.mechClusterBuilding).Count <= 0;
            
        }

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            Map map = (Map)parms.target;
            int countToSpawn = this.CountToSpawn;
            IntVec3 cell = IntVec3.Invalid;

                Predicate<IntVec3> validator = delegate (IntVec3 c)
                {
                    if (c.Fogged(map))
                    {
                        return false;
                    }
                    foreach (IntVec3 current in GenAdj.CellsOccupiedBy(c, Rot4.North, this.def.mechClusterBuilding.size))
                    {
                        if (!current.Standable(map))
                        {
                            bool result = false;
                            return result;
                        }
                        if (map.roofGrid.Roofed(current))
                        {
                            bool result = false;
                            return result;
                        }
                    }
                    return map.reachability.CanReachColony(c);
                };
            if (!CellFinderLoose.TryFindRandomNotEdgeCellWith(14, validator, map, out IntVec3 intVec))
            {
                return false;
            }
            //GenExplosion.DoExplosion(intVec, map, 3f, DamageDefOf.Flame, null, null, null, null, null, 0f, 1, false, null, 0f, 1);
            Building_PitChthonian building_CrashedShipPart = (Building_PitChthonian)GenSpawn.Spawn(this.def.mechClusterBuilding, intVec, map);
                building_CrashedShipPart.SetFaction(Find.FactionManager.FirstFactionOfDef(FactionDef.Named("ROM_Chthonian")), null);
               
                cell = intVec;
             
                if (map == Find.CurrentMap)
                {
                    Find.CameraDriver.shaker.DoShake(1f);
                }
                Find.LetterStack.ReceiveLetter(this.def.letterLabel, this.def.letterText, this.def.letterDef, new TargetInfo(cell, map, false), null);
            return true;
        }
    }
}
