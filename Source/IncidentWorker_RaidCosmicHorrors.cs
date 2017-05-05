using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using RimWorld;
using System.Text;
using Verse.AI.Group;
using Verse.AI;

namespace CosmicHorror
{
    public class CosmicHorrorFaction
    {
        public string defName { get; set; }
        public float weight { get; set; }


        public CosmicHorrorFaction(String newName, float newWeight)
        {
            defName = newName;
            weight = newWeight;
        }
    }


    public class IncidentWorker_RaidCosmicHorrors : IncidentWorker_Raid
    {
        public FactionDef attackingFaction = null;

        public bool IsCosmicHorrorFaction(Faction f)
        {
            List<string> factions = new List<string> {
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

        public FactionDef ResolveHorrorFaction(float points)
        {
            List<CosmicHorrorFaction> factionList = new List<CosmicHorrorFaction>
            {
                new CosmicHorrorFaction("ROM_DeepOne", 4)
            };
            if (points > 1400f) factionList.Add(new CosmicHorrorFaction("ROM_StarSpawn", 1));
            if (points > 700f) factionList.Add(new CosmicHorrorFaction("ROM_Shoggoth", 2));
            if (points > 350f) factionList.Add(new CosmicHorrorFaction("ROM_MiGo", 4));
            CosmicHorrorFaction f = GenCollection.RandomElementByWeight<CosmicHorrorFaction>(factionList, GetWeight);
            Faction resolvedFaction = Find.FactionManager.FirstFactionOfDef(FactionDef.Named(f.defName));

            //This is a special case.
            //If the player has the Cults mod.
            //If they are working with Dagon.
            //Then let's do something different...
            if (Cthulhu.Utility.IsCultsLoaded() && f.defName == "ROM_DeepOne")
            {
                if (resolvedFaction.RelationWith(Faction.OfPlayer, false).hostile == false)
                {
                    //Do MiGo instead.
                    Cthulhu.Utility.DebugReport("Cosmic Horror Raid Report: Special Cult Case Handled");
                    resolvedFaction = Find.FactionManager.FirstFactionOfDef(FactionDef.Named("ROM_MiGo"));
                }
            }
            attackingFaction = resolvedFaction.def;
            Cthulhu.Utility.DebugReport("Cosmic Horror Raid Report: " + attackingFaction.ToString() + "selected");
            return FactionDef.Named(attackingFaction.defName);
        }

        public static float GetWeight(CosmicHorrorFaction f)
        {
            return f.weight;
        }

        private enum raidTypes
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

        protected override string GetRelatedPawnsInfoLetterText(IncidentParms parms)
        {
            return Translator.Translate(attackingFaction.LabelCap + "" + "CosmicHorrorRaid".Translate(), new object[]
            {
                parms.faction.def.pawnsPlural
            });
        }

        protected override bool CanFireNowSub(IIncidentTarget target)
        {
            Map map = (Map)target;
            Cthulhu.Utility.DebugReport("Cosmic Horror Raid Report: Tried to start event.");
            if (!base.CanFireNowSub(target))
            {
                Cthulhu.Utility.DebugReport("Cosmic Horror Raid Report: Failed due to base CanFireNow process");
                return false;
            }

            if (GenDate.DaysPassed < ( ModInfo.cosmicHorrorRaidDelay + this.def.earliestDay))
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

        protected override string GetLetterLabel(IncidentParms parms)
        {
            return parms.raidStrategy.letterLabelEnemy;
        }

        protected override LetterDef GetLetterDef()
        {
            return LetterDefOf.BadUrgent;
        }

        protected override void ResolveRaidStrategy(IncidentParms parms)
        {
            if (parms.raidStrategy != null)
            {
                return;
            }
            parms.raidStrategy = RaidStrategyDefOf.ImmediateAttack;
        }

        protected override void ResolveRaidPoints(IncidentParms parms)
        {
            //parms.points = StorytellerCthulhu.Utility.DefaultParmsNow(Find.Storyteller.def, RimWorld.IncidentCategory.ThreatBig).points;
            if (parms.points > 0f)
            {
                return;
            }
            parms.points = (float)Rand.Range(70, 350);
        }

        protected override bool TryResolveRaidFaction(IncidentParms parms)
        {
            parms.faction = Find.FactionManager.FirstFactionOfDef(ResolveHorrorFaction(parms.points));
            return parms.faction != null;
        }

        public override bool TryExecute(IncidentParms parms)
        {
            Map map = (Map)parms.target;

            Cthulhu.Utility.DebugReport("Cosmic Horror Raid Report: Trying execution");
            this.ResolveRaidPoints(parms);
            if (!this.TryResolveRaidFaction(parms))
            {
                Cthulhu.Utility.DebugReport("Cosmic Horror Raid Report: Failed to resolve faction");
                return false;
            }
            this.ResolveRaidStrategy(parms);
            this.ResolveRaidArriveMode(parms);
            this.ResolveRaidSpawnCenter(parms);
            IncidentParmsUtility.AdjustPointsForGroupArrivalParams(parms);
            PawnGroupMakerParms defaultPawnGroupMakerParms = IncidentParmsUtility.GetDefaultPawnGroupMakerParms(parms);

            List<Pawn> list = PawnGroupMakerUtility.GeneratePawns(PawnGroupKindDefOf.Normal, defaultPawnGroupMakerParms).ToList<Pawn>();
            if (list.Count == 0)
            {
                Cthulhu.Utility.ErrorReport("Got no pawns spawning raid from parms " + parms);
                return false;
            }
            TargetInfo target = TargetInfo.Invalid;
            if (parms.raidArrivalMode == PawnsArriveMode.CenterDrop || parms.raidArrivalMode == PawnsArriveMode.EdgeDrop)
            {
                DropPodUtility.DropThingsNear(parms.spawnCenter, map, list.Cast<Thing>(), parms.raidPodOpenDelay, false, true, true);
                target = new TargetInfo(parms.spawnCenter, map);
            }
            else
            {
                foreach (Pawn arg_B3_0 in list)
                {
                    IntVec3 intVec = CellFinder.RandomClosewalkCellNear(parms.spawnCenter, map, 8);
                    GenSpawn.Spawn(arg_B3_0, intVec, map);
                    target = arg_B3_0;
                }
            }
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.AppendLine("Points = " + parms.points.ToString("F0"));
            foreach (Pawn current2 in list)
            {
                string str = (current2.equipment == null || current2.equipment.Primary == null) ? "unarmed" : current2.equipment.Primary.LabelCap;
                stringBuilder.AppendLine(current2.KindLabel + " - " + str);
            }
            string letterLabel = this.GetLetterLabel(parms);
            string letterText = this.GetLetterText(parms, list);
            PawnRelationUtility.Notify_PawnsSeenByPlayer(list, ref letterLabel, ref letterText, this.GetRelatedPawnsInfoLetterText(parms), true);
            Find.LetterStack.ReceiveLetter(letterLabel, letterText, this.GetLetterDef(), target, stringBuilder.ToString());
            if (this.GetLetterDef() == LetterDefOf.BadUrgent)
            {
                TaleRecorder.RecordTale(TaleDefOf.RaidArrived, new object[0]);
            }
            Lord lord = LordMaker.MakeNewLord(parms.faction, parms.raidStrategy.Worker.MakeLordJob(parms, map), map, list);
            AvoidGridMaker.RegenerateAvoidGridsFor(parms.faction, map);
            LessonAutoActivator.TeachOpportunity(ConceptDefOf.EquippingWeapons, OpportunityType.Critical);
            //if (!PlayerKnowledgeDatabase.IsComplete(ConceptDefOf.PersonalShields))
            //{
            //    for (int i = 0; i < list.Count; i++)
            //    {
            //        Pawn pawn = list[i];
            //        if (pawn.apparel.WornApparel.Any((Apparel ap) => ap is PersonalShield))
            //        {
            //            LessonAutoActivator.TeachOpportunity(ConceptDefOf.PersonalShields, OpportunityType.Critical);
            //            break;
            //        }
            //    }
            //}
            if (DebugViewSettings.drawStealDebug && parms.faction.HostileTo(Faction.OfPlayer))
            {
                Log.Message(string.Concat(new object[]
                {
                        "Market value threshold to start stealing: ",
                        StealAIUtility.StartStealingMarketValueThreshold(lord),
                        " (colony wealth = ",
                        map.wealthWatcher.WealthTotal,
                        ")"
                }));
            }
            return true;
        }

        protected override void ResolveRaidArriveMode(IncidentParms parms)
        {
            parms.raidArrivalMode = PawnsArriveMode.EdgeWalkIn;
        }
    }
}
