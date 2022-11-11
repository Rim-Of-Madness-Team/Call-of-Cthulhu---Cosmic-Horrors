using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
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
            chthonianFaction =
                Find.FactionManager.FirstFactionOfDef(facDef: FactionDef.Named(defName: "ROM_Chthonian"));
        }

        public override void PostTick()
        {
            base.PostTick();

            if (!triggered &&
                (pawn?.Spawned ?? false) &&
                CurStageIndex >= 3 &&
                (this?.pawn?.IsHashIntervalTick(interval: 300) ?? false) &&
                (this?.pawn?.health?.hediffSet != null))
            {
                triggered = true;
                Utility.DebugReport(x: "CurStage :: " + CurStageIndex.ToString());
                Severity = 1f;
                TrySpawningLarva();
                IntVec3 randomCell = new CellRect(minX: pawn.PositionHeld.x - 1, minZ: pawn.PositionHeld.z - 1,
                    width: 3, height: 3).RandomCell;
                if (randomCell.InBounds(map: pawn.Map) &&
                    GenSight.LineOfSight(start: randomCell, end: pawn.PositionHeld, map: pawn.Map))
                {
                    FilthMaker.TryMakeFilth(c: randomCell, map: pawn.MapHeld, filthDef: pawn.RaceProps.BloodDef,
                        source: pawn.LabelIndefinite());
                }

                pawn.Kill(
                    dinfo: new DamageInfo(
                        def: DamageDefOf.Bite,
                        amount: 9999,
                        armorPenetration: 1f,
                        angle: -1f,
                        instigator: pawn,
                        hitPart: pawn.health.hediffSet.GetBrain(),
                        weapon: null
                    ), exactCulprit: this);
                pawn.health.RemoveHediff(hediff: this);
                if (!pawn.health.hediffSet.HasHead)
                    return;
                pawn.health.AddHediff(def: HediffDefOf.MissingBodyPart,
                    part: pawn.health.hediffSet.GetNotMissingParts()
                        .First(predicate: (BodyPartRecord p) => p.def == BodyPartDefOf.Head));
            }
        }

        private void TrySpawningLarva()
        {
            Map map = this.pawn.Map as Map;

            //Get a random cell.
            IntVec3 intVec = this.pawn.Position;

            //Spawn Larva
            var pawns = new List<CosmicHorrorPawn>(
                collection: Utility.SpawnHorrorsOfCountAt(kindDef: MonsterDefOf.ROM_ChthonianLarva, at: intVec,
                    map: map, count: Rand.Range(min: 2, max: 3), fac: Faction.OfPlayer, berserk: false, target: false)
            );
            if (pawns.Count > 0)
            {
                foreach (CosmicHorrorPawn pawn in pawns)
                {
                    pawn.ageTracker.AgeBiologicalTicks = 0;
                    pawn.ageTracker.AgeChronologicalTicks = 0;
                }
            }

            Messages.Message(text: "ChthonianLarvaSpawned".Translate(args: new object[]
            {
                this.pawn.Name.ToStringShort
            }), def: MessageTypeDefOf.PositiveEvent);
        }


        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look<Faction>(refee: ref chthonianFaction, label: "chthonianFaction",
                saveDestroyedThings: false);
            Scribe_Values.Look<bool>(value: ref triggered, label: "triggered", defaultValue: false);
        }
    }
}