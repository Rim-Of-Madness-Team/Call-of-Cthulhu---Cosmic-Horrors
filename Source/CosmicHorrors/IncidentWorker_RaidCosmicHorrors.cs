using System.Collections.Generic;
using System.Linq;
using Verse;
using RimWorld;
using System.Text;
using UnityEngine;
using Verse.AI.Group;
using Verse.AI;

namespace CosmicHorror
{
    public class IncidentWorker_RaidCosmicHorrors : IncidentWorker_Raid
    {
        public FactionDef attackingFaction = null;

        public bool IsCosmicHorrorFaction(Faction f)
        {
            List<string> factions = new List<string>
            {
                "ROM_StarSpawn",
                "ROM_Shoggoth",
                "ROM_MiGo",
                "ROM_DeepOne",
            };
            foreach (string s in factions)
            {
                if (f.def.defName.Contains(s))
                {
                    return true;
                }
            }
            return false;
        }


        private enum RaidTypes
        {
            One = 175,
            Two = 350,
            Three = 700,
            Four = 1400,
            AllHorrors
        }

        private const int PV_DeepOne = 70;
        private const int PV_MiGoDrone = 105;
        private const int PV_MiGoCaster = 315;
        private const int PV_Shoggoth = 525;
        private const int PV_StarSpawn = 700;

        protected override string GetLetterText(IncidentParms parms, List<Pawn> list)
        {
            string text = "CosmicHorrorRaidIncidentDesc".Translate();
            return text;
        }

        protected override string GetRelatedPawnsInfoLetterText(IncidentParms parms) =>
            "LetterRelatedPawnsRaidEnemy".Translate(new object[]
            {
                Faction.OfPlayer.def.pawnsPlural,
                parms.faction.def.pawnsPlural
            });

        protected override bool CanFireNowSub(IncidentParms parms)
        {
            Map map = (Map) parms.target;
            Cthulhu.Utility.DebugReport("Cosmic Horror Raid Report: Tried to start event.");
            if (!base.CanFireNowSub(parms))
            {
                Cthulhu.Utility.DebugReport("Cosmic Horror Raid Report: Failed due to base CanFireNow process");
                return false;
            }

            if (GenDate.DaysPassed < (ModInfo.cosmicHorrorRaidDelay + this.def.earliestDay))
            {
                return false;
            }

            if (map.gameConditionManager.ConditionIsActive(GameConditionDefOf.Eclipse))
            {
                Cthulhu.Utility.DebugReport("Cosmic Horror Raid Report: Firing accepted - Eclipse");
                return true;
            }

            WeatherDef weather = map.weatherManager.curWeather;
            if (weather == WeatherDef.Named("Fog") ||
                weather == WeatherDef.Named("RainyThunderstorm") ||
                weather == WeatherDef.Named("FoggyRain"))
            {
                Cthulhu.Utility.DebugReport("Cosmic Horror Raid Report: Firing accepted - " + weather.label);
                return true;
            }

            if (!(GenLocalDate.HourInteger(map) >= 6 && GenLocalDate.HourInteger(map) <= 17))
            {
                Cthulhu.Utility.DebugReport("Cosmic Horror Raid Report: Firing accepted - Nighttime");
                return true;
            }
            return false;
        }

        protected override string GetLetterLabel(IncidentParms parms) => parms.raidStrategy.letterLabelEnemy;

        protected override LetterDef GetLetterDef() => LetterDefOf.ThreatBig;
 
        public override void ResolveRaidStrategy(IncidentParms parms, PawnGroupKindDef groupKind)
        {
            if (parms.raidStrategy == null)
            {
                Map map = (Map) parms.target;
                if (!(from d in DefDatabase<RaidStrategyDef>.AllDefs
                    where d.Worker.CanUseWith(parms, groupKind) &&
                          (parms.raidArrivalMode != null ||
                           (d.arriveModes != null &&
                            d.arriveModes.Any((PawnsArrivalModeDef x) => x.Worker.CanUseWith(parms))))
                    select d).TryRandomElementByWeight(
                    (RaidStrategyDef d) => d.Worker.SelectionWeight(map, parms.points), out parms.raidStrategy))
                {
                    Log.Error(string.Concat(new object[]
                    {
                        "No raid stategy for ",
                        parms.faction,
                        " with points ",
                        parms.points,
                        ", groupKind=",
                        groupKind,
                        "\nparms=",
                        parms
                    }), false);
                    if (!Prefs.DevMode)
                    {
                        parms.raidStrategy = RaidStrategyDefOf.ImmediateAttack;
                    }
                }
            }
        }

        protected override void ResolveRaidPoints(IncidentParms parms)
        {
            parms.points = StorytellerUtility.DefaultThreatPointsNow(parms.target);
        }

        protected override bool TryResolveRaidFaction(IncidentParms parms)
        {
            parms.faction = Find.World.GetComponent<FactionTracker>()
                .ResolveHorrorFactionByIncidentPoints(parms.points);
            return parms.faction != null;
        }

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            Map map = (Map) parms.target;

            Cthulhu.Utility.DebugReport("Cosmic Horror Raid Report: Trying execution");
            this.ResolveRaidPoints(parms);
            bool result = false;
            if (!this.TryResolveRaidFaction(parms))
            {
                Cthulhu.Utility.DebugReport("Cosmic Horror Raid Report: Failed to resolve faction");
                result = false;
            }
            else
            {
                PawnGroupKindDef combat = PawnGroupKindDefOf.Combat;
                this.ResolveRaidStrategy(parms, PawnGroupKindDefOf.Combat);
                this.ResolveRaidArriveMode(parms);
                if (!parms.raidArrivalMode.Worker.TryResolveRaidSpawnCenter(parms))
                {
                    result = false;
                }
                else
                {
                    parms.points = IncidentWorker_Raid.AdjustedRaidPoints(parms.points, parms.raidArrivalMode,
                        parms.raidStrategy, parms.faction, combat);

                    PawnGroupMakerParms defaultPawnGroupMakerParms =
                        IncidentParmsUtility.GetDefaultPawnGroupMakerParms(combat, parms);

                    List<Pawn> list = PawnGroupMakerUtility
                        .GeneratePawns(defaultPawnGroupMakerParms, true).ToList<Pawn>();
                    if (list.Count == 0)
                    {
                        Cthulhu.Utility.ErrorReport("Got no pawns spawning raid from parms " + parms);
                        result = false;
                    }
                    parms.raidArrivalMode.Worker.Arrive(list, parms);

                    StringBuilder stringBuilder = new StringBuilder();
                    stringBuilder.AppendLine("Points = " + parms.points.ToString("F0"));
                    foreach (Pawn current2 in list)
                    {
                        string str = (current2.equipment == null || current2.equipment.Primary == null)
                            ? "unarmed"
                            : current2.equipment.Primary.LabelCap;
                        stringBuilder.AppendLine(current2.KindLabel + " - " + str);
                    }
                    string letterLabel = this.GetLetterLabel(parms);
                    string letterText = this.GetLetterText(parms, list);
                    TaggedString letterLabelS = letterLabel;
                    TaggedString letterTextS = letterText;
                    //string lalalal = this.GetRelatedPawnsInfoLetterText(parms);
                    PawnRelationUtility.Notify_PawnsSeenByPlayer_Letter(list, ref letterLabelS, ref letterTextS,
                        this.GetRelatedPawnsInfoLetterText(parms), true, true);
                    List<TargetInfo> list2 = new List<TargetInfo>();
                    if (parms.pawnGroups != null)
                    {
                        List<List<Pawn>> list3 = IncidentParmsUtility.SplitIntoGroups(list, parms.pawnGroups);
                        List<Pawn> list4 = list3.MaxBy((List<Pawn> x) => x.Count);
                        if (list4.Any<Pawn>())
                        {
                            list2.Add(list4[0]);
                        }
                        for (int i = 0; i < list3.Count; i++)
                        {
                            if (list3[i] != list4)
                            {
                                if (list3[i].Any<Pawn>())
                                {
                                    list2.Add(list3[i][0]);
                                }
                            }
                        }
                    }
                    else if (list.Any<Pawn>())
                    {
                        list2.Add(list[0]);
                    }
                    Find.LetterStack.ReceiveLetter(letterLabel, letterText, this.GetLetterDef(), list2, parms.faction);
                    if (this.GetLetterDef() == LetterDefOf.ThreatBig)
                    {
                        //TaleRecorder.RecordTale(TaleDefOf. RaidArrived, new object[0]);
                    }
                    parms.raidStrategy.Worker.MakeLords(parms, list);
                    LessonAutoActivator.TeachOpportunity(ConceptDefOf.EquippingWeapons, OpportunityType.Critical);
                    if (!PlayerKnowledgeDatabase.IsComplete(ConceptDefOf.ShieldBelts))
                    {
                        for (int j = 0; j < list.Count; j++)
                        {
                            Pawn pawn2 = list[j];
                            if (pawn2.apparel.WornApparel.Any((Apparel ap) => ap is ShieldBelt))
                            {
                                LessonAutoActivator.TeachOpportunity(ConceptDefOf.ShieldBelts,
                                    OpportunityType.Critical);
                                break;
                            }
                        }
                    }
                    result = true;
                }
            }
            return result;
        }

        public override void ResolveRaidArriveMode(IncidentParms parms) =>
            parms.raidArrivalMode = PawnsArrivalModeDefOf.EdgeWalkIn;
    }
}