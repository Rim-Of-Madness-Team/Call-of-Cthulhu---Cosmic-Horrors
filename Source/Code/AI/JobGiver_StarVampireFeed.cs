using System;
//Maybe?
using RimWorld;
using Verse.AI;
using Verse;
using Verse.Sound;

namespace CosmicHorror
{
    public class JobGiver_StarVampireFeed : ThinkNode_JobGiver
    {
        private const int MaxMeleeChaseTicks = 900;
        private const int MinMeleeChaseTicks = 420;
        private const float WaitChance = 0.5f;
        private const int WaitTicks = 90;

        private const float notRaidingAttackRange = 999.9f;
        //protected IntRange ticksUntilStartingAttack = new IntRange(2000, 4000);
        //protected int startTicks;
        //protected bool setStartTicks = false;
        //protected bool showedMessage = false;

        public override ThinkNode DeepCopy(bool resolve = true)
        {
            JobGiver_StarVampireFeed jobGiver_StarVampireFeed =
                (JobGiver_StarVampireFeed)base.DeepCopy(resolve: resolve);
            //jobGiver_StarVampireFeed.ticksUntilStartingAttack = this.ticksUntilStartingAttack;
            //jobGiver_StarVampireFeed.startTicks = this.startTicks;
            //jobGiver_StarVampireFeed.setStartTicks = this.setStartTicks;
            //jobGiver_StarVampireFeed.showedMessage = this.showedMessage;
            return jobGiver_StarVampireFeed;
        }
        

        protected override Job TryGiveJob(Pawn pawn)
        {
            if (pawn.Downed || pawn.Dead || !pawn.Spawned) return null;
            if (pawn.TryGetAttackVerb(target: null) == null)
            {
                return null;
            }

            //Target turrets first
            bool Validator(Thing t) => ((t != pawn) && t.Spawned) && (t is Building_Turret);
            var pawn2 = pawn as CosmicHorrorPawn;
            var targetA = GenClosest.ClosestThingReachable(root: pawn.Position,
                map: pawn.MapHeld,
                thingReq: ThingRequest.ForGroup(@group: ThingRequestGroup.BuildingArtificial),
                peMode: PathEndMode.ClosestTouch,
                traverseParams: TraverseParms.For(pawn: pawn2,
                    maxDanger: Danger.Deadly,
                    mode: TraverseMode.PassDoors,
                    canBashDoors: true),
                maxDistance: 999f,
                validator: Validator,
                customGlobalSearchSet: null);
            if (targetA != null)
            {
                //if (pawnSelf.canReveal()) { pawnSelf.Reveal(); }
                return new Job(def: JobDefOf.AttackMelee, targetA: targetA)
                {
                    //maxNumMeleeAttacks = 1,
                    expiryInterval = Rand.Range(min: 420, max: 900)
                };
            }

            bool Predicate2(Thing t)
            {
                if (t == pawn)
                    return false;
                Pawn pawn1 = t as Pawn;
                if ((pawn1 == null) || (!t.Spawned) || (pawn1.kindDef.ToString() == "ROM_StarVampire"))
                    return false;
                if (pawn1.RaceProps.Animal)
                    return false;
                return true;
            }
            var thing2 = GenClosest.ClosestThingReachable(root: pawn.Position,
                map: pawn.MapHeld,
                thingReq: ThingRequest.ForGroup(@group: ThingRequestGroup.Pawn),
                peMode: PathEndMode.OnCell,
                traverseParams: TraverseParms.For(pawn: pawn2,
                    maxDanger: Danger.Deadly,
                    mode: TraverseMode.PassDoors,
                    canBashDoors: false),
                maxDistance: notRaidingAttackRange,
                validator: Predicate2);
            var pawnTarget = thing2 as Pawn;
            if (thing2 == null) return null;

            Thing thing3;
            using (var path = pawn.Map.pathFinder.FindPath(start: pawn.Position,
                       dest: thing2.Position,
                       traverseParms: TraverseParms.For(pawn: pawn,
                           maxDanger: Danger.Deadly,
                           mode: TraverseMode.PassDoors,
                           canBashDoors: false),
                       peMode: PathEndMode.OnCell))
            {
                thing3 = path.FirstBlockingBuilding(cellBefore: out IntVec3 vec, pawn: pawn);
            }

            if (thing3 != null)
            {
                return new Job(def: JobDefOf.AttackMelee, targetA: thing3)
                {
                    //maxNumMeleeAttacks = 1,
                    expiryInterval = Rand.Range(min: 420, max: 900),
                    locomotionUrgency = LocomotionUrgency.Sprint
                };
            }


            if (pawnTarget != null)
            {
                if (pawnTarget.Downed)
                    return new Job(def: JobDefOf.AttackMelee, targetA: thing2)
                    {
                        expiryInterval = Rand.Range(min: 57420, max: 57900),
                        locomotionUrgency = LocomotionUrgency.Sprint
                    };
                if (pawnTarget.Dead)
                {
                    return new Job(def: JobDefOf.Ingest, targetA: thing2)
                    {
                        //maxNumMeleeAttacks = 1,
                        expiryInterval = Rand.Range(min: 57420, max: 57900),
                        locomotionUrgency = LocomotionUrgency.Sprint
                    };
                }
                return new Job(def: JobDefOf.AttackMelee, targetA: thing2)
                {
                    //maxNumMeleeAttacks = 1,
                    expiryInterval = Rand.Range(min: 57420, max: 57900),
                    locomotionUrgency = LocomotionUrgency.Sprint,
                    killIncappedTarget = true
                };
            }
            return null;

            //if (pawnSelf.canReveal()) { pawnSelf.Reveal(); }
            //pawnSelf.Hide();
        }
    }
}