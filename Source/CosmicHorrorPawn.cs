//What I need
using System;
using System.Linq;
//Maybe?
using RimWorld;
using Verse.AI;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace CosmicHorror
{
    public class CosmicHorrorPawn : Pawn
    {
        //Fields
        public float sanityLossRate = 0.03f;
        public float sanityLossMax = 0.3f;

        private IntVec3 lastSpot = IntVec3.Invalid;
        private IntVec3 lastFoggedSpot = IntVec3.Invalid;
        private Sustainer movingSound = null;

        private bool isInvisible = false;
        public bool IsInvisible { get => this.isInvisible; set => this.isInvisible = value; }

        private PawnExtension pawnExtension;

        public PawnExtension PawnExtension =>
            this.pawnExtension ?? (this.pawnExtension = this.def.HasModExtension<PawnExtension>() ? this.def.GetModExtension<PawnExtension>() : null);

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look<IntVec3>(ref this.lastSpot, "lastSpot", default(IntVec3), false);
            Scribe_Values.Look<bool>(ref this.isInvisible, "isInvisible", false, false);
            Scribe_Values.Look<float>(ref this.sanityLossRate, "sanityLossRate", 0.03f, false);
            Scribe_Values.Look<float>(ref this.sanityLossMax, "sanityLossMax", 0.3f, false);
        }
         
        #region SpawnSetup

        /// <summary>
        /// Check for initial startup.
        /// If Star Vampire, then hide.
        /// </summary>
        /// 
        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);

            this.IsInvisible = this.PawnExtension.invisible;
            this.sanityLossRate = this.PawnExtension.sanityLossRate;
            this.sanityLossMax = this.PawnExtension.sanityLossMax;

            //Give immunities.
            this.health.AddHediff(HediffDef.Named("ROM_CosmicHorrorImmunities"));
        }
        #endregion SpawnSetup
        
        #region Invisibility
        /// <summary>
        /// Overriding the drawer for the Pawn
        /// </summary>
        /// <param name="drawLoc"></param>
        public override void DrawAt(Vector3 drawLoc, bool flip)
        {
                if (this.IsInvisible)
                {
                    return;
                }
            base.DrawAt(drawLoc, flip);
        }

        public override void Draw()
        {
            if (this.IsInvisible)
            {
                return;
            }
            base.Draw();
        }

        /// <summary>
        /// If we are melee attacking, we need to reveal the Star Vampire.
        /// Check for three steps away from the player.
        /// Reveal the Star Vampire.
        /// </summary>
        /// <returns></returns>
        public bool CanReveal
        {
            get
            {
                if (this.IsInvisible)
                {
                    IntVec3 position = this.Position;
                    Predicate<Thing> predicate2 = delegate (Thing t)
                    {
                        if (t == this)
                        {
                            return false;
                        }
                        Pawn pawn1 = t as Pawn;
                        if (pawn1 == null || !t.Spawned || !this.PawnExtension.invisible)
                        {
                            return false;
                        }
                        if (pawn1.RaceProps.Animal)
                        {
                            return false;
                        }
                        return true;
                    };
                    Thing thing2 = GenClosest.ClosestThingReachable(this.Position, this.MapHeld, ThingRequest.ForGroup(ThingRequestGroup.Pawn), PathEndMode.OnCell, TraverseParms.For(this, Danger.Deadly, TraverseMode.PassDoors, false), 3, predicate2);
                    if (thing2 != null)
                    {
                        return true;
                    }
                }
                return false;
            }
        }

        public void ResolveInvisibility(DamageInfo dinfo, bool inAbsorbed, out bool absorbed)
        {
            absorbed = inAbsorbed;
            if (this.IsInvisible)
            {
                dinfo.SetAmount(0); //No damage if it's invisible
                absorbed = true;
            }

        }
        #endregion Invisibility
        
        public override void PreApplyDamage(ref DamageInfo dinfo, out bool absorbed)
        {
            base.PreApplyDamage(ref dinfo, out absorbed);
            ResolveInvisibility(dinfo, absorbed, out absorbed);

            if (dinfo.Instigator != null)
            {
                if (dinfo.Instigator is Pawn)
                {
                    if (this.def.defName == "ROM_Chthonian")
                    {
                        if (this.jobs != null)
                        {
                            if (this.jobs.curJob.def != JobDefOf.AttackMelee)
                            {
                                Job j = new Job(JobDefOf.AttackMelee, dinfo.Instigator)
                                {
                                    expiryInterval = 1000,
                                    checkOverrideOnExpire = true,
                                    expireRequiresEnemiesNearby = true
                                };
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
            if (this.CanReveal|| this.Downed || this.Dead)
            {
                this.Reveal();
            }

            HandleFluPlagueImmunity();
            ResolveSpecialEffects();
            DownedCheck();
            ResolveBleeding();
        }

        private void HandleFluPlagueImmunity()
        {
            if (Find.TickManager.TicksGame % 100 == 0)
            {
                health.hediffSet.hediffs.RemoveAll(x => x.def == HediffDef.Named("Animal_Flu") ||
                                                        x.def == HediffDef.Named("Animal_Plague") ||
                                                        x.def == HediffDef.Named("SandInEyes") ||
                                                        x.def == HediffDef.Named("MudInEyes") ||
                                                        x.def == HediffDef.Named("GravelInEyes") ||
                                                        x.def == HediffDef.Named("WaterInEyes") ||
                                                        x.def == HediffDef.Named("DirtInEyes"));
            }
        }

        public void ResolveBleeding()
        {
            if (this.PawnExtension != null && this.PawnExtension.regenInterval != 0 && this.PawnExtension.regenRate != 0)
            {
                if (Find.TickManager.TicksGame % this.PawnExtension.regenInterval == this.PawnExtension.regenRate)
                {
                    if (this.health != null)
                    {
                        if (this.health.hediffSet.GetInjuriesTendable() != null && this.health.hediffSet.GetInjuriesTendable().Count<Hediff_Injury>() > 0)
                        {
                            foreach (Hediff_Injury injury in this.health.hediffSet.GetInjuriesTendable())
                            {
                                injury.Severity = Mathf.Clamp(injury.Severity - 0.1f, 0.0f, 1.0f);
                            }
                        }
                    }
                }
            }
        }

        public void DownedCheck()
        {
            if (this.Downed)
            {
                if (!Utility.IsTameable(this.kindDef))
                {
                    this.Kill(null);
                }
            }
        }

        public void ResolveSpecialEffects()
        {
            Map map = this.Map;
            IntVec3 intVec = this.Position;

            if (!this.Dead && this.Spawned && this.pather.Moving)
            {
                if (this.def == MonsterDefOf.ROM_Chthonian.race)
                {
                    if (this.movingSound == null)
                    {
                        SoundInfo info = SoundInfo.InMap(this, MaintenanceType.PerTick);
                        this.movingSound = SoundDef.Named("Pawn_ROM_Chthonian_Moving").TrySpawnSustainer(info);
                    }
                    if (this.lastSpot != IntVec3.Invalid)
                    {
                        //Standing still? No smoke needed
                        if (this.lastSpot != this.Position)
                        {
                            //Do this... inside the map, obviously.
                            if (intVec.InBounds(map))
                            {
                                //Throw some smoke
                                MoteMaker.ThrowSmoke(intVec.ToVector3(), map, 1f);
                                MoteMaker.ThrowDustPuff(intVec, map, 1f);
                                MoteMaker.ThrowDustPuff(this.lastSpot, map, 0.5f);

                                //Break the floor
                                if (map.terrainGrid.TerrainAt(intVec).layerable)
                                    map.terrainGrid.RemoveTopLayer(intVec, false);

                                TerrainDef richSoil = TerrainDef.Named("SoilRich");

                                //If it's not sand, change to fertile soil
                                if (map.terrainGrid.TerrainAt(intVec).fertility > 0.06 &&
                                    map.terrainGrid.TerrainAt(intVec) != richSoil)
                                {
                                    this.Map.terrainGrid.SetTerrain(intVec, richSoil);
                                }
                            }
                        }
                    }
                }
            }
            if (this.movingSound != null)
                this.movingSound.End();
            this.lastSpot = this.Position;
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
            if (this.IsInvisible)
            {
                this.IsInvisible = false;
                MoteMaker.ThrowAirPuffUp(GenThing.TrueCenter(this), this.MapHeld);
            }
            return;
        }

        /// <summary>
        /// Reveal the hidden creature
        /// </summary>
        public void Hide()
        {
            if (!this.IsInvisible)
            {
                this.IsInvisible = true;
            }
            return;
        }
        #endregion Abilities

        public static bool ManhunterMessageSent = false;

        public Predicate<Thing> Predicate => delegate (Thing t)
        {
            if (t == null)
                return false;
            if (t == this)
                return false;
            if (!t.Spawned)
                return false;
            Pawn pawn1 = t as Pawn;
            if (pawn1 == null)
                return false;
            if (pawn1.Dead)
                return false;
            if (pawn1 is CosmicHorrorPawn)
                return false;
            if (pawn1.Faction == null)
                return false;
            if (this.Faction != null && pawn1.Faction != null)
            {
                if (this.Faction == pawn1.Faction)
                    return false;
                if (!this.Faction.HostileTo(pawn1.Faction))
                    return false;
            }

            if (pawn1.needs == null)
                return false;
            if (pawn1.needs.mood == null)
                return false;
            if (pawn1.needs.mood.thoughts == null)
                return false;
            if (pawn1.needs.mood.thoughts.memories == null)
                return false;
            return true;
        };

        public void ObservationEffectLive(Pawn target)
        {
            try
            {
                if (this.StoringThing() == null && target.RaceProps.Humanlike)
                {
                    Thought_MemoryObservation thought_MemoryObservation;
                    thought_MemoryObservation = (Thought_MemoryObservation)ThoughtMaker.MakeThought(DefDatabase<ThoughtDef>.GetNamed("Observed" + this.def.ToString()));
                    thought_MemoryObservation.Target = this;
                    target.needs.mood.thoughts.memories.TryGainMemory(thought_MemoryObservation);
                }

                ///This area gives sanity loss, if the witness sees a living cosmic horror.
                Cthulhu.Utility.ApplySanityLoss(target, this.sanityLossRate, this.sanityLossMax);
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
                if (ourBody.StoringThing() == null)
                {
                    ThoughtDef defToImplement = DefDatabase<ThoughtDef>.GetNamedSilentFail("Observed" + this.def.ToString());
                    if (defToImplement == null) return;
                    Thought_MemoryObservation thought_MemoryObservation;
                    thought_MemoryObservation = (Thought_MemoryObservation)ThoughtMaker.MakeThought(defToImplement);
                    thought_MemoryObservation.Target = this;
                    target.needs.mood.thoughts.memories.TryGainMemory(thought_MemoryObservation);
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
                Predicate<Thing> predicate = this.Predicate;
                
                Thing thing2 = GenClosest.ClosestThingReachable(this.PositionHeld, this.MapHeld, ThingRequest.ForGroup(ThingRequestGroup.Pawn), PathEndMode.OnCell, TraverseParms.For(this, Danger.Deadly, TraverseMode.PassDoors, false), 15, predicate);
                if (thing2 != null && thing2.Position != IntVec3.Invalid)
                {
                    if (GenSight.LineOfSight(thing2.Position, this.PositionHeld, this.MapHeld))
                    {
                        if (thing2 is Pawn target)
                        {
                            if (target.RaceProps != null)
                            {
                                if (!target.RaceProps.IsMechanoid)
                                {
                                    if (!this.isInvisible)
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
            }
            catch (NullReferenceException)
            { }
        }

    }


}
