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

        public override DamageWorker.DamageResult Apply(DamageInfo dinfo, Thing thing)
        {
            Pawn pawn = thing as Pawn;
            if (pawn == null)
            {
                return base.Apply(dinfo, thing);
            }
            return this.ApplyToPawn(dinfo, pawn);
        }

        private DamageWorker.DamageResult ApplyToPawn(DamageInfo dinfo, Pawn pawn)
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
            BodyPartRecord consciousnessSource = pawn.def.race.body.AllParts.FirstOrDefault((BodyPartRecord x) => x.def == BodyPartDefOf.Brain || x.def.tags.Contains(BodyPartTagDefOf.ConsciousnessSource));
            if (consciousnessSource != null)
            {
                Hediff pressure = pawn.health.hediffSet.GetFirstHediffOfDef(MonsterDefOf.ROM_IntracranialPressure);
                if (pressure == null)
                {
                    pressure = HediffMaker.MakeHediff(MonsterDefOf.ROM_IntracranialPressure, pawn, consciousnessSource);
                    pressure.Severity = 0.01f;
                    pawn.health.AddHediff(pressure, consciousnessSource, null);
                }
                float resultDMG = pressure.Severity + Rand.Range(0.1f, 0.3f);
                pressure.Severity = Mathf.Clamp(resultDMG, 0.0f, 1.0f);
                result.totalDamageDealt += resultDMG;
                return result;
            }
            return result;
            
        }
        
        
    }
}
