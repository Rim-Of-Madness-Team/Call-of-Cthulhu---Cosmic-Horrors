// ----------------------------------------------------------------------
// These are basic usings. Always let them be here.
// ----------------------------------------------------------------------

using System.Collections.Generic;
using System.Diagnostics;

// ----------------------------------------------------------------------
// These are RimWorld-specific usings. Activate/Deactivate what you need:
// ----------------------------------------------------------------------
//using VerseBase;         // Material/Graphics handling functions are found here
using Verse; // RimWorld universal objects are here (like 'Building')
using Verse.AI; // Needed when you do something with the AI
using RimWorld; // RimWorld specific functions are found here (like 'Building_Battery')
using System;

//using RimWorld.SquadAI;  // RimWorld specific functions for squad brains 

namespace CosmicHorror
{
    public class JobDriver_HaulSacrifice : JobDriver
    {
        private const TargetIndex TakeeIndex = TargetIndex.A;
        private const TargetIndex AltarIndex = TargetIndex.B;
        private string customString = "";

        protected Pawn Takee => (Pawn)job.GetTarget(ind: TargetIndex.A).Thing;

        protected Building_PitChthonian DropAltar =>
            (Building_PitChthonian)job.GetTarget(ind: TargetIndex.B).Thing;

        [DebuggerHidden]
        protected override IEnumerable<Toil> MakeNewToils()
        {
            //Commence fail checks!
            this.FailOnDestroyedOrNull(ind: TargetIndex.A);
            this.FailOnDestroyedOrNull(ind: TargetIndex.B);

            yield return Toils_Reserve.Reserve(ind: TakeeIndex, maxPawns: 1);
            yield return Toils_Reserve.Reserve(ind: AltarIndex, maxPawns: 1);

            yield return new Toil
            {
                initAction = delegate
                {
                    DropAltar.IsSacrificing = true;
                    customString = "ChthonianPitSacrificeGathering".Translate();
                }
            };

            yield return Toils_Goto.GotoThing(ind: TargetIndex.A, peMode: PathEndMode.ClosestTouch)
                .FailOnSomeonePhysicallyInteracting(ind: TargetIndex.A);
            yield return Toils_Construct.UninstallIfMinifiable(thingInd: TargetIndex.A)
                .FailOnSomeonePhysicallyInteracting(ind: TargetIndex.A);
            yield return Toils_Haul.StartCarryThing(haulableInd: TargetIndex.A);
            yield return Toils_Goto.GotoThing(ind: TargetIndex.B, peMode: PathEndMode.Touch);
            Toil chantingTime = new Toil()
            {
                defaultCompleteMode = ToilCompleteMode.Delay,
                defaultDuration = 1200
            };
            chantingTime.WithProgressBarToilDelay(ind: TargetIndex.A, interpolateBetweenActorAndTarget: false,
                offsetZ: -0.5f);
            chantingTime.initAction = delegate
            {
                customString = "ChthonianPitSacrificeDropping".Translate(args: new object[]
                {
                    Takee.LabelShort
                });
            };
            yield return chantingTime;
            yield return new Toil
            {
                initAction = delegate
                {
                    customString = "ChthonianPitSacrificeFinished".Translate();
                    IntVec3 position = DropAltar.Position;
                    pawn.carryTracker.TryDropCarriedThing(dropLoc: position, mode: ThingPlaceMode.Direct,
                        resultingThing: out Thing thing, placedAction: null);
                    if (!DropAltar.Destroyed)
                    {
                        SacrificeCompleted();
                    }
                },
                defaultCompleteMode = ToilCompleteMode.Instant
            };

            yield return Toils_Reserve.Release(ind: TargetIndex.B);

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
            Takee.Position = DropAltar.Position;
            Takee.Notify_Teleported(endCurrentJob: false);
            Takee.stances.CancelBusyStanceHard();
            //....the pit
            Takee.DeSpawn();
            DropAltar.GetDirectlyHeldThings().TryAdd(item: Takee);

            //Quiet the pit.
            DropAltar.IsActive = false;

            //Satisfy the creature.
            DropAltar.GaveSacrifice = true;

            //Record a tale
            TaleRecorder.RecordTale(def: TaleDefOf.ExecutedPrisoner, args: new object[]
            {
                pawn,
                Takee
            });
        }

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return true;
        }
    }
}