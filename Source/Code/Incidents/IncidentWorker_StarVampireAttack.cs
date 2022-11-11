using RimWorld;
using System;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace CosmicHorror
{
    public class IncidentWorker_StarVampireAttack : IncidentWorker
    {
        protected override bool CanFireNowSub(IncidentParms parms)
        {
            if (!base.CanFireNowSub(parms: parms))
            {
                return false;
            }

            Map map = (Map)parms.target;
            IntVec3 intVec;
            return RCellFinder.TryFindRandomPawnEntryCell(result: out intVec, map: map,
                roadChance: CellFinder.EdgeRoadChance_Animal, allowFogged: false, extraValidator: null);
        }

        private int NumberToSpawn(IncidentParms parms)
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
            else
            {
                iwCount = 3;
            }

            return iwCount;
        }

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            Map map = (Map)parms.target;
            IntVec3 intVec;
            if (!RCellFinder.TryFindRandomPawnEntryCell(result: out intVec, map: map,
                    roadChance: CellFinder.EdgeRoadChance_Animal, allowFogged: false, extraValidator: null))
            {
                return false;
            }

            map.GetComponent<MapComponent_StarVampireTracker>()
                .AddNewStarVampireSpawner(spawnLoc: intVec, num: NumberToSpawn(parms: parms));

            //Slow down time
            Find.TickManager.slower.SignalForceNormalSpeed();
            //Play a sound.
            MonsterDefOf.Pawn_ROM_StarVampire_Warning.PlayOneShotOnCamera();
            //Show the warning message.
            Messages.Message(text: "StarVampireIncidentMessage".Translate(),
                lookTargets: new RimWorld.Planet.GlobalTargetInfo(cell: IntVec3.Invalid, map: (Map)parms.target),
                def: MessageTypeDefOf.SituationResolved);

            return true;
        }
    }
}