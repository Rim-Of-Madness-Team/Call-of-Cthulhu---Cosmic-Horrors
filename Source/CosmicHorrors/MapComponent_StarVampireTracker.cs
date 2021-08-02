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
            for(int i = 0; i < numberToSpawn; i++)
            {
                int index = this.starVampireSpawnType.lifeStages.Count - 1;
                Pawn pawn = PawnGenerator.GeneratePawn(new PawnGenerationRequest(this.starVampireSpawnType, this.starVampireFac, PawnGenerationContext.NonPlayer, -1, false, false, false, false, true, false, 1f, false, true, true, true, false, false, false, false, 0f, 0f, null, 1f, null, null, null, null, null, new float?(this.starVampireSpawnType.race.race.lifeStageAges[index].minAge), null, null, null, null, null, null, null, false, false, false));
                GenSpawn.Spawn(pawn, CellFinder.RandomClosewalkCellNear(this.positionToSpawn, this.mapLocation, 1, null), this.mapLocation, WipeMode.Vanish);
                Lord lord = mapLocation.GetComponent<MapComponent_StarVampireTracker>().GetStarVampireLord(pawn);
                lord.AddPawn(pawn);
            }
            currentStatus = SpawnerStatus.MarkedForRemoval;
        }


        public void ExposeData()
        {
            Scribe_Defs.Look<PawnKindDef>(ref this.starVampireSpawnType, "starVampireSpawnType");
            Scribe_References.Look<Faction>(ref this.starVampireFac, "starVampireFac");
            Scribe_References.Look<Map>(ref this.mapLocation, "mapLocation");
            Scribe_Values.Look(ref this.positionToSpawn, "positionToSpawn");
            Scribe_Values.Look<int>(ref this.numberToSpawn, "numberToSpawn");
            Scribe_Values.Look<int>(ref this.ticksUntilSpawned, "ticksUntilSpawned");
            Scribe_Values.Look(ref this.currentStatus, "currentStatus");
        }

    }

    public class MapComponent_StarVampireTracker : MapComponent
    {
        Lord starVampireLord = null;
        List<StarVampireSpawn> starVampireSpawners = new List<StarVampireSpawn>();

        public MapComponent_StarVampireTracker(Map map) : base(map)
        {

        }

        private PawnKindDef GetRandomStarVampireType()
        {
            List<PawnKindDef> types = new List<PawnKindDef>
            {
                PawnKindDef.Named("ROM_StarVampire"),
                PawnKindDef.Named("ROM_StarVampireAlbino"),
                PawnKindDef.Named("ROM_StarVampireAbberation"),
                PawnKindDef.Named("ROM_StarVampireNight")
            };
            return types.RandomElement();
        }


        private Faction GetStarVampireFaction()
        {
            return Find.FactionManager.FirstFactionOfDef(FactionDefOf.AncientsHostile);
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
                starVampireSpawners.RemoveAll(x => x.SafeToRemove());
            }
        }

        public void AddNewStarVampireSpawner(IntVec3 spawnLoc, int num)
        {
            if (starVampireSpawners == null) starVampireSpawners = new List<StarVampireSpawn>();
            IntVec3 intVec;
            if (!RCellFinder.TryFindRandomPawnEntryCell(out intVec, map, CellFinder.EdgeRoadChance_Animal, false, null))
            {
                Log.Error("Cannot find a place to spawn Star Vampire");
                return;
            }
            starVampireSpawners.Add(new StarVampireSpawn(GetRandomStarVampireType(), GetStarVampireFaction(), this.map, intVec, num, 12000));
        }

        public Lord GetStarVampireLord(Thing caller)
        {
            if (starVampireLord == null)
                starVampireLord = CreateNewLord(caller, true, 99999, typeof(LordJob_AssaultColony));
            return starVampireLord;
        }


        public static Lord CreateNewLord(Thing byThing, bool aggressive, float defendRadius, Type lordJobType)
        {
            IntVec3 invalid;
            if (!CellFinder.TryFindRandomCellNear(byThing.Position, byThing.Map, 5, (IntVec3 c) => c.Standable(byThing.Map) && byThing.Map.reachability.CanReach(c, byThing, PathEndMode.Touch, TraverseParms.For(TraverseMode.PassDoors, Danger.Deadly, false, false, false)), out invalid, -1))
            {
                Log.Error("Found no place for mechanoids to defend " + byThing);
                invalid = IntVec3.Invalid;
            }
            return LordMaker.MakeNewLord(byThing.Faction, Activator.CreateInstance(lordJobType, new object[]
            {
                new SpawnedPawnParams
                {
                    aggressive = aggressive,
                    defendRadius = defendRadius,
                    defSpot = invalid,
                    spawnerThing = byThing
                }
            }) as LordJob, byThing.Map, null);
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look<StarVampireSpawn>(ref this.starVampireSpawners, "starVampireSpawners", LookMode.Deep, Array.Empty<object>());
            Scribe_References.Look<Lord>(ref this.starVampireLord, "starVampireLord");
        }
    }
}
