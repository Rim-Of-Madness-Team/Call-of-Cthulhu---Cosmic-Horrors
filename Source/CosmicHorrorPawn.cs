//What I need
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
    internal class CosmicHorrorPawn : Pawn
    {
        //Fields
        public bool isCosmicHorror = true;
        public bool isInvisible = false;
        public float sanityLossRate = 0.03f;
        public float sanityLossMax = 0.3f;

        private IntVec3 lastSpot = IntVec3.Invalid;
        private IntVec3 lastFoggedSpot = IntVec3.Invalid;
        private Sustainer movingSound = null;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.LookValue<IntVec3>(ref this.lastSpot, "lastSpot", default(IntVec3), false);
            Scribe_Values.LookValue<bool>(ref this.isCosmicHorror, "isCosmicHorror", true, false);
            Scribe_Values.LookValue<bool>(ref this.isInvisible, "isInvisible", false, false);
            Scribe_Values.LookValue<float>(ref this.sanityLossRate, "sanityLossRate", 0.03f, false);
            Scribe_Values.LookValue<float>(ref this.sanityLossMax, "sanityLossMax", 0.3f, false);
        }

        #region SpawnSetup
        /// <summary>
        /// Check for initial startup.
        /// If Star Vampire, then hide.
        /// </summary>
        /// 
        public override void SpawnSetup(Map map)
        {
            base.SpawnSetup(map);

            if (this.kindDef.ToString() == "CosmicHorror_StarVampire")
            {
                this.isInvisible = true;
            }
            //How much sanity will each monster cause to lose?
            Cthulhu.Utility.GetSanityLossRate(this.kindDef);
        }
        #endregion SpawnSetup

        #region Invisibility
        /// <summary>
        /// Overriding the drawer for the Pawn
        /// </summary>
        /// <param name="drawLoc"></param>
        public override void DrawAt(Vector3 drawLoc)
        {
            if (this.isInvisible)
            {
                return;
            }
            base.DrawAt(drawLoc);
        }

        /// <summary>
        /// If we are melee attacking, we need to reveal the Star Vampire.
        /// Check for three steps away from the player.
        /// Reveal the Star Vampire.
        /// </summary>
        /// <returns></returns>
        public bool canReveal()
        {
            if (this.isInvisible)
            {

                IntVec3 position = this.Position;

                Predicate<Thing> predicate2 = delegate (Thing t)
                {
                    if (t == this)
                    {
                        return false;
                    }
                    Pawn pawn1 = t as Pawn;
                    if (((pawn1 == null)) || (!t.Spawned || (pawn1.kindDef.ToString() == "CosmicHorror_StarVampire")))
                    {
                        return false;
                    }
                    if (pawn1.RaceProps.Animal)
                    {
                        return false;
                    }
                    return true;
                };
                Thing thing2 = GenClosest.ClosestThingReachable(this.Position, this.MapHeld, ThingRequest.ForGroup(ThingRequestGroup.Pawn), PathEndMode.OnCell, TraverseParms.For(this, Danger.Deadly, TraverseMode.PassDoors, false), 3, predicate2, null, 50, true);
                if (thing2 != null)
                {
                    return true;
                }

            }
            return false;
        }

        public void ResolveInvisibility(DamageInfo dinfo, bool inAbsorbed, out bool absorbed)
        {
            absorbed = inAbsorbed;
            if (this.isInvisible)
            {
                dinfo.SetAmount((int)(0)); //No damage if it's invisible
                absorbed = true;
            }

        }

        #endregion Invisibility
        

        public override void PreApplyDamage(DamageInfo dinfo, out bool absorbed)
        {
            base.PreApplyDamage(dinfo, out absorbed);
            ResolveInvisibility(dinfo, absorbed, out absorbed);

            if (dinfo.Instigator != null)
            {
                if (dinfo.Instigator is Pawn)
                {
                    if (this.def.defName == "CosmicHorror_Chthonian")
                    {
                        if (this.jobs != null)
                        {
                            if (this.jobs.curJob.def != JobDefOf.AttackMelee)
                            {
                                Job j = new Job(JobDefOf.AttackMelee, dinfo.Instigator);
                                j.expiryInterval = 1000;
                                j.checkOverrideOnExpire = true;
                                j.expireRequiresEnemiesNearby = true;
                                this.jobs.EndCurrentJob(JobCondition.Incompletable);
                                this.jobs.TryTakeOrderedJob(j);
                            }
                        }
                    }
                }
            }
            if (absorbed)
            {
                return;
            }
        }

        public override void Tick()
        {
            base.Tick();
            if (this.canReveal() || this.Downed || this.Dead)
            {
                this.Reveal();
            }
            ResolveSpecialEffects();
            DownedCheck();
        }

        public void DownedCheck()
        {
            if (this.Downed)
            {
                if (!Utility.IsTameable(this.kindDef))
                {
                    this.health.Kill(null, null);
                }
            }
        }

        public void ResolveSpecialEffects()
        {
            Map map = this.Map;
            IntVec3 intVec = this.Position;

            if (!this.Dead && this.Spawned && this.pather.Moving)
            {

                if (this.def.defName == "CosmicHorror_Chthonian")
                {
                    if (movingSound == null)
                    {
                        SoundInfo info = SoundInfo.InMap(this, MaintenanceType.PerTick);
                        this.movingSound = SoundDef.Named("Pawn_CosmicHorror_Chthonian_Moving").TrySpawnSustainer(info);
                    }
                    if (lastSpot != IntVec3.Invalid)
                    {
                        //Standing still? No smoke needed
                        if (lastSpot != this.Position)
                        {
                            //Do this... inside the map, obviously.
                            if (intVec.InBounds(map))
                            {
                                //Throw some smoke
                                MoteMaker.ThrowSmoke(intVec.ToVector3(), map, 1f);
                                MoteMaker.ThrowDustPuff(intVec, map, 1f);
                                MoteMaker.ThrowDustPuff(lastSpot, map, 0.5f);

                                //Break the floor
                                if (map.terrainGrid.TerrainAt(intVec).layerable)
                                    map.terrainGrid.RemoveTopLayer(intVec, false);


                                TerrainDef richSoil = TerrainDef.Named("SoilRich");

                                //If it's not sand, change to fertile soil
                                if (map.terrainGrid.TerrainAt(intVec).fertility > 0.06 &&
                                    map.terrainGrid.TerrainAt(intVec) != richSoil)
                                {
                                    base.Map.terrainGrid.SetTerrain(intVec, richSoil);
                                }
                            }
                        }
                    }

                }
            }
            if (movingSound != null) movingSound.End();
            lastSpot = this.Position;
        }

        public override void TickRare()
        {
            base.TickRare();
            ObservationEffect();
        }

        #region Abilities
        /// <summary>
        /// Reveal the hidden creature
        /// </summary>
        public void Reveal()
        {
            if (this.isInvisible)
            {
                this.isInvisible = false;

                MoteMaker.ThrowAirPuffUp(Gen.TrueCenter(this), this.MapHeld);
            }
            return;
        }

        /// <summary>
        /// Reveal the hidden creature
        /// </summary>
        public void Hide()
        {
            if (!this.isInvisible)
            {
                this.isInvisible = true;
            }
            return;
        }
        #endregion Abilities

        public static bool ManhunterMessageSent = false;


        public Predicate<Thing> GetPredicate()
        {
            Predicate<Thing> predicate = delegate (Thing t)
            {
                if (t == null) return false;
                if (t == this) return false;
                if (!t.Spawned) return false;
                Pawn pawn1 = t as Pawn;
                if (pawn1 == null) return false;
                if (pawn1.Dead) return false;
                if (pawn1 is CosmicHorrorPawn) return false;
                if (pawn1.Faction == null) return false;
                if (this.Faction != null && pawn1.Faction != null)
                {
                    if (this.Faction == pawn1.Faction) return false;
                    if (!this.Faction.HostileTo(pawn1.Faction)) return false;
                }

                if (pawn1.needs == null) return false;
                if (pawn1.needs.mood == null) return false;
                if (pawn1.needs.mood.thoughts == null) return false;
                if (pawn1.needs.mood.thoughts.memories == null) return false;
                return true;
            };
            return predicate;
        }

        public void ObservationEffectLive(Pawn target)
        {
            try
            {
                if (this.StoringBuilding() == null && target.RaceProps.Humanlike)
                {
                    Thought_MemoryObservation thought_MemoryObservation;
                    thought_MemoryObservation = (Thought_MemoryObservation)ThoughtMaker.MakeThought(DefDatabase<ThoughtDef>.GetNamed("Observed" + def.ToString()));
                    thought_MemoryObservation.Target = this;
                    target.needs.mood.thoughts.memories.TryGainMemoryThought(thought_MemoryObservation);
                }

                ///This area gives sanity loss, if the witness sees a living cosmic horror.
                Cthulhu.Utility.ApplySanityLoss(target, sanityLossRate, sanityLossMax);
            }
            catch (NullReferenceException)
            { }
        }

        public void ObservationEffectDead(Pawn target)
        {
            try
            {

                Corpse ourBody = this.Corpse;
                if (ourBody == null) return;

                ///This area gives the thought for witnessing a cosmic horror.
                if (this == null) return;
                if (!ourBody.Spawned) return;
                if (ourBody.StoringBuilding() == null)
                {
                    ThoughtDef defToImplement = DefDatabase<ThoughtDef>.GetNamedSilentFail("Observed" + def.ToString());
                    if (defToImplement == null) return;
                    Thought_MemoryObservation thought_MemoryObservation;
                    thought_MemoryObservation = (Thought_MemoryObservation)ThoughtMaker.MakeThought(defToImplement);
                    thought_MemoryObservation.Target = this;
                    target.needs.mood.thoughts.memories.TryGainMemoryThought(thought_MemoryObservation);
                }
            }
            catch (NullReferenceException)
            { }
        }

        /// <summary>
        /// Checks around the cosmic horror for pawns to give sanity loss.
        /// Also gives a bad memory of the experinece
        /// </summary>
        public void ObservationEffect()
        {
            try
            {


                //This finds a suitable target pawn.
                Predicate<Thing> predicate = GetPredicate();


                Thing thing2 = GenClosest.ClosestThingReachable(this.PositionHeld, this.MapHeld, ThingRequest.ForGroup(ThingRequestGroup.Pawn), PathEndMode.OnCell, TraverseParms.For(this, Danger.Deadly, TraverseMode.PassDoors, false), 15, predicate, null, 50, true);
                if (thing2 != null && thing2.Position != IntVec3.Invalid)
                {
                    if (GenSight.LineOfSight(thing2.Position, this.PositionHeld, this.MapHeld))
                    {
                        Pawn target = thing2 as Pawn;
                        if (target != null)
                        {
                            if (target.RaceProps != null)
                            {
                                if (!target.RaceProps.IsMechanoid)
                                {
                                    if (!this.Dead && this.MapHeld != null)
                                    {
                                        ObservationEffectLive(target);
                                    }
                                    else
                                    {
                                        ObservationEffectDead(target);
                                    }
                                }
                            }
                        }
                    }

                }
            }
            catch (NullReferenceException)
            { }
        }

    }


}
