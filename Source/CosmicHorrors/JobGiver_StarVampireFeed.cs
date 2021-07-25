
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
            JobGiver_StarVampireFeed jobGiver_StarVampireFeed = (JobGiver_StarVampireFeed)base.DeepCopy(resolve);
            //jobGiver_StarVampireFeed.ticksUntilStartingAttack = this.ticksUntilStartingAttack;
            //jobGiver_StarVampireFeed.startTicks = this.startTicks;
            //jobGiver_StarVampireFeed.setStartTicks = this.setStartTicks;
            //jobGiver_StarVampireFeed.showedMessage = this.showedMessage;
            return jobGiver_StarVampireFeed;
        }

        protected override Job TryGiveJob(Pawn pawn)
        {
            if (pawn.Downed || pawn.Dead || !pawn.Spawned) return null;
            if (pawn.TryGetAttackVerb(null) == null)
            {
                return null;
            }
            Predicate<Thing> validator = t => ((t != pawn) && t.Spawned) && (t is Building_Turret);
            var pawn2 = pawn as CosmicHorrorPawn;
            var targetA = GenClosest.ClosestThingReachable(pawn.Position, pawn.MapHeld, ThingRequest.ForGroup(ThingRequestGroup.BuildingArtificial), PathEndMode.ClosestTouch, TraverseParms.For(pawn2, Danger.Deadly, TraverseMode.PassDoors, true), 999f, validator, null);
            if (targetA != null)
            {

                //if (pawnSelf.canReveal()) { pawnSelf.Reveal(); }
                return new Job(JobDefOf.AttackMelee, targetA)
                {
                    //maxNumMeleeAttacks = 1,
                    expiryInterval = Rand.Range(420, 900)
                };
            }

            bool Predicate2(Thing t)
            {
                if (t == pawn)
                {
                    return false;
                }
                Pawn pawn1 = t as Pawn;
                if ((pawn1 == null) || (!t.Spawned) || (pawn1.kindDef.ToString() == "ROM_StarVampire"))
                {
                    return false;
                }
                if (pawn1.RaceProps.Animal)
                {
                    return false;
                }
                return true;
            }

            var thing2 = GenClosest.ClosestThingReachable(pawn.Position, pawn.MapHeld, ThingRequest.ForGroup(ThingRequestGroup.Pawn), PathEndMode.OnCell, TraverseParms.For(pawn2, Danger.Deadly, TraverseMode.PassDoors, false), notRaidingAttackRange, Predicate2);
            var pawnTarget = thing2 as Pawn;
            if (thing2 == null) return null;
            Thing thing3;
            using (var path = pawn.Map.pathFinder.FindPath(pawn.Position, thing2.Position, TraverseParms.For(pawn, Danger.Deadly, TraverseMode.PassDoors, false), PathEndMode.OnCell))
            {
                thing3 = path.FirstBlockingBuilding(out IntVec3 vec, pawn);
            }
            if (thing3 != null)
            {
                return new Job(JobDefOf.AttackMelee, thing3)
                {

                    //maxNumMeleeAttacks = 1,
                    expiryInterval = Rand.Range(420, 900),
                    locomotionUrgency = LocomotionUrgency.Sprint
                };
            }
            if (pawnTarget == null) return null;
            if (pawnTarget.Downed)
                return new Job(JobDefOf.AttackMelee, thing2)
                {
                    expiryInterval = Rand.Range(57420, 57900),
                    locomotionUrgency = LocomotionUrgency.Sprint
                };
            return pawnTarget.Dead
                ? new Job(JobDefOf.Ingest, thing2)
                {
                    //maxNumMeleeAttacks = 1,
                    expiryInterval = Rand.Range(57420, 57900),
                    locomotionUrgency = LocomotionUrgency.Sprint
                }
                : new Job(JobDefOf.PredatorHunt, thing2)
                {
                    //maxNumMeleeAttacks = 1,
                    expiryInterval = Rand.Range(57420, 57900),
                    locomotionUrgency = LocomotionUrgency.Sprint,
                    killIncappedTarget = true
                };

            //if (pawnSelf.canReveal()) { pawnSelf.Reveal(); }
            //pawnSelf.Hide();
        }
    }

}