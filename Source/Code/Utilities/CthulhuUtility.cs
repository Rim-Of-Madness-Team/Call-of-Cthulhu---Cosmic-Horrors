// ----------------------------------------------------------------------
// These are basic usings. Always let them be here.
// ----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

// ----------------------------------------------------------------------
// These are RimWorld-specific usings. Activate/Deactivate what you need:
// ----------------------------------------------------------------------
using UnityEngine; // Always needed
//using VerseBase;         // Material/Graphics handling functions are found here
using Verse; // RimWorld universal objects are here (like 'Building')
using Verse.AI; // Needed when you do something with the AI
using RimWorld; // RimWorld specific functions are found here (like 'Building_Battery')
using RimWorld.Planet; // RimWorld specific functions for world creation
using System.Reflection;
using CosmicHorror;

//using RimWorld.SquadAI;  // RimWorld specific functions for squad brains 

/// <summary>
/// Utility File for use between Cthulhu mods.
/// Last Update: 5/5/2017
/// </summary>
namespace CosmicHorror
{
    public static class ModProps
    {
        public const string main = "Cthulhu";
        public const string mod = "Horrors";
        public const string version = "1.5.15";
    }

    public static class SanityLossSeverity
    {
        public const float Initial = 0.1f;
        public const float Minor = 0.25f;
        public const float Major = 0.5f;
        public const float Severe = 0.7f;
        public const float Extreme = 0.95f;
    }

    static public class Utility
    {
        public enum SanLossSev
        {
            None = 0,
            Hidden,
            Initial,
            Minor,
            Major,
            Extreme
        };

        public const string SanityLossDef = "ROM_SanityLoss";
        public const string AltSanityLossDef = "Cults_SanityLoss";

        public static bool modCheck = false;
        public static bool loadedCosmicHorrors = false;
        public static bool loadedIndustrialAge = false;
        public static bool loadedCults = false;
        public static bool loadedFactions = false;


        public static bool IsMorning(Map map) =>
            GenLocalDate.HourInteger(map: map) > 6 && GenLocalDate.HourInteger(map: map) < 10;

        public static bool IsEvening(Map map) =>
            GenLocalDate.HourInteger(map: map) > 18 && GenLocalDate.HourInteger(map: map) < 22;

        public static bool IsNight(Map map) => GenLocalDate.HourInteger(map: map) > 22;

        public static T GetMod<T>(string s) where T : Mod
        {
            //Call of Cthulhu - Cosmic Horrors
            T result = default(T);
            foreach (Mod ResolvedMod in LoadedModManager.ModHandles)
            {
                if (ResolvedMod.Content.Name == s) result = ResolvedMod as T;
            }

            return result;
        }


        public static Faction ResolveHorrorFactionByIncidentPoints(float points)
        {
            List<CosmicHorrorWeight> factionList = new List<CosmicHorrorWeight>
            {
                new CosmicHorrorWeight(newName: "ROM_DeepOne", newWeight: 4)
            };
            if (points > 1400f) factionList.Add(item: new CosmicHorrorWeight(newName: "ROM_StarSpawn", newWeight: 1));
            if (points > 700f) factionList.Add(item: new CosmicHorrorWeight(newName: "ROM_Shoggoth", newWeight: 2));
            if (points > 350f) factionList.Add(item: new CosmicHorrorWeight(newName: "ROM_MiGo", newWeight: 4));
            CosmicHorrorWeight f =
                factionList.RandomElementByWeight<CosmicHorrorWeight>(weightSelector: fac => fac.Weight);
            Faction resolvedFaction =
                Find.FactionManager.FirstFactionOfDef(facDef: FactionDef.Named(defName: f.DefName));

            //This is a special case.
            //If the player has the Cults mod.
            //If they are working with Dagon.
            //Then let's do something different...
            if (IsCultsLoaded() && f.DefName == "ROM_DeepOne")
            {
                if (resolvedFaction.RelationWith(other: Faction.OfPlayer, allowNull: false).kind !=
                    FactionRelationKind.Hostile)
                {
                    //Do MiGo instead.
                    DebugReport(x: "Cosmic Horror Raid Report: Special Cult Case Handled");
                    resolvedFaction =
                        Find.FactionManager.FirstFactionOfDef(facDef: FactionDef.Named(defName: "ROM_MiGo"));
                }
            }

            //this.attackingFaction = resolvedFaction.def;
            DebugReport(x: "Cosmic Horror Raid Report: " + resolvedFaction.def.ToString() + "selected");
            return resolvedFaction;
        }

        public static IEnumerable<CosmicHorrorPawn> SpawnHorrorsOfCountAt(PawnKindDef kindDef, IntVec3 at, Map map,
            int count, Faction fac = null, bool berserk = false, bool target = false)
        {
            List<CosmicHorrorPawn> pawns = new List<CosmicHorrorPawn>();
            for (int i = 1; i <= count; i++)
            {
                if ((from cell in GenAdj.CellsAdjacent8Way(pack: new TargetInfo(cell: at, map: map))
                        where at.Walkable(map: map) && !at.Fogged(map: map) && at.InBounds(map: map)
                        select cell).TryRandomElement(result: out at))
                {
                    CosmicHorrorPawn pawn = null;
                    if (fac != Faction.OfPlayer)
                    {
                        PawnGenerationRequest request = new PawnGenerationRequest(kind: kindDef, faction: fac,
                            context: PawnGenerationContext.NonPlayer, tile: map.Tile);
                        pawn = (CosmicHorrorPawn)GenSpawn.Spawn(newThing: PawnGenerator.GeneratePawn(request: request),
                            loc: at, map: map);
                    }
                    else
                    {
                        pawn = (CosmicHorrorPawn)GenSpawn.Spawn(
                            newThing: PawnGenerator.GeneratePawn(kindDef: kindDef, faction: fac), loc: at, map: map);
                    }

                    pawns.Add(item: pawn);
                    if (berserk)
                        pawn.mindState.mentalStateHandler.TryStartMentalState(stateDef: MentalStateDefOf.Berserk);
                }
            }

            return pawns.AsEnumerable<CosmicHorrorPawn>();
        }


        public static bool IsTameable(PawnKindDef kindDef)
        {
            if (kindDef.defName == "ROM_ChthonianLarva" ||
                kindDef.defName == "ROM_DarkYoung")
                return true;
            return false;
        }

        public static bool IsCosmicHorror(Pawn thing)
        {
            if (!IsCosmicHorrorsLoaded()) return false;

            Type type = Type.GetType(typeName: "CosmicHorror.CosmicHorrorPawn");
            if (type != null)
            {
                if (thing.GetType() == type)
                {
                    return true;
                }
            }

            return false;
        }

        //public static float GetSanityLossRate(PawnKindDef kindDef)
        //{
        //    float sanityLossRate = 0f;
        //    if (kindDef.ToString() == "ROM_StarVampire")
        //        sanityLossRate = 0.04f;
        //    if (kindDef.ToString() == "StarSpawnOfCthulhu")
        //        sanityLossRate = 0.02f;
        //    if (kindDef.ToString() == "DarkYoung")
        //        sanityLossRate = 0.004f;
        //    if (kindDef.ToString() == "DeepOne")
        //        sanityLossRate = 0.008f;
        //    if (kindDef.ToString() == "DeepOneGreat")
        //        sanityLossRate = 0.012f;
        //    if (kindDef.ToString() == "MiGo")
        //        sanityLossRate = 0.008f;
        //    if (kindDef.ToString() == "Shoggoth")
        //        sanityLossRate = 0.012f;
        //    return sanityLossRate;
        //}

        public static bool IsActorAvailable(Pawn preacher, bool downedAllowed = false)
        {
            StringBuilder s = new StringBuilder();
            s.Append(value: "ActorAvailble Checks Initiated");
            s.AppendLine();
            if (preacher == null)
                return ResultFalseWithReport(s: s);
            s.Append(value: "ActorAvailble: Passed null Check");
            s.AppendLine();
            //if (!preacher.Spawned)
            //    return ResultFalseWithReport(s);
            //s.Append("ActorAvailble: Passed not-spawned check");
            //s.AppendLine();
            if (preacher.Dead)
                return ResultFalseWithReport(s: s);
            s.Append(value: "ActorAvailble: Passed not-dead");
            s.AppendLine();
            if (preacher.Downed && !downedAllowed)
                return ResultFalseWithReport(s: s);
            s.Append(value: "ActorAvailble: Passed downed check & downedAllowed = " + downedAllowed.ToString());
            s.AppendLine();
            if (preacher.Drafted)
                return ResultFalseWithReport(s: s);
            s.Append(value: "ActorAvailble: Passed drafted check");
            s.AppendLine();
            if (preacher.InAggroMentalState)
                return ResultFalseWithReport(s: s);
            s.Append(value: "ActorAvailble: Passed drafted check");
            s.AppendLine();
            if (preacher.InMentalState)
                return ResultFalseWithReport(s: s);
            s.Append(value: "ActorAvailble: Passed InMentalState check");
            s.AppendLine();
            s.Append(value: "ActorAvailble Checks Passed");
            DebugReport(x: s.ToString());
            return true;
        }

        public static bool ResultFalseWithReport(StringBuilder s)
        {
            s.Append(value: "ActorAvailble: Result = Unavailable");
            DebugReport(x: s.ToString());
            return false;
        }

        static public Pawn GenerateNewPawnFromSource(ThingDef newDef, Pawn sourcePawn)
        {
            Pawn pawn = (Pawn)ThingMaker.MakeThing(def: newDef);
            //Utility.DebugReport("Declare a new thing");
            pawn.Name = sourcePawn.Name;
            //Utility.DebugReport("The name!");
            pawn.SetFactionDirect(newFaction: Faction.OfPlayer);
            pawn.kindDef = sourcePawn.kindDef;
            //Utility.DebugReport("The def!");
            pawn.pather = new Pawn_PathFollower(newPawn: pawn);
            //Utility.DebugReport("The pather!");
            pawn.ageTracker = new Pawn_AgeTracker(newPawn: pawn);
            pawn.health = new Pawn_HealthTracker(pawn: pawn);
            pawn.jobs = new Pawn_JobTracker(newPawn: pawn);
            pawn.mindState = new Pawn_MindState(pawn: pawn);
            pawn.filth = new Pawn_FilthTracker(pawn: pawn);
            pawn.needs = new Pawn_NeedsTracker(newPawn: pawn);
            pawn.stances = new Pawn_StanceTracker(newPawn: pawn);
            pawn.natives = new Pawn_NativeVerbs(pawn: pawn);
            pawn.relations = sourcePawn.relations;
            PawnComponentsUtility.CreateInitialComponents(pawn: pawn);

            if (pawn.RaceProps.ToolUser)
            {
                pawn.equipment = new Pawn_EquipmentTracker(newPawn: pawn);
                pawn.carryTracker = new Pawn_CarryTracker(pawn: pawn);
                pawn.apparel = new Pawn_ApparelTracker(pawn: pawn);
                pawn.inventory = new Pawn_InventoryTracker(pawn: pawn);
            }

            if (pawn.RaceProps.intelligence <= Intelligence.ToolUser)
            {
                pawn.caller = new Pawn_CallTracker(pawn: pawn);
            }

            pawn.gender = sourcePawn.gender;
            pawn.needs.SetInitialLevels();
            GenerateRandomAge(pawn: pawn, map: sourcePawn.Map);
            CopyPawnRecords(pawn: sourcePawn, newPawn: pawn);
            //Utility.DebugReport("We got so far.");
            return pawn;
        }


        /// <summary>
        /// This method handles the application of Sanity Loss in multiple mods.
        /// It returns true and false depending on if it applies successfully.
        /// </summary>
        /// <param name="pawn"></param>
        /// <param name="sanityLoss"></param>
        /// <param name="sanityLossMax"></param>
        public static bool RemoveSanityLoss(Pawn pawn)
        {
            bool removedSuccessfully = false;
            if (pawn != null)
            {
                string sanityLossDef = (!IsCosmicHorrorsLoaded()) ? AltSanityLossDef : SanityLossDef;

                var pawnSanityHediff =
                    pawn.health.hediffSet.GetFirstHediffOfDef(
                        def: DefDatabase<HediffDef>.GetNamedSilentFail(defName: sanityLossDef));
                if (pawnSanityHediff != null)
                {
                    pawn.health.RemoveHediff(hediff: pawnSanityHediff);
                }
            }

            return removedSuccessfully;
        }

        static public void CopyPawnRecords(Pawn pawn, Pawn newPawn)
        {
            //Who has a relationship with this pet?
            Pawn pawnMaster = null;
            Map map = pawn.Map;
            foreach (Pawn current in map.mapPawns.AllPawns)
            {
                if (current.relations.DirectRelationExists(def: PawnRelationDefOf.Bond, otherPawn: pawn))
                {
                    pawnMaster = current;
                }
            }

            //Fix the relations
            if (pawnMaster != null)
            {
                pawnMaster.relations.TryRemoveDirectRelation(def: PawnRelationDefOf.Bond, otherPawn: pawn);
                pawnMaster.relations.AddDirectRelation(def: PawnRelationDefOf.Bond, otherPawn: newPawn);
                //Train that stuff!

                DefMap<TrainableDef, int> oldMap = (DefMap<TrainableDef, int>)typeof(Pawn_TrainingTracker)
                    .GetField(name: "steps", bindingAttr: BindingFlags.Instance | BindingFlags.NonPublic)
                    .GetValue(obj: pawn.training);
                DefMap<TrainableDef, int> newMap = (DefMap<TrainableDef, int>)typeof(Pawn_TrainingTracker)
                    .GetField(name: "steps", bindingAttr: BindingFlags.Instance | BindingFlags.NonPublic)
                    .GetValue(obj: newPawn.training);

                foreach (TrainableDef def in DefDatabase<TrainableDef>.AllDefs)
                {
                    newMap[def: def] = oldMap[def: def];
                }
            }


            foreach (Hediff hediff in pawn.health.hediffSet.hediffs)
            {
                newPawn.health.AddHediff(hediff: hediff);
            }
        }

        static public void GenerateRandomAge(Pawn pawn, Map map)
        {
            int num = 0;
            int num2;
            do
            {
                if (pawn.RaceProps.ageGenerationCurve != null)
                {
                    num2 = Mathf.RoundToInt(f: Rand.ByCurve(curve: pawn.RaceProps.ageGenerationCurve));
                }
                else if (pawn.RaceProps.IsMechanoid)
                {
                    num2 = Rand.Range(min: 0, max: 2500);
                }
                else
                {
                    if (!pawn.RaceProps.Animal)
                    {
                        goto IL_84;
                    }

                    num2 = Rand.Range(min: 1, max: 10);
                }

                num++;
                if (num > 100)
                {
                    goto IL_95;
                }
            } while (num2 > pawn.kindDef.maxGenerationAge || num2 < pawn.kindDef.minGenerationAge);

            goto IL_A5;
            IL_84:
            Log.Warning(text: "Didn't get age for " + pawn);
            return;
            IL_95:
            Log.Error(text: "Tried 100 times to generate age for " + pawn);
            IL_A5:
            pawn.ageTracker.AgeBiologicalTicks = ((long)(num2 * 3600000f) + Rand.Range(min: 0, max: 3600000));
            int num3;
            if (Rand.Value < pawn.kindDef.backstoryCryptosleepCommonality)
            {
                float value = Rand.Value;
                if (value < 0.7f)
                {
                    num3 = Rand.Range(min: 0, max: 100);
                }
                else if (value < 0.95f)
                {
                    num3 = Rand.Range(min: 100, max: 1000);
                }
                else
                {
                    int num4 = GenLocalDate.Year(map: map) - 2026 - pawn.ageTracker.AgeBiologicalYears;
                    num3 = Rand.Range(min: 1000, max: num4);
                }
            }
            else
            {
                num3 = 0;
            }

            long num5 = GenTicks.TicksAbs - pawn.ageTracker.AgeBiologicalTicks;
            num5 -= num3 * 3600000L;
            pawn.ageTracker.BirthAbsTicks = num5;
            if (pawn.ageTracker.AgeBiologicalTicks > pawn.ageTracker.AgeChronologicalTicks)
            {
                pawn.ageTracker.AgeChronologicalTicks = (pawn.ageTracker.AgeBiologicalTicks);
            }
        }


        /// <summary>
        /// A very complicated method for finding a proper place for objects to spawn in Cthulhu Utility.
        /// </summary>
        /// <param name="def"></param>
        /// <param name="nearLoc"></param>
        /// <param name="map"></param>
        /// <param name="maxDist"></param>
        /// <param name="pos"></param>
        /// <returns></returns>
        public static bool TryFindSpawnCell(ThingDef def, IntVec3 nearLoc, Map map, int maxDist, out IntVec3 pos) =>
            CellFinder.TryFindRandomCellNear(root: nearLoc, map: map, squareRadius: maxDist,
                validator: delegate(IntVec3 x)
                {
                    ///Check if the entire area is safe based on the size of the object definition.
                    foreach (IntVec3 current in GenAdj.OccupiedRect(center: x, rot: Rot4.North,
                                 size: new IntVec2(newX: def.size.x + 2, newZ: def.size.z + 2)))
                    {
                        if (!current.InBounds(map: map) || current.Fogged(map: map) || !current.Standable(map: map) ||
                            (current.Roofed(map: map) && current.GetRoof(map: map).isThickRoof))
                        {
                            return false;
                        }

                        if (!current.SupportsStructureType(map: map, surfaceType: def.terrainAffordanceNeeded))
                        {
                            return false;
                        }

                        ///
                        //  If it has an interaction cell, check to see if it can be reached by colonists.
                        //
                        bool intCanBeReached = true;
                        if (def.interactionCellOffset != IntVec3.Zero)
                        {
                            foreach (Pawn colonist in map.mapPawns.FreeColonistsSpawned)
                            {
                                if (!colonist.CanReach(dest: current + def.interactionCellOffset,
                                        peMode: PathEndMode.ClosestTouch, maxDanger: Danger.Deadly))
                                    intCanBeReached = false;
                            }
                        }

                        if (!intCanBeReached)
                            return false;
                        //

                        //Don't wipe existing objets...
                        List<Thing> thingList = current.GetThingList(map: map);
                        for (int i = 0; i < thingList.Count; i++)
                        {
                            Thing thing = thingList[index: i];
                            if (thing.def.category != ThingCategory.Plant &&
                                GenSpawn.SpawningWipes(newEntDef: def, oldEntDef: thing.def))
                            {
                                return false;
                            }
                        }
                    }

                    return true;
                }, result: out pos);

        public static BodyPartRecord GetHeart(HediffSet set)
        {
            foreach (BodyPartRecord current in set.GetNotMissingParts(height: BodyPartHeight.Undefined,
                         depth: BodyPartDepth.Undefined))
            {
                for (int i = 0; i < current.def.tags.Count; i++)
                {
                    if (current.def.tags[index: i].defName == "BloodPumpingSource")
                    {
                        return current;
                    }
                }
            }

            return null;
        }


        public static void SpawnThingDefOfCountAt(ThingDef of, int count, TargetInfo target)
        {
            while (count > 0)
            {
                Thing thing = ThingMaker.MakeThing(def: of, stuff: null);

                thing.stackCount = Math.Min(val1: count, val2: of.stackLimit);
                GenPlace.TryPlaceThing(thing: thing, center: target.Cell, map: target.Map, mode: ThingPlaceMode.Near);
                count -= thing.stackCount;
            }
        }

        public static void SpawnPawnsOfCountAt(PawnKindDef kindDef, IntVec3 at, Map map, int count, out Pawn returnable,
            Faction fac = null, bool berserk = false, bool target = false)
        {
            Pawn result = null;
            for (int i = 1; i <= count; i++)
            {
                if ((from cell in GenAdj.CellsAdjacent8Way(pack: new TargetInfo(cell: at, map: map))
                        where at.Walkable(map: map)
                        select cell).TryRandomElement(result: out at))
                {
                    Pawn pawn = PawnGenerator.GeneratePawn(kindDef: kindDef, faction: fac);
                    if (result == null) result = pawn;
                    if (GenPlace.TryPlaceThing(thing: pawn, center: at, map: map, mode: ThingPlaceMode.Near,
                            placedAction: null))
                    {
                        //if (target) Map.GetComponent<MapComponent_SacrificeTracker>().lastLocation = at;
                        //continue;
                    }

                    //Find.WorldPawns.PassToWorld(pawn, PawnDiscardDecideMode.Discard);
                    if (berserk)
                        pawn.mindState.mentalStateHandler.TryStartMentalState(stateDef: MentalStateDefOf.Berserk);
                }
            }

            returnable = result;
        }

        public static void SpawnPawnsOfCountAt(PawnKindDef kindDef, IntVec3 at, Map map, int count, Faction fac = null,
            bool berserk = false, bool target = false)
        {
            for (int i = 1; i <= count; i++)
            {
                if ((from cell in GenAdj.CellsAdjacent8Way(pack: new TargetInfo(cell: at, map: map))
                        where at.Walkable(map: map)
                        select cell).TryRandomElement(result: out at))
                {
                    Pawn pawn = PawnGenerator.GeneratePawn(kindDef: kindDef, faction: fac);
                    if (GenPlace.TryPlaceThing(thing: pawn, center: at, map: map, mode: ThingPlaceMode.Near,
                            placedAction: null))
                    {
                        //if (target) Map.GetComponent<MapComponent_SacrificeTracker>().lastLocation = at;
                        //continue;
                    }

                    //Find.WorldPawns.PassToWorld(pawn, PawnDiscardDecideMode.Discard);
                    if (berserk)
                        pawn.mindState.mentalStateHandler.TryStartMentalState(stateDef: MentalStateDefOf.Berserk);
                }
            }
        }

        public static bool TryGetUnreservedPewSpot(Thing pew, Pawn claimer, out IntVec3 loc)
        {
            loc = IntVec3.Invalid;

            Map map = pew.Map;
            Rot4 currentDirection = pew.Rotation;

            IntVec3 CellNorth = pew.Position + GenAdj.CardinalDirections[Rot4.North.AsInt];
            IntVec3 CellSouth = pew.Position + GenAdj.CardinalDirections[Rot4.South.AsInt];
            IntVec3 CellEast = pew.Position + GenAdj.CardinalDirections[Rot4.East.AsInt];
            IntVec3 CellWest = pew.Position + GenAdj.CardinalDirections[Rot4.West.AsInt];

            if (claimer.CanReserve(target: pew.PositionHeld))
            {
                loc = pew.Position;
                return true;
            }

            if (currentDirection == Rot4.North ||
                currentDirection == Rot4.South)
            {
                if (claimer.CanReserve(target: CellWest))
                {
                    loc = CellWest;
                    return true;
                }

                if (claimer.CanReserve(target: CellEast))
                {
                    loc = CellEast;
                    return true;
                }
            }

            if (currentDirection == Rot4.East ||
                currentDirection == Rot4.West)
            {
                if (claimer.CanReserve(target: CellNorth))
                {
                    loc = CellNorth;
                    return true;
                }

                if (claimer.CanReserve(target: CellSouth))
                {
                    loc = CellSouth;
                    return true;
                }
            }

            //map.reservationManager.Reserve(claimer, pew);
            return false;
        }


        public static void ChangeResearchProgress(ResearchProjectDef projectDef, float progressValue,
            bool deselectCurrentResearch = false)
        {
            FieldInfo researchProgressInfo = typeof(ResearchManager).GetField(name: "progress",
                bindingAttr: BindingFlags.Instance | BindingFlags.NonPublic);
            object researchProgress = researchProgressInfo.GetValue(obj: Find.ResearchManager);
            PropertyInfo itemPropertyInfo = researchProgress.GetType().GetProperty(name: "Item");
            itemPropertyInfo.SetValue(obj: researchProgress, value: progressValue, index: new[] { projectDef });
            if (deselectCurrentResearch) Find.ResearchManager.currentProj = null;
            Find.ResearchManager.ReapplyAllMods();
        }

        public static float CurrentSanityLoss(Pawn pawn)
        {
            string sanityLossDef;
            sanityLossDef = AltSanityLossDef;
            if (IsCosmicHorrorsLoaded()) sanityLossDef = SanityLossDef;

            Hediff pawnSanityHediff =
                pawn.health.hediffSet.GetFirstHediffOfDef(def: DefDatabase<HediffDef>.GetNamed(defName: sanityLossDef));
            if (pawnSanityHediff != null)
            {
                return pawnSanityHediff.Severity;
            }

            return 0f;
        }


        public static void ApplyTaleDef(string defName, Map map)
        {
            Pawn randomPawn = map.mapPawns.FreeColonists.RandomElement<Pawn>();
            TaleDef taleToAdd = TaleDef.Named(str: defName);
            TaleRecorder.RecordTale(def: taleToAdd, args: new object[]
            {
                randomPawn,
            });
        }

        public static void ApplyTaleDef(string defName, Pawn pawn)
        {
            TaleDef taleToAdd = TaleDef.Named(str: defName);
            if ((pawn.IsColonist || pawn.HostFaction == Faction.OfPlayer) && taleToAdd != null)
            {
                TaleRecorder.RecordTale(def: taleToAdd, args: new object[]
                {
                    pawn,
                });
            }
        }


        public static bool HasSanityLoss(Pawn pawn) =>
            pawn?.health?.hediffSet?.GetFirstHediffOfDef(
                def: DefDatabase<HediffDef>.GetNamed(defName: SanityLossDef)) != null;

        public static void ApplySanityLoss(Pawn pawn, float sanityLoss = 0.3f, float sanityLossMax = 1.0f)
        {
            if (pawn == null) return;
            if (pawn.Spawned == false) return;
            if (pawn.Destroyed) return;
            if (pawn?.RaceProps is { Animal: false, Humanlike: false }) return;

            if (pawn?.health?.hediffSet?.GetFirstHediffOfDef(
                    def: DefDatabase<HediffDef>.GetNamed(defName: SanityLossDef)) is { } pawnSanityHediff)
            {
                if (pawnSanityHediff.Severity > sanityLossMax) sanityLossMax = pawnSanityHediff.Severity;
                float result = pawnSanityHediff.Severity;
                result += sanityLoss;
                result = Mathf.Clamp(value: result, min: 0.0f, max: sanityLossMax);
                pawnSanityHediff.Severity = result;
            }
            else if (sanityLoss > 0)
            {
                Hediff sanityLossHediff = HediffMaker.MakeHediff(
                    def: DefDatabase<HediffDef>.GetNamed(defName: SanityLossDef), pawn: pawn, partRecord: null);
                sanityLossHediff.Severity = sanityLoss;
                if (pawn.health != null) pawn.health.AddHediff(hediff: sanityLossHediff, part: null, dinfo: null);
                string sanityLossName = "";
                if (pawn?.Name is Name name)
                {
                    sanityLossName = name.ToStringShort;
                }

                if (pawn?.Faction?.IsPlayer == true)
                    Messages.Message(msg: new Message(text: "ROMCH_SanityLossBegun".Translate(arg1: sanityLossName),
                        def: MessageTypeDefOf.NegativeEvent, lookTargets: pawn));
            }
        }


        public static int GetSocialSkill(Pawn p) => p.skills.GetSkill(skillDef: SkillDefOf.Social).Level;

        public static int GetResearchSkill(Pawn p) => p.skills.GetSkill(skillDef: SkillDefOf.Intellectual).Level;

        public static bool IsCosmicHorrorsLoaded()
        {
            if (!modCheck) ModCheck();
            return loadedCosmicHorrors;
        }


        public static bool IsIndustrialAgeLoaded()
        {
            if (!modCheck) ModCheck();
            return loadedIndustrialAge;
        }


        public static bool IsCultsLoaded()
        {
            if (!modCheck) ModCheck();
            return loadedCults;
        }

        public static bool IsRandomWalkable8WayAdjacentOf(IntVec3 cell, Map map, out IntVec3 resultCell)
        {
            if (cell != IntVec3.Invalid)
            {
                IntVec3 temp = cell.RandomAdjacentCell8Way();
                if (map != null)
                {
                    for (int i = 0; i < 100; i++)
                    {
                        temp = cell.RandomAdjacentCell8Way();
                        if (temp.Walkable(map: map))
                        {
                            resultCell = temp;
                            return true;
                        }
                    }
                }
            }

            resultCell = IntVec3.Invalid;
            return false;
        }

        public static void TemporaryGoodwill(Faction faction, bool reset = false)
        {
            Faction playerFaction = Faction.OfPlayer;
            if (!reset)
            {
                if (faction.GoodwillWith(other: playerFaction) == 0f)
                {
                    faction.RelationWith(other: playerFaction, allowNull: false).baseGoodwill = faction.PlayerGoodwill;
                }

                faction.RelationWith(other: playerFaction, allowNull: false).baseGoodwill = 100;
                faction.RelationWith(other: playerFaction, allowNull: false).kind = FactionRelationKind.Neutral;
            }
            else
            {
                faction.RelationWith(other: playerFaction, allowNull: false).baseGoodwill = 0;
                faction.RelationWith(other: playerFaction, allowNull: false).kind = FactionRelationKind.Hostile;
            }
        }


        public static void ModCheck()
        {
            loadedCosmicHorrors = false;
            loadedIndustrialAge = false;
            foreach (ModContentPack ResolvedMod in LoadedModManager.RunningMods)
            {
                if (loadedCosmicHorrors && loadedIndustrialAge && loadedCults) break; //Save some loading
                if (ResolvedMod.Name.Contains(value: "Call of Cthulhu - Cosmic Horrors"))
                {
                    DebugReport(x: "Loaded - Call of Cthulhu - Cosmic Horrors");
                    loadedCosmicHorrors = true;
                }

                if (ResolvedMod.Name.Contains(value: "Call of Cthulhu - Industrial Age"))
                {
                    DebugReport(x: "Loaded - Call of Cthulhu - Industrial Age");
                    loadedIndustrialAge = true;
                }

                if (ResolvedMod.Name.Contains(value: "Call of Cthulhu - Cults"))
                {
                    DebugReport(x: "Loaded - Call of Cthulhu - Cults");
                    loadedCults = true;
                }

                if (ResolvedMod.Name.Contains(value: "Call of Cthulhu - Factions"))
                {
                    DebugReport(x: "Loaded - Call of Cthulhu - Factions");
                    loadedFactions = true;
                }
            }

            modCheck = true;
            return;
        }

        public static string Prefix => ModProps.main + " :: " + ModProps.mod + " " + ModProps.version + " :: ";

        public static void DebugReport(string x)
        {
            if (Prefs.DevMode && DebugSettings.godMode)
            {
                Log.Message(text: Prefix + x);
            }
        }

        public static void ErrorReport(string x) => Log.Error(text: Prefix + x);
    }
}