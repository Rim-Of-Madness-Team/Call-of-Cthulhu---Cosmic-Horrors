using RimWorld;
using System;
using System.Collections.Generic;
using Verse;

namespace CosmicHorror
{
    public class DamageWorker_Constrict : DamageWorker_AddInjury
    {
        public override DamageWorker.DamageResult Apply(DamageInfo dinfo, Thing victim)
        {
            Pawn instigator = dinfo.Instigator as Pawn;
            Pawn target = victim as Pawn;
            Map map = victim.Map;
            DamageWorker.DamageResult result = new DamageWorker.DamageResult();
            if (JecsTools.GrappleUtility.TryGrapple(instigator, target))
            {
                JecsTools.GrappleUtility.ApplyGrappleEffect(instigator, target, 300);
                return base.Apply(dinfo, victim);

            }
            return result;
        }
    }
}
