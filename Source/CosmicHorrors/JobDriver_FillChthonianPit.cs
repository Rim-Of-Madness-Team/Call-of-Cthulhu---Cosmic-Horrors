// ----------------------------------------------------------------------
// These are basic usings. Always let them be here.
// ----------------------------------------------------------------------
using System.Collections.Generic;
using System.Diagnostics;

// ----------------------------------------------------------------------
// These are RimWorld-specific usings. Activate/Deactivate what you need:
// ----------------------------------------------------------------------
//using VerseBase;         // Material/Graphics handling functions are found here
using Verse;               // RimWorld universal objects are here (like 'Building')
using Verse.AI;          // Needed when you do something with the AI
using RimWorld;            // RimWorld specific functions are found here (like 'Building_Battery')
using System;
//using RimWorld.SquadAI;  // RimWorld specific functions for squad brains 

namespace CosmicHorror
{
    public class JobDriver_FillChthonianPit : JobDriver
    {
        private const TargetIndex AltarIndex = TargetIndex.A;
        private string customString = "";

        protected Building_PitChthonian DropAltar => (Building_PitChthonian)base.job.GetTarget(TargetIndex.A).Thing;

        [DebuggerHidden]
        protected override IEnumerable<Toil> MakeNewToils()
        {
            //Commence fail checks!
            this.FailOnDestroyedOrNull(TargetIndex.A);
            
            yield return Toils_Reserve.Reserve(AltarIndex, 1);

            yield return new Toil
            {
                initAction = delegate
                {
                    this.DropAltar.IsFilling = true;
                    this.customString = "FillChthonianPitGoing".Translate();
                }
            };


            yield return Toils_Goto.GotoThing(AltarIndex, PathEndMode.Touch);
            Toil chantingTime = new Toil()
            {
                defaultCompleteMode = ToilCompleteMode.Delay,
                defaultDuration = 5000
            };
            chantingTime.WithProgressBarToilDelay(AltarIndex, false, -0.5f);
            chantingTime.PlaySustainerOrSound(() => SoundDefOf.Interact_CleanFilth);
            chantingTime.initAction = delegate
            {
                this.customString = "FillChthonianPitFilling".Translate();
            };
            chantingTime.AddPreTickAction(() =>
            {
               if (this.DropAltar.IsActive)
                {
                    if (Rand.Range(1, 100) > 95)
                    {
                        this.DropAltar.TrySpawnChthonian();
                    }
                } 
                if (this.DropAltar.GaveSacrifice)
                {
                    if (this.pawn.IsHashIntervalTick(300))
                    {
                        if (Rand.Range(1, 100) > 60)
                        {
                            Messages.Message("CriesFromBelow".Translate(new object[] 
                            {
                                this.pawn.LabelShort,
                                this.pawn.gender.GetPronoun()
                            }), MessageTypeDefOf.NegativeEvent);
                        }
                    }
                }
            });
            yield return chantingTime;
            yield return new Toil
            {
                initAction = delegate
                {
                    this.customString = "FillChthonianPitFinished".Translate();
                    IntVec3 position = this.DropAltar.Position;
                    FillingCompleted();
                },
                defaultCompleteMode = ToilCompleteMode.Instant
            };

            yield return Toils_Reserve.Release(TargetIndex.A);

            //Toil 9: Think about that.
            yield return new Toil
            {
                initAction = delegate
                {
                    ////It's a day to remember
                    //TaleDef taleToAdd = TaleDef.Named("HeldSermon");
                    //if ((this.pawn.IsColonist || this.pawn.HostFaction == Faction.OfPlayer) && taleToAdd != null)
                    //{
                    //    TaleRecorder.RecordTale(taleToAdd, new object[]
                    //    {
                    //       this.pawn,
                    //    });
                    //}
                },
                defaultCompleteMode = ToilCompleteMode.Instant
            };

            yield break;


        }

        public override string GetReport()
        {
            if (this.customString == "")
            {
                return base.GetReport();
            }
            return this.customString;
        }


        private void FillingCompleted()
        {
            //Quiet the pit.
            if (this.DropAltar.IsActive) this.DropAltar.IsActive = false;

            //Destroy the pit.
            this.DropAltar.Destroy(0);
                        
            //Record a tale
            //TaleRecorder.RecordTale(TaleDefOf.ExecutedPrisoner, new object[]
            //{
            //            this.pawn,
            //});
        }

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return true;
        }
    }
}
