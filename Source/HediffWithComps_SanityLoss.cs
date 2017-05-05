using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;

namespace CosmicHorror
{
    class HediffWithComps_SanityLoss : HediffWithComps
    {
        public override void PostTick()
        {
            base.PostTick();

            if (pawn != null)
            {
                if (pawn.RaceProps != null)
                {
                    if (pawn.RaceProps.IsMechanoid ||
                        base.pawn is CosmicHorrorPawn)
                    {
                        MakeSane();
                    }
                }
            }
        }

        public void MakeSane()
        {
            this.Severity -= 1f;
            base.pawn.health.Notify_HediffChanged(this);
        }
    }
}
