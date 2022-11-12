// RimWorld.IncidentWorker_RaidEnemy

using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace CosmicHorror

{
    /// <summary>
    /// A shortened version of RimWorld's raid code that forces
    /// at least one cosmic horror pawn in any raid
    ///
    /// Cosmic Horrors raids:
    /// - Force dark and dreary weather
    /// - Always have at least one monster
    /// </summary>
    public class IncidentWorker_RaidCosmicHorrors : IncidentWorker
    {
        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            List<Pawn> list;
            Map map = (Map)parms.target;

            ResolveRaid(map: map, parms: parms, list: out list);
            ResolveDarkAndDrearyWeather(map: map);

            SendStandardLetter(baseLetterLabel: "CosmicHorrorRaid".Translate(),
                baseLetterText: "CosmicHorrorRaidIncidentDesc".Translate(), baseLetterDef: LetterDefOf.ThreatBig,
                parms: parms, lookTargets: list, textArgs: Array.Empty<NamedArgument>());
            parms.raidStrategy.Worker.MakeLords(parms: parms, pawns: list);

            return true;
        }

        private static void ResolveRaid(Map map, IncidentParms parms, out List<Pawn> list)
        {
            //Always have at least 100 points...
            if (parms.points <= 100) parms.points = 100f;
            parms.faction = Utility.ResolveHorrorFactionByIncidentPoints(points: parms.points);
            parms.raidStrategy = RaidStrategyDefOf.ImmediateAttack;
            parms.raidArrivalMode = PawnsArrivalModeDefOf.EdgeWalkIn;
            parms.raidStrategy.Worker.TryGenerateThreats(parms: parms);
            RCellFinder.TryFindRandomPawnEntryCell(result: out parms.spawnCenter,
                map: map,
                roadChance: CellFinder.EdgeRoadChance_Neutral,
                allowFogged: false,
                extraValidator: null);
            parms.points = IncidentWorker_Raid.AdjustedRaidPoints(
                points: parms.points
                ,raidArrivalMode: parms.raidArrivalMode
                ,raidStrategy: parms.raidStrategy
                ,faction: parms.faction
                ,groupKind: PawnGroupKindDefOf.Combat
                //,ageRestriction: null //1.4 only
                );
            list = parms.raidStrategy.Worker.SpawnThreats(parms: parms);
            if (list == null)
            {
                PawnGroupMakerParms defaultPawnGroupMakerParms =
                    IncidentParmsUtility.GetDefaultPawnGroupMakerParms(groupKind: PawnGroupKindDefOf.Combat,
                        parms: parms, ensureCanGenerateAtLeastOnePawn: true); //Always spawn at least 1 pawn
                list = PawnGroupMakerUtility.GeneratePawns(parms: defaultPawnGroupMakerParms, warnOnZeroResults: true)
                    .ToList<Pawn>();
                parms.raidArrivalMode.Worker.Arrive(pawns: list, parms: parms);
            }

            parms.target.StoryState.lastRaidFaction = parms.faction;
        }

        private readonly List<string> darkOrDrearyWeather = new List<string>
        {
            "Fog", "Rain", "RainyThunderstorm", "FoggyRain"
            //,"SnowHard"
        };

        private bool IsDarkOrDrearyWeather(Map map)
        {
            return darkOrDrearyWeather.Contains(item: map.weatherManager.curWeather.defName);
        }

        private void ResolveDarkAndDrearyWeather(Map map)
        {
            if (!IsDarkOrDrearyWeather(map: map))
            {
                map.weatherManager.TransitionTo(
                    newWeather: WeatherDef.Named(defName: darkOrDrearyWeather.RandomElement()));
            }
        }
    }
}