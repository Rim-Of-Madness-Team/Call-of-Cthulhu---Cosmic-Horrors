using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using Verse.Sound;

namespace CosmicHorror
{
    public class CosmicHorrorPawn_StarVampire : CosmicHorrorPawn
    {
        public bool alertedPlayer = false;

        public override void Tick()
        {
            base.Tick();
            AlertPlayer();
        }

        public void AlertPlayer()
        {
            if (!alertedPlayer)
            {
                alertedPlayer = true;
                SoundDef warnSound = SoundDef.Named("Pawn_ROM_StarVampire_Warning");
                warnSound.PlayOneShotOnCamera();
                Messages.Message("StarVampireIncidentMessage2".Translate(), new RimWorld.Planet.GlobalTargetInfo(IntVec3.Invalid, Map), MessageTypeDefOf.ThreatBig);
                HealthUtility.AdjustSeverity(this as Pawn, HediffDef.Named("ROM_StarVampireDescent"), 1.0f);
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look<bool>(ref this.alertedPlayer, "alertedPlayer", false);
        }
    }
}
