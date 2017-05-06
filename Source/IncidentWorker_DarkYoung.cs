
using RimWorld;
using Verse;


namespace CosmicHorror
{
    internal class IncidentWorker_DarkYoung : IncidentWorker
    {

        protected override bool CanFireNowSub(IIncidentTarget target)
        {
            Map map = (Map)target;
            if (GenDate.DaysPassed < (ModInfo.cosmicHorrorRaidDelay + this.def.earliestDay))
            {
                return false;
            }
            return base.CanFireNowSub(target);
        }

        public override bool TryExecute(IncidentParms parms)
        {
            Map map = (Map)parms.target;
            if (!RCellFinder.TryFindRandomPawnEntryCell(out IntVec3 intVec, map, 0.5f))
            {
                return false;
            }
            Find.LetterStack.ReceiveLetter("DarkYoungIncidentLabel".Translate(), "DarkYoungIncidentDesc".Translate(), LetterDefOf.BadNonUrgent, new TargetInfo(intVec, map), null);

            SpawnDarkYoung(parms, intVec);

            return true;
        }
        public void SpawnDarkYoung(IncidentParms parms, IntVec3 vecparms)
        {
            int iwCount = 1;
            IntVec3 intVec = vecparms;
            Map map = (Map)parms.target;
            PawnKindDef DarkYoung = PawnKindDef.Named("ROM_DarkYoung");

            if (parms.points <= 200f)
            {
                iwCount = Rand.RangeInclusive(1, 2);
            }
            else if (parms.points <= 400f)
            {
                iwCount = Rand.RangeInclusive(2, 3);
            }
            else if (parms.points <= 700f)
            {
                iwCount = Rand.RangeInclusive(4, 5);
            }
            else if (parms.points <= 1400f)
            {
                iwCount = Rand.RangeInclusive(4, 6);
            }
            else if (parms.points <= 2500f)
            {
                iwCount = Rand.RangeInclusive(4, 8);
            }
            
            for (int i = 0; i < iwCount; i++)
            {
                IntVec3 loc = CellFinder.RandomClosewalkCellNear(intVec, map, 10);
                Pawn newThing = PawnGenerator.GeneratePawn(DarkYoung, null);
                CosmicHorrorPawn newDarkYoung = newThing as CosmicHorrorPawn;
                GenSpawn.Spawn(newDarkYoung, loc, map);
            }
        }
    }
}
