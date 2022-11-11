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
                SoundDef warnSound = SoundDef.Named(defName: "Pawn_ROM_StarVampire_Warning");
                warnSound.PlayOneShotOnCamera();
                Messages.Message(text: "StarVampireIncidentMessage2".Translate(),
                    lookTargets: new RimWorld.Planet.GlobalTargetInfo(cell: IntVec3.Invalid, map: Map),
                    def: MessageTypeDefOf.ThreatBig);
                HealthUtility.AdjustSeverity(pawn: this as Pawn,
                    hdDef: HediffDef.Named(defName: "ROM_StarVampireDescent"), sevOffset: 1.0f);
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look<bool>(value: ref alertedPlayer, label: "alertedPlayer", defaultValue: false);
        }
    }
}