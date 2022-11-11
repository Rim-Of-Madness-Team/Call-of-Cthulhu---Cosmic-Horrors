using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace CosmicHorror
{
    public class DamageWorker_CranialPressure : DamageWorker
    {
        public override DamageResult Apply(DamageInfo dinfo, Thing thing)
        {
            Pawn pawn = thing as Pawn;
            if (pawn == null)
            {
                return base.Apply(dinfo: dinfo, victim: thing);
            }

            return ApplyToPawn(dinfo: dinfo, pawn: pawn);
        }

        private DamageResult ApplyToPawn(DamageInfo dinfo, Pawn pawn)
        {
            DamageResult result = new DamageResult();
            if (dinfo.Amount <= 0)
            {
                return result;
            }

            if (!DebugSettings.enablePlayerDamage && pawn.Faction == Faction.OfPlayer)
            {
                return result;
            }

            Map mapHeld = pawn.MapHeld;
            bool spawnedOrAnyParentSpawned = pawn.SpawnedOrAnyParentSpawned;
            BodyPartRecord consciousnessSource = pawn.def.race.body.AllParts.FirstOrDefault(
                predicate: (BodyPartRecord x) => x.def == BodyPartDefOf.Brain ||
                                                 x.def.tags.Contains(item: BodyPartTagDefOf.ConsciousnessSource));
            if (consciousnessSource != null)
            {
                Hediff pressure = pawn.health.hediffSet.GetFirstHediffOfDef(def: MonsterDefOf.ROM_IntracranialPressure);
                if (pressure == null)
                {
                    pressure = HediffMaker.MakeHediff(def: MonsterDefOf.ROM_IntracranialPressure, pawn: pawn,
                        partRecord: consciousnessSource);
                    pressure.Severity = 0.01f;
                    pawn.health.AddHediff(hediff: pressure, part: consciousnessSource, dinfo: null);
                }

                float resultDMG = pressure.Severity + Rand.Range(min: 0.1f, max: 0.3f);
                pressure.Severity = Mathf.Clamp(value: resultDMG, min: 0.0f, max: 1.0f);
                result.totalDamageDealt += resultDMG;
                return result;
            }

            return result;
        }
    }
}