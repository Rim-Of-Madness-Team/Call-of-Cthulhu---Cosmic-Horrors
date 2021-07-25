using RimWorld;
using System;
using System.Collections.Generic;
using Verse;

namespace CosmicHorror
{

    class HediffWithComps_GutWorms : HediffWithComps
    {
        private Faction chthonianFaction;
        private bool triggered = false;

        public override void PostMake()
        {
            base.PostMake();
            this.chthonianFaction = Find.FactionManager.FirstFactionOfDef(FactionDef.Named("ROM_Chthonian"));
        }

        public override void PostTick()
        {
            base.PostTick();
            
                if (!triggered &&
                    (pawn?.Spawned ?? false) &&
                    CurStageIndex >= 3 &&
                    (this?.pawn?.IsHashIntervalTick(300) ?? false) &&
                    (this?.pawn?.health?.hediffSet != null))
                {
                    triggered = true;
                    Cthulhu.Utility.DebugReport("CurStage :: " + this.CurStageIndex.ToString());
                    this.Severity = 1f;
                    Cthulhu.Utility.DebugReport("GutWorms Triggered");
                    TrySpawningLarva();
                    this.pawn.TakeDamage(new DamageInfo(DamageDefOf.Bite, 9999, 1f, -1f, this.pawn, this.pawn.health.hediffSet.GetBrain(), null));
                    this.pawn.health.RemoveHediff(this);
                }
        }

        public void TrySpawningLarva()
        {
            Map map = this.pawn.Map as Map;

            //Get a random cell.
            IntVec3 intVec = this.pawn.Position;

            //Spawn Larva
            var pawns = new List<CosmicHorrorPawn>(
                Utility.SpawnHorrorsOfCountAt(MonsterDefOf.ROM_ChthonianLarva, intVec, map, Rand.Range(2, 3), Faction.OfPlayer, false, false)
                );
            if (pawns.Count > 0)
            {
                foreach (CosmicHorrorPawn pawn in pawns)
                {
                    pawn.ageTracker.AgeBiologicalTicks = 0;
                    pawn.ageTracker.AgeChronologicalTicks = 0;
                }
            }
            Messages.Message("ChthonianLarvaSpawned".Translate(new object[]
            {
                this.pawn.Name.ToStringShort
            }), MessageTypeDefOf.PositiveEvent);
        }


        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look<Faction>(ref this.chthonianFaction, "chthonianFaction", false);
            Scribe_Values.Look<bool>(ref this.triggered, "triggered", false);
        }

    }
}
