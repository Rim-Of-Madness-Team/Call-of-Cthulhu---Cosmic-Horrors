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

        public bool IsInvisible
        {
            get => isInvisible;
            set => isInvisible = value;
        }

        private PawnExtension pawnExtension;

        public PawnExtension PawnExtension =>
            pawnExtension ?? (pawnExtension = def.HasModExtension<PawnExtension>()
                ? def.GetModExtension<PawnExtension>()
                : null);

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look<IntVec3>(value: ref lastSpot, label: "lastSpot", defaultValue: default(IntVec3),
                forceSave: false);
            Scribe_Values.Look<bool>(value: ref isInvisible, label: "isInvisible", defaultValue: false,
                forceSave: false);
            Scribe_Values.Look<float>(value: ref sanityLossRate, label: "sanityLossRate", defaultValue: 0.03f,
                forceSave: false);
            Scribe_Values.Look<float>(value: ref sanityLossMax, label: "sanityLossMax", defaultValue: 0.3f,
                forceSave: false);
        }

        #region SpawnSetup

        /// <summary>
        /// Check for initial startup.
        /// If Star Vampire, then hide.
        /// </summary>
        /// 
        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map: map, respawningAfterLoad: respawningAfterLoad);

            IsInvisible = PawnExtension.invisible;
            sanityLossRate = PawnExtension.sanityLossRate;
            sanityLossMax = PawnExtension.sanityLossMax;

            //Give immunities.
            health.AddHediff(def: HediffDef.Named(defName: "ROM_CosmicHorrorImmunities"));
        }

        #endregion SpawnSetup

        #region Invisibility

        /// <summary>
        /// Overriding the drawer for the Pawn
        /// </summary>
        /// <param name="drawLoc"></param>
        public override void DrawAt(Vector3 drawLoc, bool flip)
        {
            if (IsInvisible)
            {
                return;
            }

            base.DrawAt(drawLoc: drawLoc, flip: flip);
        }

        public override void Draw()
        {
            if (IsInvisible)
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
                if (IsInvisible)
                {
                    IntVec3 position = Position;
                    Predicate<Thing> predicate2 = delegate(Thing t)
                    {
                        if (t == this)
                        {
                            return false;
                        }

                        Pawn pawn1 = t as Pawn;
                        if (pawn1 == null || !t.Spawned || !PawnExtension.invisible)
                        {
                            return false;
                        }

                        if (pawn1.RaceProps.Animal)
                        {
                            return false;
                        }

                        return true;
                    };
                    Thing thing2 = GenClosest.ClosestThingReachable(root: Position, map: MapHeld,
                        thingReq: ThingRequest.ForGroup(@group: ThingRequestGroup.Pawn), peMode: PathEndMode.OnCell,
                        traverseParams: TraverseParms.For(pawn: this, maxDanger: Danger.Deadly,
                            mode: TraverseMode.PassDoors, canBashDoors: false), maxDistance: 3, validator: predicate2);
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
            if (IsInvisible)
            {
                dinfo.SetAmount(newAmount: 0); //No damage if it's invisible
                absorbed = true;
            }
        }

        #endregion Invisibility

        public override void PreApplyDamage(ref DamageInfo dinfo, out bool absorbed)
        {
            base.PreApplyDamage(dinfo: ref dinfo, absorbed: out absorbed);
            ResolveInvisibility(dinfo: dinfo, inAbsorbed: absorbed, absorbed: out absorbed);

            if (dinfo.Instigator != null)
            {
                if (dinfo.Instigator is Pawn)
                {
                    if (def.defName == "ROM_Chthonian")
                    {
                        if (jobs != null)
                        {
                            if (jobs.curJob.def != JobDefOf.AttackMelee)
                            {
                                Job j = new Job(def: JobDefOf.AttackMelee, targetA: dinfo.Instigator)
                                {
                                    expiryInterval = 1000,
                                    checkOverrideOnExpire = true,
                                    expireRequiresEnemiesNearby = true
                                };
                                jobs.EndCurrentJob(condition: JobCondition.Incompletable);
                                jobs.TryTakeOrderedJob(job: j);
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
            if (CanReveal || Downed || Dead)
            {
                Reveal();
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
                health.hediffSet.hediffs.RemoveAll(match: x => x.def == HediffDef.Named(defName: "Animal_Flu") ||
                                                               x.def == HediffDef.Named(defName: "Animal_Plague") ||
                                                               x.def == HediffDef.Named(defName: "SandInEyes") ||
                                                               x.def == HediffDef.Named(defName: "MudInEyes") ||
                                                               x.def == HediffDef.Named(defName: "GravelInEyes") ||
                                                               x.def == HediffDef.Named(defName: "WaterInEyes") ||
                                                               x.def == HediffDef.Named(defName: "DirtInEyes"));
            }
        }

        public void ResolveBleeding()
        {
            if (PawnExtension != null && PawnExtension.regenInterval != 0 &&
                PawnExtension.regenRate != 0)
            {
                if (Find.TickManager.TicksGame % PawnExtension.regenInterval == PawnExtension.regenRate)
                {
                    if (health != null)
                    {
                        if (health.hediffSet.GetInjuriesTendable() != null &&
                            health.hediffSet.GetInjuriesTendable().Count<Hediff_Injury>() > 0)
                        {
                            foreach (Hediff_Injury injury in health.hediffSet.GetInjuriesTendable())
                            {
                                injury.Severity = Mathf.Clamp(value: injury.Severity - 0.1f, min: 0.0f, max: 1.0f);
                            }
                        }
                    }
                }
            }
        }

        public void DownedCheck()
        {
            if (Downed)
            {
                if (!Utility.IsTameable(kindDef: kindDef))
                {
                    Kill(dinfo: null);
                }
            }
        }

        public void ResolveSpecialEffects()
        {
            Map map = Map;
            IntVec3 intVec = Position;

            if (!Dead && Spawned && pather.Moving)
            {
                if (def == MonsterDefOf.ROM_Chthonian.race)
                {
                    if (movingSound == null)
                    {
                        SoundInfo info = SoundInfo.InMap(maker: this, maint: MaintenanceType.PerTick);
                        movingSound = SoundDef.Named(defName: "Pawn_ROM_Chthonian_Moving")
                            .TrySpawnSustainer(info: info);
                    }

                    if (lastSpot != IntVec3.Invalid)
                    {
                        //Standing still? No smoke needed
                        if (lastSpot != Position)
                        {
                            //Do this... inside the map, obviously.
                            if (intVec.InBounds(map: map))
                            {
                                //Throw some smoke
                                FleckMaker.ThrowSmoke(loc: intVec.ToVector3(), map: map, size: 1f);
                                FleckMaker.ThrowDustPuff(cell: intVec, map: map, scale: 1f);
                                FleckMaker.ThrowDustPuff(cell: lastSpot, map: map, scale: 0.5f);

                                //Break the floor
                                if (map.terrainGrid.TerrainAt(c: intVec).layerable)
                                    map.terrainGrid.RemoveTopLayer(c: intVec, doLeavings: false);

                                TerrainDef richSoil = TerrainDef.Named(defName: "SoilRich");

                                //If it's not sand, change to fertile soil
                                if (map.terrainGrid.TerrainAt(c: intVec).fertility > 0.06 &&
                                    map.terrainGrid.TerrainAt(c: intVec) != richSoil)
                                {
                                    Map.terrainGrid.SetTerrain(c: intVec, newTerr: richSoil);
                                }
                            }
                        }
                    }
                }
            }

            if (movingSound != null)
                movingSound.End();
            lastSpot = Position;
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
            if (IsInvisible)
            {
                IsInvisible = false;
                FleckMaker.ThrowAirPuffUp(loc: GenThing.TrueCenter(t: this), map: MapHeld);
            }

            return;
        }

        /// <summary>
        /// Reveal the hidden creature
        /// </summary>
        public void Hide()
        {
            if (!IsInvisible)
            {
                IsInvisible = true;
            }

            return;
        }

        #endregion Abilities

        public static bool ManhunterMessageSent = false;

        public Predicate<Thing> Predicate => delegate(Thing t)
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
            if (Faction != null && pawn1.Faction != null)
            {
                if (Faction == pawn1.Faction)
                    return false;
                if (!Faction.HostileTo(other: pawn1.Faction))
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
                    thought_MemoryObservation =
                        (Thought_MemoryObservation)ThoughtMaker.MakeThought(
                            def: DefDatabase<ThoughtDef>.GetNamed(defName: "Observed" + def.ToString()));
                    thought_MemoryObservation.Target = this;
                    target.needs.mood.thoughts.memories.TryGainMemory(newThought: thought_MemoryObservation);
                }

                ///This area gives sanity loss, if the witness sees a living cosmic horror.
                Utility.ApplySanityLoss(pawn: target, sanityLoss: sanityLossRate,
                    sanityLossMax: sanityLossMax);
            }
            catch (NullReferenceException)
            {
            }
        }

        public void ObservationEffectDead(Pawn target)
        {
            try
            {
                Corpse ourBody = Corpse;
                if (ourBody == null) return;

                ///This area gives the thought for witnessing a cosmic horror.
                if (this == null) return;
                if (!ourBody.Spawned) return;
                if (ourBody.StoringThing() == null)
                {
                    ThoughtDef defToImplement =
                        DefDatabase<ThoughtDef>.GetNamedSilentFail(defName: "Observed" + def.ToString());
                    if (defToImplement == null) return;
                    Thought_MemoryObservation thought_MemoryObservation;
                    thought_MemoryObservation =
                        (Thought_MemoryObservation)ThoughtMaker.MakeThought(def: defToImplement);
                    thought_MemoryObservation.Target = this;
                    target.needs.mood.thoughts.memories.TryGainMemory(newThought: thought_MemoryObservation);
                }
            }
            catch (NullReferenceException)
            {
            }
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
                Predicate<Thing> predicate = Predicate;

                Thing thing2 = GenClosest.ClosestThingReachable(root: PositionHeld, map: MapHeld,
                    thingReq: ThingRequest.ForGroup(@group: ThingRequestGroup.Pawn), peMode: PathEndMode.OnCell,
                    traverseParams: TraverseParms.For(pawn: this, maxDanger: Danger.Deadly,
                        mode: TraverseMode.PassDoors, canBashDoors: false), maxDistance: 15, validator: predicate);
                if (thing2 != null && thing2.Position != IntVec3.Invalid)
                {
                    if (GenSight.LineOfSight(start: thing2.Position, end: PositionHeld, map: MapHeld))
                    {
                        if (thing2 is Pawn target)
                        {
                            if (target.RaceProps != null)
                            {
                                if (!target.RaceProps.IsMechanoid)
                                {
                                    if (!isInvisible)
                                    {
                                        if (!Dead && MapHeld != null)
                                        {
                                            ObservationEffectLive(target: target);
                                        }
                                        else
                                        {
                                            ObservationEffectDead(target: target);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (NullReferenceException)
            {
            }
        }
    }
}