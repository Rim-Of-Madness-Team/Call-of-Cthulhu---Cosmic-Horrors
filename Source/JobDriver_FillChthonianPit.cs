// ----------------------------------------------------------------------
// These are basic usings. Always let them be here.
// ----------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

// ----------------------------------------------------------------------
// These are RimWorld-specific usings. Activate/Deactivate what you need:
// ----------------------------------------------------------------------
using UnityEngine;         // Always needed
//using VerseBase;         // Material/Graphics handling functions are found here
using Verse;               // RimWorld universal objects are here (like 'Building')
using Verse.AI;          // Needed when you do something with the AI
using Verse.AI.Group;
using Verse.Sound;       // Needed when you do something with Sound
using Verse.Noise;       // Needed when you do something with Noises
using RimWorld;            // RimWorld specific functions are found here (like 'Building_Battery')
using RimWorld.Planet;   // RimWorld specific functions for world creation
//using RimWorld.SquadAI;  // RimWorld specific functions for squad brains 

namespace CosmicHorror
{
    public class JobDriver_FillChthonianPit : JobDriver
    {
        private const TargetIndex AltarIndex = TargetIndex.A;
        private string customString = "";

        protected Building_PitChthonian DropAltar
        {
            get
            {
                return (Building_PitChthonian)base.CurJob.GetTarget(TargetIndex.A).Thing;
            }
        }

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
                    DropAltar.IsFilling = true;
                    customString = "FillChthonianPitGoing".Translate();
                }
            };


            yield return Toils_Goto.GotoThing(AltarIndex, PathEndMode.Touch);
            Toil chantingTime = new Toil();
            chantingTime.defaultCompleteMode = ToilCompleteMode.Delay;
            chantingTime.defaultDuration = 5000;
            chantingTime.WithProgressBarToilDelay(AltarIndex, false, -0.5f);
            chantingTime.PlaySustainerOrSound(() => SoundDefOf.Interact_ClearSnow);
            chantingTime.initAction = delegate
            {
                customString = "FillChthonianPitFilling".Translate();
            };
            chantingTime.AddPreTickAction(() =>
            {
               if (DropAltar.IsActive)
                {
                    if (Rand.Range(1, 100) > 95)
                    {
                        DropAltar.TrySpawnChthonian();
                    }
                } 
                if (DropAltar.GaveSacrifice)
                {
                    if (this.pawn.IsHashIntervalTick(300))
                    {
                        if (Rand.Range(1, 100) > 60)
                        {
                            Messages.Message("CriesFromBelow".Translate(new object[] 
                            {
                                this.pawn.LabelShort,
                                this.pawn.gender.GetPronoun()
                            }), MessageSound.Negative);
                        }
                    }
                }
            });
            yield return chantingTime;
            yield return new Toil
            {
                initAction = delegate
                {
                    customString = "FillChthonianPitFinished".Translate();
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
            if (customString == "")
            {
                return base.GetReport();
            }
            return customString;
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
    }
}
