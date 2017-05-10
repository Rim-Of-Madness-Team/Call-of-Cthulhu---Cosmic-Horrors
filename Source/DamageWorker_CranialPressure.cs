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
        
        public override float Apply(DamageInfo dinfo, Thing thing)
        {
            Pawn pawn = thing as Pawn;
            if (pawn == null)
            {
                return base.Apply(dinfo, thing);
            }
            return this.ApplyToPawn(dinfo, pawn);
        }

        private float ApplyToPawn(DamageInfo dinfo, Pawn pawn)
        {
            if (dinfo.Amount <= 0)
            {
                return 0f;
            }
            if (!DebugSettings.enablePlayerDamage && pawn.Faction == Faction.OfPlayer)
            {
                return 0f;
            }
            Map mapHeld = pawn.MapHeld;
            bool spawnedOrAnyParentSpawned = pawn.SpawnedOrAnyParentSpawned;
            BodyPartRecord consciousnessSource = pawn.def.race.body.AllParts.FirstOrDefault((BodyPartRecord x) => x.def == BodyPartDefOf.Brain || x.def.tags.Contains("ConsciousnessSource"));
            if (consciousnessSource != null)
            {
                Hediff pressure = pawn.health.hediffSet.GetFirstHediffOfDef(MonsterDefOf.ROM_IntracranialPressure);
                if (pressure == null)
                {
                    pressure = HediffMaker.MakeHediff(MonsterDefOf.ROM_IntracranialPressure, pawn, consciousnessSource);
                    pressure.Severity = 0.01f;
                    pawn.health.AddHediff(pressure, consciousnessSource, null);
                }
                float result = pressure.Severity + Rand.Range(0.1f, 0.3f);
                pressure.Severity = Mathf.Clamp(result, 0.0f, 1.0f);
                return 0f;
            }
            return 0f;
            
        }
        
        
    }
}
