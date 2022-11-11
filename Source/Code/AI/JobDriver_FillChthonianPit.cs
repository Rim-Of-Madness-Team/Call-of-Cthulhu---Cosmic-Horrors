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

namespace CosmicHorror
{
    public class JobDriver_FillChthonianPit : JobDriver
    {
        private const TargetIndex AltarIndex = TargetIndex.A;
        private string customString = "";

        private Building_PitChthonian pit =>
            (Building_PitChthonian)job.GetTarget(ind: TargetIndex.A).Thing;

        [DebuggerHidden]
        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDestroyedOrNull(ind: TargetIndex.A);
            yield return Toils_Reserve.Reserve(ind: AltarIndex, maxPawns: 1);
            yield return new Toil
            {
                initAction = delegate
                {
                    pit.IsFilling = true;
                    customString = "FillChthonianPitGoing".Translate();
                }
            };
            yield return Toils_Goto.GotoThing(ind: AltarIndex, peMode: PathEndMode.Touch);
            Toil filling = new Toil()
            {
                defaultCompleteMode = ToilCompleteMode.Delay,
                defaultDuration = 5000
            };
            filling.WithProgressBarToilDelay(ind: AltarIndex, interpolateBetweenActorAndTarget: false,
                offsetZ: -0.5f);
            filling.PlaySustainerOrSound(soundDefGetter: () => SoundDefOf.Interact_CleanFilth);
            filling.initAction = delegate { customString = "FillChthonianPitFilling".Translate(); };
            filling.AddPreTickAction(newAct: () =>
            {
                if (pit.IsActive)
                {
                    if (Rand.Range(min: 1, max: 100) > 95)
                    {
                        pit.TrySpawnChthonian();
                    }
                }
                if (pit?.container?.Count > 0)
                {
                    if (pawn.IsHashIntervalTick(interval: 300))
                    {
                        if (Rand.Range(min: 1, max: 100) > 60)
                        {
                            Messages.Message(text: "CriesFromBelow".Translate(args: new object[]
                            {
                                pawn.LabelShort,
                                pawn.gender.GetPronoun()
                            }), def: MessageTypeDefOf.NegativeEvent);
                        }
                    }
                }
            });
            yield return filling;
            yield return new Toil
            {
                initAction = delegate
                {
                    customString = "FillChthonianPitFinished".Translate();
                    IntVec3 position = pit.Position;
                    FillingCompleted();
                },
                defaultCompleteMode = ToilCompleteMode.Instant
            };

            yield return Toils_Reserve.Release(ind: TargetIndex.A);

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
            if (pit.IsActive) pit.IsActive = false;

            //Destroy the pit.
            pit.Destroy(mode: 0);

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