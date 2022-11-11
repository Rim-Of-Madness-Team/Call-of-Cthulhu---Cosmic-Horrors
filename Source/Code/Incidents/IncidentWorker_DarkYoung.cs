using RimWorld;
using Verse;


namespace CosmicHorror
{
    internal class IncidentWorker_DarkYoung : IncidentWorker
    {
        protected override bool CanFireNowSub(IncidentParms parms)
        {
            Map map = (Map)parms.target;
            if (GenDate.DaysPassed < (ModInfo.cosmicHorrorRaidDelay + def.earliestDay))
            {
                return false;
            }

            return base.CanFireNowSub(parms: parms);
        }

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            Map map = (Map)parms.target;
            if (!RCellFinder.TryFindRandomPawnEntryCell(result: out IntVec3 intVec, map: map, roadChance: 0.5f))
            {
                return false;
            }

            Find.LetterStack.ReceiveLetter(label: "DarkYoungIncidentLabel".Translate(),
                text: "DarkYoungIncidentDesc".Translate(), textLetterDef: LetterDefOf.ThreatSmall,
                lookTargets: new TargetInfo(cell: intVec, map: map), relatedFaction: null);

            SpawnDarkYoung(parms: parms, vecparms: intVec);

            return true;
        }

        public void SpawnDarkYoung(IncidentParms parms, IntVec3 vecparms)
        {
            int iwCount = 1;
            IntVec3 intVec = vecparms;
            Map map = (Map)parms.target;
            PawnKindDef DarkYoung = PawnKindDef.Named(defName: "ROM_DarkYoung");

            iwCount = parms.points switch
            {
                <= 500f => Rand.RangeInclusive(min: 1, max: 2),
                <= 600f => Rand.RangeInclusive(min: 2, max: 3),
                <= 1400f => Rand.RangeInclusive(min: 4, max: 5),
                _ => iwCount
            };

            for (int i = 0; i < iwCount; i++)
            {
                IntVec3 loc = CellFinder.RandomClosewalkCellNear(root: intVec, map: map, radius: 10);
                Pawn newThing = PawnGenerator.GeneratePawn(kindDef: DarkYoung, faction: null);
                CosmicHorrorPawn newDarkYoung = newThing as CosmicHorrorPawn;
                GenSpawn.Spawn(newThing: newDarkYoung, loc: loc, map: map);
            }
        }
    }
}