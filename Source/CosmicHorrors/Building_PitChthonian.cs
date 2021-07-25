using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.AI.Group;
using Verse.Sound;
using RimWorld;
using System.Reflection;

namespace CosmicHorror
{
    public class Building_PitChthonian : Building, IThingHolder
    {
        protected int age;
        
        private bool isActive = true;
        private bool isSacrificing = false;
        private bool isFilling = false;
        private bool gaveSacrifice = false;

        private Lord lord = null;
        private CosmicHorrorPawn spawnedChthonian = null;

        private int ticksToSanityLoss = -999;
        private int ticksToDeSpawn = -999;
        private float sanityLossRange = 30f;
        private int ticksToReturn = -999;
        private int rareTicks = 250;
        private static HashSet<IntVec3> reachableCells = new HashSet<IntVec3>();

        #region Container Values
        protected ThingOwner container;

        public void GetChildHolders(List<IThingHolder> outChildren) => ThingOwnerUtility.AppendThingHoldersFromThings(outChildren, this.GetDirectlyHeldThings());

        public ThingOwner GetDirectlyHeldThings() => this.container;

        #endregion Container Values

        public bool GaveSacrifice
        {
            get => this.gaveSacrifice;
            set
            {
                if (this.gaveSacrifice != value)
                {
                    if (!this.gaveSacrifice && value)
                    {
                        Messages.Message("ChthonianPitActivityStopped".Translate(), MessageTypeDefOf.SituationResolved);
                    }
                }
                this.gaveSacrifice = value;
            }
        }
        
        public bool IsSacrificing
        {
            get => this.isSacrificing;
            set => this.isSacrificing = value;
        }
        
        public bool IsFilling
        {
            get => this.isFilling;
            set => this.isFilling = value;
        }

        public bool IsActive
        {
            get => this.isActive;
            set
            {
                if (this.isActive == value)
                {
                    this.isActive = value;
                }
                else
                {
                    if (this.isActive && value == false)
                    {
                        Messages.Message("ChthonianPitActivityStopped".Translate(), MessageTypeDefOf.SituationResolved);
                        Sustainer sustainer = (Sustainer)typeof(Building).GetField("sustainerAmbient", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(this);
                        sustainer.End();
                        this.isActive = value;
                    }
                    else
                    {
                        Messages.Message("ChthonianPitActivityStarted".Translate(), MessageTypeDefOf.SituationResolved);
                        this.isActive = value;
                    }
                }
            }
        }

        protected float SanityLossRange => this.sanityLossRange;
        protected int SanityLossInterval => Mathf.Clamp(Mathf.RoundToInt(4f - 0.6f * this.age / 60000f), 2, 4);

        public Building_PitChthonian()
        {
            this.container = new ThingOwner<Thing>(this, false, LookMode.Deep);
            this.rareTicks = 250;
        }


        public void ProcessInput()
        {
            if (!this.isSacrificing)
            {
                List<FloatMenuOption> list = new List<FloatMenuOption>();
                Map map = this.Map;
                List<Pawn> prisoners = map.mapPawns.PrisonersOfColonySpawned;
                if (prisoners.Count != 0)
                {
                    foreach (Pawn current in map.mapPawns.PrisonersOfColonySpawned)
                    {
                        if (!current.Dead)
                        {
                            string text = current.Name.ToStringFull;
                            List<FloatMenuOption> arg_121_0 = list;
                            Func<Rect, bool> extraPartOnGUI = (Rect rect) => Widgets.InfoCardButton(rect.x + 5f, rect.y + (rect.height - 24f) / 2f, current);
                            arg_121_0.Add(new FloatMenuOption(text, delegate
                            {
                                this.TrySacrificePrisoner(current);
                            }, MenuOptionPriority.Default, null, null, 29f, extraPartOnGUI, null));
                        }
                    }
                }
                else
                {
                    list.Add(new FloatMenuOption("NoPrisoners".Translate(), delegate
                    {
                    }, MenuOptionPriority.Default));
                }
                Find.WindowStack.Add(new FloatMenu(list));
            }
            else
            {
                TryCancelSacrifice();
            }
        }

        private void TryCancelSacrifice(string reason ="")
        {
            Pawn pawn = null;
            List<Pawn> listeners = this.Map.mapPawns.AllPawnsSpawned.FindAll(x => x.RaceProps.intelligence == Intelligence.Humanlike);
            bool[] flag = new bool[listeners.Count];
            for (int i = 0; i < listeners.Count; i++)
            {
                pawn = listeners[i];
                if (pawn.Faction == Faction.OfPlayer)
                {
                    if (pawn.CurJob.def == MonsterDefOf.ROM_HaulChthonianSacrifice)
                    {
                        pawn.jobs.StopAll();
                    }
                }
            }
            this.isSacrificing = false;
            Messages.Message("Cancelling sacrifice. " + reason, MessageTypeDefOf.NegativeEvent);
        }
        private void StartSacrifice(Pawn executioner, Pawn sacrifice)
        {
            if (this.Destroyed || !this.Spawned)
            {
                TryCancelSacrifice("The altar is unavailable.");
                return;
            }
            if (!Cthulhu.Utility.IsActorAvailable(executioner))
            {
                TryCancelSacrifice("The executioner is unavailable.");
                return;
            }
            if (!Cthulhu.Utility.IsActorAvailable(sacrifice, true))
            {
                TryCancelSacrifice("The sacrifice, " + sacrifice.LabelShort + " is unavaialable.");
                return;
            }

            Messages.Message("A sacrifice is starting.", TargetInfo.Invalid, MessageTypeDefOf.SituationResolved);
            this.isSacrificing = true;
            
            Cthulhu.Utility.DebugReport("Force Sacrifice called");
            Job job = new Job(MonsterDefOf.ROM_HaulChthonianSacrifice, sacrifice, this)
            {
                count = 1
            };
            executioner.jobs.TryTakeOrderedJob(job);
            //executioner.QueueJob(job);
            //executioner.jobs.EndCurrentJob(JobCondition.InterruptForced);

            Cthulhu.Utility.DebugReport("Sacrifice state set to gathering");
        }
        private void TrySacrificePrisoner(Pawn prisoner)
        {
            Pawn executioner = null;

            //Try to find an executioner.
            foreach (Pawn current in this.Map.mapPawns.FreeColonistsSpawned)
            {
                if (!current.Dead)
                {
                    if (current.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation) &&
                      current.health.capacities.CapableOf(PawnCapacityDefOf.Moving))
                    {
                        if (Cthulhu.Utility.IsActorAvailable(current))
                        {
                            executioner = current;
                            break;
                        }
                    }
                }
            }

            if (executioner != null)
            {
                StartSacrifice(executioner, prisoner);
            }
            else
            {
                Messages.Message("Cannot find executioner to carry out sacrifice", MessageTypeDefOf.RejectInput);
            }
        }
        private void TryReturnSacrifice()
        {
            if (this.container.Count != 0)
            {
                IntVec3 intVec = this.RandomAdjacentCell8Way();
                Pawn pawn = null;
                Pawn toRemove = null;
                foreach (Pawn t in this.container)
                {
                    if (toRemove == null && t.kindDef.defName == "ROM_Chthonian")
                    {
                        toRemove = t;
                    }
                    pawn = t;
                }
                if (toRemove != null) { this.container.Remove(toRemove); toRemove = null; }
                if (pawn == null) return;

                this.container.TryDrop(pawn, ThingPlaceMode.Near, out Thing temp);

                Hediff wormsHediff = HediffMaker.MakeHediff(DefDatabase<HediffDef>.GetNamed("ROM_GutWorms"), pawn, null);
                wormsHediff.Part = pawn.health.hediffSet.GetBrain();
                wormsHediff.Severity = 0.05f;
                pawn.health.AddHediff(wormsHediff, null, null);

                Building_PitChthonian.GiveInjuriesToForceDowned(pawn);

                Find.LetterStack.ReceiveLetter("ChthonianSacrificeReturnedLabel".Translate(), "ChthonianSacrificeReturnedDesc".Translate(), LetterDefOf.ThreatSmall, new TargetInfo(pawn), null);
                //TaleRecorder.RecordTale(TaleDefOf.RaidArrived, new object[0]);

            }
        }

        public void CheckStatus()
        {

            if (this.gaveSacrifice)
            {
                if (this.container.Count != 0)
                {
                    if (this.ticksToReturn == -999)
                    {
                        int ran = Rand.Range(1, 2);
                        this.ticksToReturn = Find.TickManager.TicksGame + (GenDate.TicksPerDay * ran);
                    }
                    if (this.ticksToReturn < Find.TickManager.TicksGame)
                    {
                        TryReturnSacrifice();
                    }
                    //Cthulhu.Utility.DebugReport("returnedTicks :: " + ticksToReturn.ToString() + " :: gameTicks :: " + Find.TickManager.TicksGame.ToString());
                }
            }
            if (this.spawnedChthonian != null && this.isActive)
            {
                if (this.spawnedChthonian.needs != null)
                {
                    this.spawnedChthonian.needs.food.CurLevelPercentage = 0.1f;
                    this.spawnedChthonian.needs.rest.CurLevelPercentage = 1f; // ForceSetLevel(1f);
                }
            }

            bool flag1 = false;
            bool flag2 = false;
            foreach (Pawn current in this.Map.mapPawns.FreeColonistsSpawned)
            {
                if (current.CurJob.def == MonsterDefOf.ROM_HaulChthonianSacrifice)
                {
                    flag1 = true;
                }
                if (current.CurJob.def == MonsterDefOf.ROM_FillChthonianPit)
                {
                    flag2 = true;
                }
            }
            this.isSacrificing = flag1;
            this.isFilling = flag2;
        }

        public void TryReturnChthonian()
        {
            if (this.spawnedChthonian != null)
            {
                if (this.spawnedChthonian.Map == null) return;
                if (this.spawnedChthonian.Dead) return;
                if (this.spawnedChthonian.Downed) return;
                if (this.spawnedChthonian.ParentHolder == this.container) return;
            

                if (this.ticksToDeSpawn == -999)
                    this.ticksToDeSpawn = 16000;
                if (GenAI.InDangerousCombat(this.spawnedChthonian) || GenAI.EnemyIsNear(this.spawnedChthonian, 5f))
                {
                    this.ticksToDeSpawn += 10;
                }
                this.ticksToDeSpawn--;
                if (this.ticksToDeSpawn < 0)
                {
                    this.spawnedChthonian.DeSpawn();
                    this.container.TryAdd(this.spawnedChthonian);
                    this.IsActive = true;
                }
            }
        }
        
        public void TrySpawnChthonian()
        {
            PawnKindDef kindDef = PawnKindDef.Named("ROM_Chthonian");
            Faction pawnFaction = Find.FactionManager.FirstFactionOfDef(kindDef.defaultFactionType);
            if (this.lord == null)
            {
                if (!CellFinder.TryFindRandomCellNear(this.Position, this.Map, 5, (IntVec3 c) => c.Standable(this.Map) && this.Map.reachability.CanReach(c, this, PathEndMode.Touch, TraverseParms.For(TraverseMode.PassDoors, Danger.Deadly, false)), out IntVec3 invalid))
                {
                    Cthulhu.Utility.ErrorReport("Found no place for the Chthonian to spawn " + this);
                    invalid = IntVec3.Invalid;
                }
                LordJob_DefendPoint lordJob = new LordJob_DefendPoint(this.Position);
                this.lord = LordMaker.MakeNewLord(pawnFaction, lordJob, this.Map, null);
            }


            if (this.spawnedChthonian == null)
            {
                if ((from cell in GenAdj.CellsAdjacent8Way(this)
                     where cell.Walkable(Map)
                     select cell).TryRandomElement(out IntVec3 center))
                {
                    Pawn pawn = PawnGenerator.GeneratePawn(kindDef, pawnFaction);
                    if (GenPlace.TryPlaceThing(pawn, center, this.Map, ThingPlaceMode.Near, null))
                    {
                        this.spawnedChthonian = (CosmicHorrorPawn)pawn;
                        this.lord.AddPawn(pawn);
                        this.isActive = false;
                    }
                    //Find.WorldPawns.PassToWorld(pawn, PawnDiscardDecideMode.Discard);
                }
                if (this.Map == Find.CurrentMap)
                {
                    SoundDef.Named("Pawn_ROM_Chthonian_Scream").PlayOneShotOnCamera();
                }
                return;
            }
            else
            {
                if (!this.spawnedChthonian.Dead && this.spawnedChthonian.ParentHolder == this.container)
                {
                    this.container.TryDrop(this.spawnedChthonian, this.Position.RandomAdjacentCell8Way(), this.Map, ThingPlaceMode.Near, out Thing temp);
                    if (!this.lord.ownedPawns.Contains(this.spawnedChthonian)) this.lord.AddPawn(this.spawnedChthonian);
                    this.isActive = false;
                    this.ticksToDeSpawn += 16000;
                }
            }
        }
        
        private void GiveSanityLoss()
        {
            if (this.SanityLossRange < 0.0001f)
            {
                return;
            }
            float angle = Rand.Range(0f, 360f);
            float num = Rand.Range(0f, this.SanityLossRange);
            num = Mathf.Sqrt(num / this.SanityLossRange) * this.SanityLossRange;
            Quaternion rotation = Quaternion.AngleAxis(angle, Vector3.up);
            Vector3 point = Vector3.forward * num;
            Vector3 v = rotation * point;
            IntVec3 b = IntVec3.FromVector3(v);
            IntVec3 c = this.Position + b;
            if (this.Map == null) return;
            if (c.InBounds(this.Map))
            {
                Pawn victim = c.GetFirstPawn(this.Map);
                if (victim != null)
                {
                    Cthulhu.Utility.ApplySanityLoss(victim, 0.1f);
                }
            }
            this.ticksToSanityLoss = this.SanityLossInterval;
        }


        private void TryCancelFillHole(string reason = "")
        {
            Pawn pawn = null;
            List<Pawn> listeners = this.Map.mapPawns.AllPawnsSpawned.FindAll(x => x.RaceProps.intelligence == Intelligence.Humanlike);
            bool[] flag = new bool[listeners.Count];
            for (int i = 0; i < listeners.Count; i++)
            {
                pawn = listeners[i];
                if (pawn.Faction == Faction.OfPlayer)
                {
                    if (pawn.CurJob.def == MonsterDefOf.ROM_FillChthonianPit)
                    {
                        pawn.jobs.StopAll();
                    }
                }
            }
            this.isSacrificing = false;
            Messages.Message("Cancelling filling the hole. " + reason, MessageTypeDefOf.NegativeEvent);
        }

        private void TryCancelFillHole()
        {
            Pawn pawn = null;
            List<Pawn> listeners = this.Map.mapPawns.AllPawnsSpawned.FindAll(x => x.RaceProps.intelligence == Intelligence.Humanlike);
            bool[] flag = new bool[listeners.Count];
            for (int i = 0; i < listeners.Count; i++)
            {
                pawn = listeners[i];
                if (pawn.Faction == Faction.OfPlayer)
                {
                    if (pawn.CurJob.def == MonsterDefOf.ROM_FillChthonianPit)
                    {
                        pawn.jobs.StopAll();
                    }
                }
            }
            this.isSacrificing = false;
            Messages.Message("Cancelling filling the hole.", MessageTypeDefOf.NegativeEvent);
        }

        private void StartFillHole(Pawn actor)
        {
            if (this.Destroyed || !this.Spawned)
            {
                TryCancelFillHole("The altar is unavailable.");
                return;
            }
            if (!Cthulhu.Utility.IsActorAvailable(actor))
            {
                TryCancelFillHole("The acting colonist is unavailable.");
                return;
            }

            Messages.Message(actor.LabelShort + " is going to fill the pit.", TargetInfo.Invalid, MessageTypeDefOf.SituationResolved);
            this.isFilling = true;

            Cthulhu.Utility.DebugReport("Force Sacrifice called");
            Job job = new Job(MonsterDefOf.ROM_FillChthonianPit, this);
            actor.jobs.TryTakeOrderedJob(job);
            //actor.QueueJob(job);
            //actor.jobs.EndCurrentJob(JobCondition.InterruptForced);

            Cthulhu.Utility.DebugReport("Actor is going to fill the Chthonian pit.");

        }

        private void TryFillHole()
        {

            Pawn actor = null;

            //Try to find an actor.
            foreach (Pawn current in this.Map.mapPawns.FreeColonistsSpawned)
            {
                if (!current.Dead)
                {
                    if (current.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation) &&
                        current.health.capacities.CapableOf(PawnCapacityDefOf.Moving))
                    {
                        if (Cthulhu.Utility.IsActorAvailable(current))
                        {
                            actor = current;
                            break;
                        }
                    }
                }
            }

            if (actor != null)
            {
                StartFillHole(actor);
            }
            else
            {
                Log.Error("Cannot find actor to carry out sacrifice");
            }
            
        }

        // Verse.HealthUtility
        private static IEnumerable<BodyPartRecord> HittablePartsViolence(HediffSet bodyModel) => from x in bodyModel.GetNotMissingParts(BodyPartHeight.Undefined, BodyPartDepth.Undefined)
                                                                                                 where x.depth == BodyPartDepth.Outside || (x.depth == BodyPartDepth.Inside && x.def.IsSolid(x, bodyModel.hediffs))
                                                                                                 select x;


        // Verse.HealthUtility
        public static void GiveInjuriesToForceDowned(Pawn p)
        {
            if (p.health.Downed)
            {
                return;
            }
            HediffSet hediffSet = p.health.hediffSet;
            p.health.forceIncap = true;
            int num = 0;
            while (num < 300 && !p.Downed && Building_PitChthonian.HittablePartsViolence(hediffSet).Any<BodyPartRecord>())
            {
                num++;
                BodyPartRecord bodyPartRecord = Building_PitChthonian.HittablePartsViolence(hediffSet).RandomElementByWeight((BodyPartRecord x) => x.coverageAbs);
                int num2 = Mathf.RoundToInt(hediffSet.GetPartHealth(bodyPartRecord)) - 3;
                if (num2 >= 8)
                {
                    DamageDef def;
                    if (bodyPartRecord.depth == BodyPartDepth.Outside)
                    {
                        def = DamageDefOf.Bite;
                    }
                    else
                    {
                        def = DamageDefOf.Blunt;
                    }
                    int amount = Rand.RangeInclusive(Mathf.RoundToInt(num2 * 0.65f), num2);
                    BodyPartRecord forceHitPart = bodyPartRecord;
                    DamageInfo dinfo = new DamageInfo(def, amount, 1f, -1f, null, forceHitPart, null);
                    dinfo.SetAllowDamagePropagation(false);
                    p.TakeDamage(dinfo);
                }
            }
            if (p.Dead)
            {
                StringBuilder stringBuilder = new StringBuilder();
                stringBuilder.AppendLine(p + " died during GiveInjuriesToForceDowned");
                for (int i = 0; i < p.health.hediffSet.hediffs.Count; i++)
                {
                    stringBuilder.AppendLine("   -" + p.health.hediffSet.hediffs[i].ToString());
                }
                Log.Error(stringBuilder.ToString());
            }
            p.health.forceIncap = false;
        }


        #region Overrides

        public override void Tick()
        {
            base.Tick();
            this.age++;
            this.ticksToSanityLoss--;
            if (this.ticksToSanityLoss <= 0)
            {
                this.GiveSanityLoss();
            }
            this.rareTicks--;
            if (this.rareTicks < 0)
            {
                this.rareTicks = 250;
                CheckStatus();
            }
            TryReturnChthonian();
        }

        public override void PreApplyDamage(ref DamageInfo dinfo, out bool absorbed)
        {

            base.PreApplyDamage(ref dinfo, out absorbed);
            if (!this.isActive)
            {
                return;
            }

            if (absorbed)
            {
                return;
            }
            if (dinfo.Def.harmsHealth)
            {
                float num = this.HitPoints - dinfo.Amount;
                if ((num < this.MaxHitPoints * 0.98f && dinfo.Instigator != null && dinfo.Instigator.Faction != null) || num < this.MaxHitPoints * 0.9f)
                {
                    this.TrySpawnChthonian();
                }
            }
            absorbed = false;
        }


        public override void DrawExtraSelectionOverlays()
        {
            float range = this.sanityLossRange;
            if (range < 90f && this.isActive)
            {
                GenDraw.DrawRadiusRing(this.Position, range);
            }
        }


        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look<bool>(ref this.isActive, "isActive", true, false);
            Scribe_Values.Look<bool>(ref this.isFilling, "isFilling", true, false);
            Scribe_Values.Look<bool>(ref this.gaveSacrifice, "gaveSacrificing", false, false);
            Scribe_Values.Look<bool>(ref this.isSacrificing, "isSacrificing", true, false);
            Scribe_Values.Look<int>(ref this.age, "age", 0, false);
            Scribe_Values.Look<int>(ref this.rareTicks, "rareTicks", 250, false);
            Scribe_Values.Look<int>(ref this.ticksToReturn, "ticksToReturn", -999, false);
            Scribe_Values.Look<int>(ref this.ticksToDeSpawn, "ticksToDeSpawn", -999, false);
            Scribe_References.Look<Lord>(ref this.lord, "defenseLord", false);
            Scribe_References.Look<CosmicHorrorPawn>(ref this.spawnedChthonian, "spawnedChthonian", false);
            Scribe_Deep.Look<ThingOwner>(ref this.container, "container", new object[]
            {
                this
            });

            //if (Scribe.mode == LoadSaveMode.PostLoadInit)
            //{
            //    if (!this.isActive)
            //    {
            //        Sustainer sustainer = (Sustainer)typeof(Building).GetField("sustainerAmbient", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(this);
            //        sustainer.End();
            //    }
            //}
        }

        public override string GetInspectString()
        {
            StringBuilder stringBuilder = new StringBuilder();
            if (base.GetInspectString() != "") stringBuilder.Append(base.GetInspectString());
            stringBuilder.AppendLine("DiscoveredDaysAgo".Translate(new object[]
            {
                this.age.TicksToDays().ToString("F1")
            }));
            if (this.isActive) stringBuilder.AppendLine("CausingSanityLoss".Translate());
            else stringBuilder.AppendLine("NotCausingSanityLoss".Translate());
            return stringBuilder.ToString().TrimEndNewlines();
        }



        public override IEnumerable<Gizmo> GetGizmos()
        {
            IEnumerator<Gizmo> enumerator = base.GetGizmos().GetEnumerator();
            while (enumerator.MoveNext())
            {
                Gizmo current = enumerator.Current;
                yield return current;
            }

            if (this.isActive)
            {
                if (!this.isSacrificing && !this.isFilling)
                {
                    Command_Action command_Action = new Command_Action()
                    {
                        action = new Action(this.ProcessInput),
                        defaultLabel = "CommandPitSacrifice".Translate(),
                        defaultDesc = "CommandPitSacrificeDesc".Translate(),
                        hotKey = KeyBindingDefOf.Misc1,
                        icon = ContentFinder<Texture2D>.Get("UI/Commands/ForPrisoners", true)
                    };
                    yield return command_Action;
                }
                else
                {
                    Command_Action command_Cancel = new Command_Action()
                    {
                        action = new Action(this.ProcessInput),
                        defaultLabel = "CommandCancelConstructionLabel".Translate(),
                        defaultDesc = "CommandCancelPitSacrificeDesc".Translate(),
                        hotKey = KeyBindingDefOf.Designator_Cancel,
                        icon = ContentFinder<Texture2D>.Get("UI/Designators/Cancel", true)
                    };
                    yield return command_Cancel;
                }
            }
            if (!this.isFilling && !this.isSacrificing)
            {

                Command_Action command_FillHole = new Command_Action()
                {
                    action = new Action(this.TryFillHole),
                    defaultLabel = "CommandFillHole".Translate()
                };
                if (this.isActive) command_FillHole.defaultDesc = "CommandFillHoleActiveDesc".Translate();
                else command_FillHole.defaultDesc = "CommandFillHoleDesc".Translate();
                command_FillHole.hotKey = KeyBindingDefOf.Misc1;
                command_FillHole.icon = ContentFinder<Texture2D>.Get("Ui/Icons/FillHole", true);
                yield return command_FillHole;
            }
            else
            {
                Command_Action command_Cancel = new Command_Action()
                {
                    action = new Action(this.TryCancelFillHole),
                    defaultLabel = "CommandCancelConstructionLabel".Translate(),
                    defaultDesc = "CommandCancelFillHoleDesc".Translate(),
                    hotKey = KeyBindingDefOf.Designator_Cancel,
                    icon = ContentFinder<Texture2D>.Get("UI/Designators/Cancel", true)
                };
                yield return command_Cancel;
            }
        }
        #endregion Overrides
    }
}
