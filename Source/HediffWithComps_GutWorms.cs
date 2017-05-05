using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace CosmicHorror
{

    class HediffWithComps_GutWorms : HediffWithComps
    {
        private Faction chthonianFaction;


        public override void PostMake()
        {
            base.PostMake();
            chthonianFaction = Find.FactionManager.FirstFactionOfDef(FactionDef.Named("ROM_Chthonian"));
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look<Faction>(ref this.chthonianFaction, "chthonianFaction", false);
        }

        public override void PostTick()
        {
            base.PostTick();

            try
            {

                if (this.pawn != null)
                {
                    if (this.pawn.Spawned)
                    {
                        if (this.CurStageIndex >= 3 && this.pawn.IsHashIntervalTick(300))
                        {
                            if (this.pawn.health != null)
                            {
                                if (this.pawn.health.hediffSet != null)
                                {
                                    Cthulhu.Utility.DebugReport("CurStage :: " + this.CurStageIndex.ToString());
                                    Hediff hediff = this.pawn.health.hediffSet.GetFirstHediffOfDef(HediffDef.Named("ROM_GutWorms"));
                                    if (hediff != null)
                                    {
                                        hediff.Severity = 1f;
                                        Cthulhu.Utility.DebugReport("GutWorms Triggered");
                                        TrySpawningLarva();
                                        DamageInfo dinfo = new DamageInfo(DamageDefOf.Bite, 99999, -1f, null, this.pawn.health.hediffSet.GetBrain(), null);
                                        this.pawn.health.RemoveHediff(hediff);
                                        this.pawn.TakeDamage(dinfo);
                                    }
                                    else
                                    {
                                        hediff = HediffMaker.MakeHediff(HediffDef.Named("ROM_GutWorms"), this.pawn, null);
                                        hediff.Severity = 1f;
                                        this.pawn.health.AddHediff(hediff, null, null);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (NullReferenceException)
            { }
        }

        public void TrySpawningLarva()
        {
            Map map = this.pawn.Map as Map;

            //Get a random cell.
            IntVec3 intVec = this.pawn.Position;

            //Spawn Larva
            List<CosmicHorrorPawn> pawns = new List<CosmicHorrorPawn>();
            //pawns.AddRange(Utility.SpawnHorrorsOfCountAt(MonsterDefOf.ROM_ChthonianLarva, intVec, map, Rand.Range(3, 5), null, false, false));
            
            pawns.AddRange(Utility.SpawnHorrorsOfCountAt(MonsterDefOf.ROM_ChthonianLarva, intVec, map, Rand.Range(2, 3), Faction.OfPlayer, false, false));

            foreach (CosmicHorrorPawn pawn in pawns)
            {
                pawn.ageTracker.AgeBiologicalTicks = 0;
                pawn.ageTracker.AgeChronologicalTicks = 0;
            }

            Messages.Message("ChthonianLarvaSpawned".Translate(new object[]
            {
                this.pawn.NameStringShort
            }), MessageSound.Benefit);
        }
    }
}
