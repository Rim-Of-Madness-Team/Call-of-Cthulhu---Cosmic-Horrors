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
    public class JobDriver_HaulSacrifice : JobDriver
    {
        private const TargetIndex TakeeIndex = TargetIndex.A;
        private const TargetIndex AltarIndex = TargetIndex.B;
        private string customString = "";

        protected Pawn Takee
        {
            get
            {
                return (Pawn)base.CurJob.GetTarget(TargetIndex.A).Thing;
            }
        }

        protected Building_PitChthonian DropAltar
        {
            get
            {
                return (Building_PitChthonian)base.CurJob.GetTarget(TargetIndex.B).Thing;
            }
        }

        [DebuggerHidden]
        protected override IEnumerable<Toil> MakeNewToils()
        {
            //Commence fail checks!
            this.FailOnDestroyedOrNull(TargetIndex.A);
            this.FailOnDestroyedOrNull(TargetIndex.B);

            yield return Toils_Reserve.Reserve(TakeeIndex, 1);
            yield return Toils_Reserve.Reserve(AltarIndex, 1);

            yield return new Toil
            {
                initAction = delegate
                {
                    DropAltar.IsSacrificing = true;
                    customString = "ChthonianPitSacrificeGathering".Translate();
                }
            };
            
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.ClosestTouch).FailOnSomeonePhysicallyInteracting(TargetIndex.A);
            yield return Toils_Construct.UninstallIfMinifiable(TargetIndex.A).FailOnSomeonePhysicallyInteracting(TargetIndex.A);
            yield return Toils_Haul.StartCarryThing(TargetIndex.A);
            yield return Toils_Goto.GotoThing(TargetIndex.B, PathEndMode.Touch);
            Toil chantingTime = new Toil();
            chantingTime.defaultCompleteMode = ToilCompleteMode.Delay;
            chantingTime.defaultDuration = 1200;
            chantingTime.WithProgressBarToilDelay(TargetIndex.A, false, -0.5f);
            chantingTime.initAction = delegate
            {
                customString = "ChthonianPitSacrificeDropping".Translate(new object[]
                    {
                        this.Takee.LabelShort
                    });
            };
            yield return chantingTime;
            yield return new Toil
            {
                initAction = delegate
                {
                    customString = "ChthonianPitSacrificeFinished".Translate();
                    IntVec3 position = this.DropAltar.Position;
                    Thing thing;
                    this.pawn.carryTracker.TryDropCarriedThing(position, ThingPlaceMode.Direct, out thing, null);
                    if (!this.DropAltar.Destroyed)
                    {
                        SacrificeCompleted();
                    }
                },
                defaultCompleteMode = ToilCompleteMode.Instant
            };

            yield return Toils_Reserve.Release(TargetIndex.B);

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


        private void SacrificeCompleted()
        {
            //Drop them in~~
            this.Takee.Position = DropAltar.Position;
            this.Takee.Notify_Teleported(false);
            this.Takee.stances.CancelBusyStanceHard();
            //....the pit
            this.DropAltar.GetInnerContainer().TryAdd(Takee);
            
            //Quiet the pit.
            this.DropAltar.IsActive = false;

            //Satisfy the creature.
            this.DropAltar.GaveSacrifice = true;

            //Record a tale
            TaleRecorder.RecordTale(TaleDefOf.ExecutedPrisoner, new object[]
            {
                        this.pawn,
                        this.Takee
            });
        }
    }
}
