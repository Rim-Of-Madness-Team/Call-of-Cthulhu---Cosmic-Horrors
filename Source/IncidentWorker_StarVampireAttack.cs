///Always required
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
///Possible libraries
using RimWorld;
using Verse;
using Verse.Sound;
using Verse.AI.Group;
//using RimWorld.Planet;
//using Verse.AI;
//using Verse.AI.Group;


namespace CosmicHorror
{
    public class IncidentWorker_StarVampireAttack : IncidentWorker
    {
        private CosmicHorrorPawn iwVampire;   //The Star Vampire Pawn
        private PawnKindDef iwKind;           //For the PawnType
        private Pawn iwPawn;                  //For the PawnGenerator
        private Faction iwFac;                //The Star Vampire Faction
        private IntVec3 iwLoc;                //The Star Vampire location
        private SoundDef iwWarn;              //The Star Vampire Warning Noise
        private Lord lord;                    //The Star Vampire AI manager

        protected override bool CanFireNowSub(IIncidentTarget target)
        {
            if (GenDate.DaysPassed < (HugsModOptionalCode.cosmicHorrorEventsDelay() + this.def.earliestDay))
            {
                Log.Message("Cosmic Horrors :: CantFireDueTo Time :: " + GenDate.DaysPassed + " days passed, but we need " + HugsModOptionalCode.cosmicHorrorEventsDelay().ToString() + " days + " + this.def.earliestDay);
                return false;
            }
            return true;
        }

        public override bool TryExecute(IncidentParms parms)
        {

            //Resolve parameters.
            ResolveSpawnCenter(parms);
            
            //Initialize variables.
            iwKind      = PawnKindDef.Named("CosmicHorror_StarVampire");
            iwFac       = Find.FactionManager.FirstFactionOfDef(FactionDef.Named("StarVampire"));
            iwWarn      = SoundDef.Named("Pawn_CosmicHorror_StarVampire_Warning");
            iwPawn      = null; //PawnGenerator.GeneratePawn(iwKind, iwFac);
            iwVampire   = null; //iwPawn as CosmicHorrorPawn;
            iwLoc       = CellFinder.RandomClosewalkCellNear(parms.spawnCenter, (Map)parms.target, 8);

            //In-case there's something silly happening...
            if (iwFac == null)
            {
                iwFac = Find.FactionManager.FirstFactionOfDef(FactionDefOf.SpacerHostile);
            }

            //Slow down time
            Find.TickManager.slower.SignalForceNormalSpeed();
            //Play a sound.
            iwWarn.PlayOneShotOnCamera();
            //Show the warning message.
            Messages.Message("StarVampireIncidentMessage".Translate(), new RimWorld.Planet.GlobalTargetInfo(IntVec3.Invalid, (Map)parms.target), MessageSound.Standard);
            //Spawn the Star Vampire.
            SpawnStarVampires(parms);
            return true;
        }

        /// <summary>
        /// Where to, Cthulhu?
        /// </summary>
        /// <param name="parms"></param>
        protected void ResolveSpawnCenter(IncidentParms parms)
        {
            Map iwMap = (Map)parms.target;
            if (parms.spawnCenter.IsValid)
            {
                return;
            }
            if (Rand.Value < 0.4f && iwMap.listerBuildings.ColonistsHaveBuildingWithPowerOn(ThingDefOf.OrbitalTradeBeacon))
            {
                parms.spawnCenter = DropCellFinder.TradeDropSpot(iwMap);
            }
            else
            {
                RCellFinder.TryFindRandomPawnEntryCell(out parms.spawnCenter, iwMap);
            }
        }

        /// <summary>
        /// Finds the best number of Star Vampires
        /// to spawn. Then spawns them according
        /// to a previously resolved location.
        /// </summary>
        /// <param name="parms"></param>
        protected void SpawnStarVampires(IncidentParms parms)
        {
            Map iwMap = (Map)parms.target;

            int iwCount = 1;

            if (parms.points <= 2000f)
            {
                iwCount = 1;
            }
            else if (parms.points <= 3000f)
            {
                iwCount = 2;
            }
            else if (parms.points <= 5000f)
            {
                iwCount = 3;
            }

            for (int i = 0; i < iwCount; i++)
            {
                CosmicHorrorPawn temp = null;
                iwPawn = null;
                iwVampire = null;
                iwPawn = PawnGenerator.GeneratePawn(iwKind, iwFac);
                iwVampire = iwPawn as CosmicHorrorPawn;

                if (this.lord == null)
                {
                    LordJob_StarVampire lordJob = new LordJob_StarVampire(iwFac, iwLoc);
                    this.lord = LordMaker.MakeNewLord(iwFac, lordJob, iwMap, null);
                }
                temp = (CosmicHorrorPawn)GenSpawn.Spawn(iwVampire, iwLoc, iwMap);
                this.lord.AddPawn(temp);
            }
        }
    }
}
