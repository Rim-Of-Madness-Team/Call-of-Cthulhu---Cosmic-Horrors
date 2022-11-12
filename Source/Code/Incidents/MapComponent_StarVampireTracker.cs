using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace CosmicHorror
{
    public class StarVampireSpawn : IExposable
    {
        public enum SpawnerStatus
        {
            Countdown = 0,
            Spawning = 1,
            MarkedForRemoval = 2
        }

        public PawnKindDef starVampireSpawnType = null;
        public Faction starVampireFac = null;
        public Map mapLocation = null;
        public IntVec3 positionToSpawn;
        public int numberToSpawn = 0;
        public int ticksUntilSpawned = -999;
        public SpawnerStatus currentStatus = SpawnerStatus.Countdown;

        public bool SafeToRemove() => currentStatus == SpawnerStatus.MarkedForRemoval;

        public StarVampireSpawn()
        {
        }

        public StarVampireSpawn(PawnKindDef pkd, Faction fac, Map map, IntVec3 position, int num, int ticks)
        {
            starVampireSpawnType = pkd;
            starVampireFac = fac;
            mapLocation = map;
            positionToSpawn = position;
            numberToSpawn = num;
            ticksUntilSpawned = ticks;
            currentStatus = SpawnerStatus.Countdown;
        }

        public void Tick()
        {
            CountDown();
            ResolveSpawn();
        }

        public void CountDown()
        {
            ticksUntilSpawned--;
        }

        public void ResolveSpawn()
        {
            if (currentStatus == SpawnerStatus.Countdown && ticksUntilSpawned <= 0)
            {
                currentStatus = SpawnerStatus.Spawning;
                TrySpawnPawns();
            }
        }


        private void TrySpawnPawns()
        {
            for (int i = 0; i < numberToSpawn; i++)
            {
                int index = starVampireSpawnType.lifeStages.Count - 1;
                Pawn pawn = PawnGenerator.GeneratePawn(
                    request: new PawnGenerationRequest(
                        kind: starVampireSpawnType,
                        faction: starVampireFac,
                        context: PawnGenerationContext.NonPlayer,
                        tile: -1,
                        forceGenerateNewPawn: false,
                        allowDead: false,
                        allowDowned: false,
                        canGeneratePawnRelations: false,
                        mustBeCapableOfViolence: true,
                        colonistRelationChanceFactor: 0f,
                        forceAddFreeWarmLayerIfNeeded: false,
                        allowGay: false,
                        // allowPregnant: false, //1.4 only
                        allowFood: false,
                        allowAddictions: false,
                        inhabitant: false,
                        certainlyBeenInCryptosleep: false,
                        forceRedressWorldPawnIfFormerColonist: false,
                        worldPawnFactionDoesntMatter: false,
                        biocodeWeaponChance: 0f,
                        biocodeApparelChance: 0f,
                        extraPawnForExtraRelationChance: null,
                        relationWithExtraPawnChanceFactor: 1f,
                        validatorPreGear: null,
                        validatorPostGear: null,
                        forcedTraits: null,
                        prohibitedTraits: null,
                        minChanceToRedressWorldPawn: null,
                        fixedBiologicalAge: new float?(value: starVampireSpawnType.race.race
                            .lifeStageAges[index: index].minAge),
                        fixedChronologicalAge: null,
                        fixedGender: null,
                        fixedLastName: null,
                        fixedBirthName: null,
                        fixedTitle: null,
                        fixedIdeo: null,
                        forceNoIdeo: false,
                        forceNoBackstory: false,
                        forbidAnyTitle: false
                        //forceDead: false 1.4 only
                    )
                );
                GenSpawn.Spawn(newThing: pawn,
                    loc: CellFinder.RandomClosewalkCellNear(root: positionToSpawn, map: mapLocation,
                        radius: 1, extraValidator: null), map: mapLocation, wipeMode: WipeMode.Vanish);
                Lord lord = mapLocation.GetComponent<MapComponent_StarVampireTracker>()
                    .GetStarVampireLord(caller: pawn);
                lord.AddPawn(p: pawn);
            }

            currentStatus = SpawnerStatus.MarkedForRemoval;
        }


        public void ExposeData()
        {
            Scribe_Defs.Look<PawnKindDef>(value: ref starVampireSpawnType, label: "starVampireSpawnType");
            Scribe_References.Look<Faction>(refee: ref starVampireFac, label: "starVampireFac");
            Scribe_References.Look<Map>(refee: ref mapLocation, label: "mapLocation");
            Scribe_Values.Look(value: ref positionToSpawn, label: "positionToSpawn");
            Scribe_Values.Look<int>(value: ref numberToSpawn, label: "numberToSpawn");
            Scribe_Values.Look<int>(value: ref ticksUntilSpawned, label: "ticksUntilSpawned");
            Scribe_Values.Look(value: ref currentStatus, label: "currentStatus");
        }
    }

    public class MapComponent_StarVampireTracker : MapComponent
    {
        Lord starVampireLord = null;
        List<StarVampireSpawn> starVampireSpawners = new List<StarVampireSpawn>();

        public MapComponent_StarVampireTracker(Map map) : base(map: map)
        {
        }

        /// <summary>
        /// Randomly chooses between four variations of Star Vampires
        /// One only appears at night
        /// Albino are the rarest of all
        /// </summary>
        /// <returns></returns>
        private PawnKindDef GetRandomStarVampireType()
        {
            List<CosmicHorrorWeight> pawnKindDefList = new List<CosmicHorrorWeight>
            {
                new CosmicHorrorWeight(newName: "ROM_StarVampire", newWeight: 50), //Most common
                new CosmicHorrorWeight(newName: "ROM_StarVampireAbberation", newWeight: 25), //Second most common
                new CosmicHorrorWeight(newName: "ROM_StarVampireAlbino", newWeight: 1) //Rarest
            };
            if (!(GenLocalDate.DayPercent(map: map) < 0.2f) && GenLocalDate.DayPercent(map: map) > 0.7f)
                pawnKindDefList.Add(item: new CosmicHorrorWeight(newName: "ROM_StarVampireNight",
                    newWeight: 5)); //Rare, at night only

            return DefDatabase<PawnKindDef>.GetNamed(defName: pawnKindDefList
                .RandomElementByWeight(weightSelector: x => x.Weight).DefName);
        }


        private Faction GetStarVampireFaction()
        {
            return Find.FactionManager.FirstFactionOfDef(facDef: FactionDefOf.AncientsHostile);
        }


        public override void MapComponentTick()
        {
            base.MapComponentTick();
            if (starVampireSpawners != null && (starVampireSpawners?.Count() ?? 0) > 0)
            {
                foreach (StarVampireSpawn spawner in starVampireSpawners)
                {
                    spawner.Tick();
                }

                starVampireSpawners.RemoveAll(match: x => x.SafeToRemove());
            }
        }

        public void AddNewStarVampireSpawner(IntVec3 spawnLoc, int num)
        {
            if (starVampireSpawners == null) starVampireSpawners = new List<StarVampireSpawn>();
            IntVec3 intVec;
            if (!RCellFinder.TryFindRandomPawnEntryCell(result: out intVec, map: map,
                    roadChance: CellFinder.EdgeRoadChance_Animal, allowFogged: false, extraValidator: null))
            {
                Log.Error(text: "Cannot find a place to spawn Star Vampire");
                return;
            }

            starVampireSpawners.Add(item: new StarVampireSpawn(pkd: GetRandomStarVampireType(),
                fac: GetStarVampireFaction(), map: map, position: intVec, num: num, ticks: 12000));
        }

        public Lord GetStarVampireLord(Thing caller)
        {
            if (starVampireLord == null)
                starVampireLord = CreateNewLord(byThing: caller, aggressive: true, defendRadius: 99999,
                    lordJobType: typeof(LordJob_AssaultColony));
            return starVampireLord;
        }


        public static Lord CreateNewLord(Thing byThing, bool aggressive, float defendRadius, Type lordJobType)
        {
            IntVec3 invalid;
            if (!CellFinder.TryFindRandomCellNear(root: byThing.Position, map: byThing.Map, squareRadius: 5,
                    validator: (IntVec3 c) => c.Standable(map: byThing.Map) && byThing.Map.reachability.CanReach(
                        start: c, dest: byThing, peMode: PathEndMode.Touch,
                        traverseParams: TraverseParms.For(mode: TraverseMode.PassDoors, maxDanger: Danger.Deadly,
                            canBashDoors: false, alwaysUseAvoidGrid: false, canBashFences: false)), result: out invalid,
                    maxTries: -1))
            {
                Log.Error(text: "Found no place for mechanoids to defend " + byThing);
                invalid = IntVec3.Invalid;
            }

            return LordMaker.MakeNewLord(faction: byThing.Faction, lordJob: Activator.CreateInstance(type: lordJobType,
                args: new object[]
                {
                    new SpawnedPawnParams
                    {
                        aggressive = aggressive,
                        defendRadius = defendRadius,
                        defSpot = invalid,
                        spawnerThing = byThing
                    }
                }) as LordJob, map: byThing.Map, startingPawns: null);
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look<StarVampireSpawn>(list: ref starVampireSpawners, label: "starVampireSpawners",
                lookMode: LookMode.Deep, ctorArgs: Array.Empty<object>());
            Scribe_References.Look<Lord>(refee: ref starVampireLord, label: "starVampireLord");
        }
    }
}