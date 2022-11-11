using RimWorld;
using System;
using System.Collections.Generic;
using Verse;

namespace CosmicHorror
{
    public class DamageWorker_Constrict : DamageWorker_AddInjury
    {
        public override DamageResult Apply(DamageInfo dinfo, Thing victim)
        {
            Pawn instigator = dinfo.Instigator as Pawn;
            Pawn target = victim as Pawn;
            Map map = victim.Map;
            DamageResult result = new DamageResult();
            if (JecsTools.GrappleUtility.TryGrapple(grappler: instigator, victim: target))
            {
                JecsTools.GrappleUtility.ApplyGrappleEffect(grappler: instigator, victim: target, ticks: 300);
                return base.Apply(dinfo: dinfo, thing: victim);
            }

            return result;
        }
    }
}