
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
//Maybe?
using RimWorld;
using RimWorld.Planet;
using Verse.AI;
using UnityEngine;
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
        protected IntRange ticksUntilStartingAttack = new IntRange(2000, 4000);
        protected int startTicks;
        protected bool setStartTicks = false;
        protected bool showedMessage = false;
        
        public override ThinkNode DeepCopy(bool resolve = true)
        {
            JobGiver_StarVampireFeed jobGiver_StarVampireFeed = (JobGiver_StarVampireFeed)base.DeepCopy(resolve);
            //jobGiver_StarVampireFeed.ticksUntilStartingAttack = this.ticksUntilStartingAttack;
            jobGiver_StarVampireFeed.startTicks = this.startTicks;
            jobGiver_StarVampireFeed.setStartTicks = this.setStartTicks;
            jobGiver_StarVampireFeed.showedMessage = this.showedMessage;
            return jobGiver_StarVampireFeed;
        }

        protected override Job TryGiveJob(Pawn pawn)
        {
            //CosmicHorrorPawn pawnSelf = pawn as CosmicHorrorPawn;
            if (pawn.Downed) return null;
            if (pawn.Dead) return null;
            if (!pawn.Spawned) return null;
            if (!setStartTicks)
            {
                startTicks = Find.TickManager.TicksGame + ticksUntilStartingAttack.RandomInRange;
                setStartTicks = true;
                return new Job(JobDefOf.WaitWander) { expiryInterval = 90 };
            }
            if (Find.TickManager.TicksGame > (startTicks / 2) && showedMessage == false)
            {
                showedMessage = true;
                SoundDef warnSound = SoundDef.Named("Pawn_ROM_StarVampire_Warning");
                warnSound.PlayOneShotOnCamera();
                Messages.Message("StarVampireIncidentMessage2".Translate(), new RimWorld.Planet.GlobalTargetInfo(IntVec3.Invalid, pawn.Map), MessageSound.Standard);
            }
            if (Find.TickManager.TicksGame < startTicks)
            {
                return new Job(JobDefOf.WaitWander) { expiryInterval = 90 };
            }

            if (Rand.Value < 0.5f)
            {
                return new Job(JobDefOf.WaitCombat) { expiryInterval = 90 };
            }
            if (!pawn.health.Downed && !pawn.health.Dead)
            {
                Predicate<Thing> validator = t => ((t != pawn) && t.Spawned) && (t is Building_Turret);
                CosmicHorrorPawn pawn2 = pawn as CosmicHorrorPawn;
                Thing targetA = GenClosest.ClosestThingReachable(pawn.Position, pawn.MapHeld, ThingRequest.ForGroup(ThingRequestGroup.BuildingArtificial), PathEndMode.ClosestTouch, TraverseParms.For(pawn2, Danger.Deadly, TraverseMode.PassDoors, true), 999f, validator, null, 5, false);
                if (targetA != null)
                {

                    //if (pawnSelf.canReveal()) { pawnSelf.Reveal(); }
                    return new Job(JobDefOf.AttackMelee, targetA)
                    {
                        //maxNumMeleeAttacks = 1,
                        expiryInterval = Rand.Range(420, 900)
                    };
                }
                Predicate<Thing> predicate2 = delegate (Thing t)
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
                };
                Thing thing2 = GenClosest.ClosestThingReachable(pawn.Position, pawn.MapHeld, ThingRequest.ForGroup(ThingRequestGroup.Pawn), PathEndMode.OnCell, TraverseParms.For(pawn2, Danger.Deadly, TraverseMode.PassDoors, false), notRaidingAttackRange, predicate2, null, 50, true);
                Pawn pawnTarget = thing2 as Pawn;
                if (thing2 != null)
                {
                    Thing thing3;
                    using (PawnPath path = pawn.Map.pathFinder.FindPath(pawn.Position, thing2.Position, TraverseParms.For(pawn, Danger.Deadly, TraverseMode.PassDoors, false), PathEndMode.OnCell))
                    {
                        IntVec3 vec;
                        thing3 = path.FirstBlockingBuilding(out vec, pawn);
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
                    if (thing2 != null)
                    {
                        if (pawnTarget.Dead)
                        {
                            //if (pawnSelf.canReveal()) { pawnSelf.Reveal(); }
                            return new Job(JobDefOf.Ingest, thing2)
                            {
                                //maxNumMeleeAttacks = 1,
                                expiryInterval = Rand.Range(57420, 57900),
                                locomotionUrgency = LocomotionUrgency.Sprint
                            };
                        }

                        //if (pawnSelf.canReveal()) { pawnSelf.Reveal(); }
                        return new Job(JobDefOf.PredatorHunt, thing2)
                        {
                            //maxNumMeleeAttacks = 1,
                            expiryInterval = Rand.Range(57420, 57900),
                            locomotionUrgency = LocomotionUrgency.Sprint,
                            killIncappedTarget = true
                        };
                    }
                }
            }
            //pawnSelf.Hide();
            return null;
        }
    }

}