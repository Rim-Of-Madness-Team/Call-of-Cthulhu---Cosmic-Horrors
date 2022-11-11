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

        public ThingOwner container;

        public void GetChildHolders(List<IThingHolder> outChildren) =>
            ThingOwnerUtility.AppendThingHoldersFromThings(outThingsHolders: outChildren,
                container: GetDirectlyHeldThings());

        public ThingOwner GetDirectlyHeldThings() => container;

        #endregion Container Values

        public bool GaveSacrifice
        {
            get => gaveSacrifice;
            set
            {
                if (gaveSacrifice != value)
                {
                    if (!gaveSacrifice && value)
                    {
                        Messages.Message(text: "ChthonianPitActivityStopped".Translate(),
                            def: MessageTypeDefOf.SituationResolved);
                    }
                }

                gaveSacrifice = value;
            }
        }

        public bool IsSacrificing
        {
            get => isSacrificing;
            set => isSacrificing = value;
        }

        public bool IsFilling
        {
            get => isFilling;
            set => isFilling = value;
        }

        public bool IsActive
        {
            get => isActive;
            set
            {
                if (isActive == value)
                {
                    isActive = value;
                }
                else
                {
                    if (isActive && value == false)
                    {
                        Messages.Message(text: "ChthonianPitActivityStopped".Translate(),
                            def: MessageTypeDefOf.SituationResolved);
                        Sustainer sustainer = (Sustainer)typeof(Building).GetField(name: "sustainerAmbient",
                            bindingAttr: BindingFlags.Instance | BindingFlags.NonPublic).GetValue(obj: this);
                        sustainer.End();
                        isActive = value;
                    }
                    else
                    {
                        Messages.Message(text: "ChthonianPitActivityStarted".Translate(),
                            def: MessageTypeDefOf.SituationResolved);
                        isActive = value;
                    }
                }
            }
        }

        protected float SanityLossRange => IsActive ? sanityLossRange : 0f;

        protected int SanityLossInterval =>
            Mathf.Clamp(value: Mathf.RoundToInt(f: 4f - 0.6f * age / 60000f), min: 2, max: 4);

        public Building_PitChthonian()
        {
            container = new ThingOwner<Thing>(owner: this, oneStackOnly: false, contentsLookMode: LookMode.Deep);
            rareTicks = 250;
        }


        public void ProcessInput()
        {
            if (!isSacrificing)
            {
                List<FloatMenuOption> list = new List<FloatMenuOption>();
                Map map = Map;
                List<Pawn> prisoners = map.mapPawns.PrisonersOfColonySpawned;
                if (prisoners.Count != 0)
                {
                    foreach (Pawn current in map.mapPawns.PrisonersOfColonySpawned)
                    {
                        if (!current.Dead)
                        {
                            string text = current.Name.ToStringFull;
                            List<FloatMenuOption> arg_121_0 = list;
                            Func<Rect, bool> extraPartOnGUI = (Rect rect) =>
                                Widgets.InfoCardButton(x: rect.x + 5f, y: rect.y + (rect.height - 24f) / 2f,
                                    thing: current);
                            arg_121_0.Add(item: new FloatMenuOption(label: text,
                                action: delegate { TrySacrificePrisoner(prisoner: current); },
                                priority: MenuOptionPriority.Default, mouseoverGuiAction: null,
                                revalidateClickTarget: null, extraPartWidth: 29f, extraPartOnGUI: extraPartOnGUI,
                                revalidateWorldClickTarget: null));
                        }
                    }
                }
                else
                {
                    list.Add(item: new FloatMenuOption(label: "NoPrisoners".Translate(), action: delegate { },
                        priority: MenuOptionPriority.Default));
                }

                Find.WindowStack.Add(window: new FloatMenu(options: list));
            }
            else
            {
                TryCancelSacrifice();
            }
        }

        private void TryCancelSacrifice(string reason = "")
        {
            Pawn pawn = null;
            List<Pawn> listeners =
                Map.mapPawns.AllPawnsSpawned.FindAll(
                    match: x => x.RaceProps.intelligence == Intelligence.Humanlike);
            bool[] flag = new bool[listeners.Count];
            foreach (var t in listeners)
            {
                pawn = t;
                if (pawn.Faction == Faction.OfPlayer)
                {
                    if (pawn.CurJob.def == MonsterDefOf.ROM_HaulChthonianSacrifice)
                    {
                        pawn.jobs.StopAll();
                    }
                }
            }

            isSacrificing = false;
            Messages.Message(text: "ROMCH_CancellingSacrifice".Translate(reason), def: MessageTypeDefOf.NegativeEvent);
        }

        private void StartSacrifice(Pawn executioner, Pawn sacrifice)
        {
            if (Destroyed || !Spawned)
            {
                TryCancelSacrifice(reason: "ROMCH_CancellingSacrificePitUnavailable".Translate());
                return;
            }

            if (!Utility.IsActorAvailable(preacher: executioner))
            {
                TryCancelSacrifice(reason: "ROMCH_CancellingSacrificeExecutionerUnavailable".Translate());
                return;
            }

            if (!Utility.IsActorAvailable(preacher: sacrifice, downedAllowed: true))
            {
                TryCancelSacrifice(reason: "ROMCH_CancellingSacrificeSacrificeUnavailable".Translate(sacrifice.LabelShort));
                return;
            }

            Messages.Message(text: "ROMCH_SacrificeStarting".Translate(), lookTargets: TargetInfo.Invalid,
                def: MessageTypeDefOf.SituationResolved);
            isSacrificing = true;

            Job job = new Job(def: MonsterDefOf.ROM_HaulChthonianSacrifice, targetA: sacrifice, targetB: this)
            {
                count = 1
            };
            executioner.jobs.TryTakeOrderedJob(job: job);
        }

        private void TrySacrificePrisoner(Pawn prisoner)
        {
            Pawn executioner = null;

            //Try to find an executioner.
            foreach (Pawn current in Map.mapPawns.FreeColonistsSpawned)
            {
                if (!current.Dead)
                {
                    if (current.health.capacities.CapableOf(capacity: PawnCapacityDefOf.Manipulation) &&
                        current.health.capacities.CapableOf(capacity: PawnCapacityDefOf.Moving))
                    {
                        if (Utility.IsActorAvailable(preacher: current))
                        {
                            executioner = current;
                            break;
                        }
                    }
                }
            }

            if (executioner != null)
            {
                StartSacrifice(executioner: executioner, sacrifice: prisoner);
            }
            else
            {
                Messages.Message(text: "Cannot find executioner to carry out sacrifice",
                    def: MessageTypeDefOf.RejectInput);
            }
        }

        private void TryReturnSacrifice()
        {
            if (container.Count != 0)
            {
                IntVec3 intVec = this.RandomAdjacentCell8Way();
                Pawn pawn = null;
                Pawn toRemove = null;
                foreach (Pawn t in container)
                {
                    if (toRemove == null && t.kindDef.defName == "ROM_Chthonian")
                    {
                        toRemove = t;
                    }

                    pawn = t;
                }

                if (toRemove != null)
                {
                    container.Remove(item: toRemove);
                    toRemove = null;
                }

                if (pawn == null) return;

                container.TryDrop(thing: pawn, mode: ThingPlaceMode.Near, lastResultingThing: out Thing temp);

                Hediff wormsHediff = HediffMaker.MakeHediff(
                    def: DefDatabase<HediffDef>.GetNamed(defName: "ROM_GutWorms"), pawn: pawn, partRecord: null);
                wormsHediff.Part = pawn.health.hediffSet.GetBrain();
                wormsHediff.Severity = 0.05f;
                pawn.health.AddHediff(hediff: wormsHediff, part: null, dinfo: null);

                GiveInjuriesToForceDowned(p: pawn);

                Find.LetterStack.ReceiveLetter(label: "ChthonianSacrificeReturnedLabel".Translate(),
                    text: "ChthonianSacrificeReturnedDesc".Translate(), textLetterDef: LetterDefOf.ThreatSmall,
                    lookTargets: new TargetInfo(thing: pawn), relatedFaction: null);
                //TaleRecorder.RecordTale(TaleDefOf.RaidArrived, new object[0]);
            }
        }

        public void CheckStatus()
        {
            if (gaveSacrifice)
            {
                if (container.Count != 0)
                {
                    if (ticksToReturn == -999)
                    {
                        int ran = Rand.Range(min: 1, max: 2);
                        ticksToReturn = Find.TickManager.TicksGame + (GenDate.TicksPerDay * ran);
                    }

                    if (ticksToReturn < Find.TickManager.TicksGame)
                    {
                        TryReturnSacrifice();
                    }
                    //Utility.DebugReport("returnedTicks :: " + ticksToReturn.ToString() + " :: gameTicks :: " + Find.TickManager.TicksGame.ToString());
                }
            }

            if (spawnedChthonian != null && isActive)
            {
                if (spawnedChthonian.needs != null)
                {
                    spawnedChthonian.needs.food.CurLevelPercentage = 0.1f;
                    spawnedChthonian.needs.rest.CurLevelPercentage = 1f; // ForceSetLevel(1f);
                }
            }

            bool flag1 = false;
            bool flag2 = false;
            foreach (Pawn current in Map.mapPawns.FreeColonistsSpawned)
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

            isSacrificing = flag1;
            isFilling = flag2;
        }

        public void TryReturnChthonian()
        {
            if (spawnedChthonian != null)
            {
                if (spawnedChthonian.Map == null) return;
                if (spawnedChthonian.Dead) return;
                if (spawnedChthonian.Downed) return;
                if (spawnedChthonian.ParentHolder == container) return;


                if (ticksToDeSpawn == -999)
                    ticksToDeSpawn = 16000;
                if (GenAI.InDangerousCombat(pawn: spawnedChthonian) ||
                    GenAI.EnemyIsNear(p: spawnedChthonian, radius: 5f))
                {
                    ticksToDeSpawn += 10;
                }

                ticksToDeSpawn--;
                if (ticksToDeSpawn < 0)
                {
                    spawnedChthonian.DeSpawn();
                    container.TryAdd(item: spawnedChthonian);
                    IsActive = true;
                }
            }
        }

        public void TrySpawnChthonian()
        {
            PawnKindDef kindDef = PawnKindDef.Named(defName: "ROM_Chthonian");
            Faction pawnFaction = Find.FactionManager.FirstFactionOfDef(facDef: kindDef.defaultFactionType);
            if (lord == null)
            {
                if (!CellFinder.TryFindRandomCellNear(root: Position, map: Map, squareRadius: 5,
                        validator: (IntVec3 c) => c.Standable(map: Map) && Map.reachability.CanReach(start: c,
                            dest: this, peMode: PathEndMode.Touch,
                            traverseParams: TraverseParms.For(mode: TraverseMode.PassDoors, maxDanger: Danger.Deadly,
                                canBashDoors: false)), result: out IntVec3 invalid))
                {
                    Utility.ErrorReport(x: "Found no place for the Chthonian to spawn " + this);
                    invalid = IntVec3.Invalid;
                }

                LordJob_DefendPoint lordJob = new LordJob_DefendPoint(point: Position);
                lord = LordMaker.MakeNewLord(faction: pawnFaction, lordJob: lordJob, map: Map,
                    startingPawns: null);
            }


            if (spawnedChthonian == null)
            {
                if ((from cell in GenAdj.CellsAdjacent8Way(t: this)
                        where cell.Walkable(map: Map)
                        select cell).TryRandomElement(result: out IntVec3 center))
                {
                    Pawn pawn = PawnGenerator.GeneratePawn(kindDef: kindDef, faction: pawnFaction);
                    if (GenPlace.TryPlaceThing(thing: pawn, center: center, map: Map, mode: ThingPlaceMode.Near,
                            placedAction: null))
                    {
                        spawnedChthonian = (CosmicHorrorPawn)pawn;
                        lord.AddPawn(p: pawn);
                        isActive = false;
                    }
                    //Find.WorldPawns.PassToWorld(pawn, PawnDiscardDecideMode.Discard);
                }

                if (Map == Find.CurrentMap)
                {
                    SoundDef.Named(defName: "Pawn_ROM_Chthonian_Scream").PlayOneShotOnCamera();
                }

                return;
            }

            if (spawnedChthonian.Dead || spawnedChthonian.ParentHolder != container) return;
            container.TryDrop(thing: spawnedChthonian,
                dropLoc: Position.RandomAdjacentCell8Way(), map: Map, mode: ThingPlaceMode.Near,
                lastResultingThing: out Thing temp);
            if (!lord.ownedPawns.Contains(item: spawnedChthonian))
                lord.AddPawn(p: spawnedChthonian);
            isActive = false;
            ticksToDeSpawn += 16000;
        }

        private void GiveSanityLoss()
        {
            if (SanityLossRange < 0.0001f)
            {
                return;
            }

            float angle = Rand.Range(min: 0f, max: 360f);
            float num = Rand.Range(min: 0f, max: SanityLossRange);
            num = Mathf.Sqrt(f: num / SanityLossRange) * SanityLossRange;
            Quaternion rotation = Quaternion.AngleAxis(angle: angle, axis: Vector3.up);
            Vector3 point = Vector3.forward * num;
            Vector3 v = rotation * point;
            IntVec3 b = IntVec3.FromVector3(v: v);
            IntVec3 c = Position + b;
            if (Map == null) return;
            if (c.InBounds(map: Map))
            {
                Pawn victim = c.GetFirstPawn(map: Map);
                if (victim != null)
                {
                    Utility.ApplySanityLoss(pawn: victim, sanityLoss: 0.1f);
                }
            }

            ticksToSanityLoss = SanityLossInterval;
        }


        private void TryCancelFillHole(string reason = "")
        {
            Pawn pawn = null;
            List<Pawn> listeners =
                Map.mapPawns.AllPawnsSpawned.FindAll(
                    match: x => x.RaceProps.intelligence == Intelligence.Humanlike);
            bool[] flag = new bool[listeners.Count];
            for (int i = 0; i < listeners.Count; i++)
            {
                pawn = listeners[index: i];
                if (pawn.Faction == Faction.OfPlayer)
                {
                    if (pawn.CurJob.def == MonsterDefOf.ROM_FillChthonianPit)
                    {
                        pawn.jobs.StopAll();
                    }
                }
            }

            isSacrificing = false;
            Messages.Message(text: "ROMCH_CancellingFilling".Translate(reason), def: MessageTypeDefOf.NegativeEvent);
        }

        private void TryCancelFillHole()
        {
            Pawn pawn = null;
            List<Pawn> listeners =
                Map.mapPawns.AllPawnsSpawned.FindAll(
                    match: x => x.RaceProps.intelligence == Intelligence.Humanlike);
            bool[] flag = new bool[listeners.Count];
            for (int i = 0; i < listeners.Count; i++)
            {
                pawn = listeners[index: i];
                if (pawn.Faction == Faction.OfPlayer)
                {
                    if (pawn.CurJob.def == MonsterDefOf.ROM_FillChthonianPit)
                    {
                        pawn.jobs.StopAll();
                    }
                }
            }

            isSacrificing = false;
            Messages.Message(text: "ROMCH_CancellingFilling".Translate(""), def: MessageTypeDefOf.NegativeEvent);
        }

        private void StartFillHole(Pawn actor)
        {
            if (Destroyed || !Spawned)
            {
                TryCancelFillHole(reason: "ROMCH_CancellingSacrificePitUnavailable".Translate());
                return;
            }
            if (!Utility.IsActorAvailable(preacher: actor))
            {
                TryCancelFillHole(reason: "ROMCH_CancellingFillingNoColonist".Translate());
                return;
            }
            Messages.Message(text: "ROMCH_FillingPit".Translate(actor.LabelShort), lookTargets: TargetInfo.Invalid,
                def: MessageTypeDefOf.SituationResolved);
            isFilling = true;
            Job job = new Job(def: MonsterDefOf.ROM_FillChthonianPit, targetA: this);
            actor.jobs.TryTakeOrderedJob(job: job);
        }

        private void TryFillHole()
        {
            Pawn actor = null;

            foreach (Pawn current in Map.mapPawns.FreeColonistsSpawned)
            {
                if (!current.Dead)
                {
                    if (current.health.capacities.CapableOf(capacity: PawnCapacityDefOf.Manipulation) &&
                        current.health.capacities.CapableOf(capacity: PawnCapacityDefOf.Moving))
                    {
                        if (Utility.IsActorAvailable(preacher: current))
                        {
                            actor = current;
                            break;
                        }
                    }
                }
            }

            if (actor == null)
            {
                Log.Error(text: "Cannot find actor to carry out sacrifice");
            }
            else
            {
                StartFillHole(actor: actor);
            }
        }

        private static IEnumerable<BodyPartRecord> HittablePartsViolence(HediffSet bodyModel) =>
            from x in bodyModel.GetNotMissingParts(height: BodyPartHeight.Undefined, depth: BodyPartDepth.Undefined)
            where x.depth == BodyPartDepth.Outside ||
                  (x.depth == BodyPartDepth.Inside && x.def.IsSolid(part: x, hediffs: bodyModel.hediffs))
            select x;


        // Verse.HealthUtility
        public static void GiveInjuriesToForceDowned(Pawn p)
        {
            if (p.health.Downed)
            {
                return;
            }

            HediffSet hediffSet = p.health.hediffSet;
            p.health.forceDowned = true;
            int num = 0;
            while (num < 300 && !p.Downed &&
                   HittablePartsViolence(bodyModel: hediffSet).Any<BodyPartRecord>())
            {
                num++;
                BodyPartRecord bodyPartRecord = HittablePartsViolence(bodyModel: hediffSet)
                    .RandomElementByWeight(weightSelector: (BodyPartRecord x) => x.coverageAbs);
                int num2 = Mathf.RoundToInt(f: hediffSet.GetPartHealth(part: bodyPartRecord)) - 3;
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

                    int amount = Rand.RangeInclusive(min: Mathf.RoundToInt(f: num2 * 0.65f), max: num2);
                    BodyPartRecord forceHitPart = bodyPartRecord;
                    DamageInfo dinfo = new DamageInfo(def: def, amount: amount, armorPenetration: 1f, angle: -1f,
                        instigator: null, hitPart: forceHitPart, weapon: null);
                    dinfo.SetAllowDamagePropagation(val: false);
                    p.TakeDamage(dinfo: dinfo);
                }
            }

            if (p.Dead)
            {
                StringBuilder stringBuilder = new StringBuilder();
                stringBuilder.AppendLine(value: p + " died during GiveInjuriesToForceDowned");
                for (int i = 0; i < p.health.hediffSet.hediffs.Count; i++)
                {
                    stringBuilder.AppendLine(value: "   -" + p.health.hediffSet.hediffs[index: i].ToString());
                }

                Log.Error(text: stringBuilder.ToString());
            }

            p.health.forceDowned = false;
        }


        #region Overrides

        public override void Tick()
        {
            base.Tick();
            age++;
            ticksToSanityLoss--;
            if (ticksToSanityLoss <= 0)
            {
                GiveSanityLoss();
            }

            rareTicks--;
            if (rareTicks < 0)
            {
                rareTicks = 250;
                CheckStatus();
            }

            TryReturnChthonian();
        }

        public override void PreApplyDamage(ref DamageInfo dinfo, out bool absorbed)
        {
            base.PreApplyDamage(dinfo: ref dinfo, absorbed: out absorbed);
            if (!isActive)
            {
                return;
            }

            if (absorbed)
            {
                return;
            }

            if (dinfo.Def.harmsHealth)
            {
                float num = HitPoints - dinfo.Amount;
                if ((num < MaxHitPoints * 0.98f && dinfo.Instigator != null && dinfo.Instigator.Faction != null) ||
                    num < MaxHitPoints * 0.9f)
                {
                    TrySpawnChthonian();
                }
            }

            absorbed = false;
        }


        public override void DrawExtraSelectionOverlays()
        {
            float range = sanityLossRange;
            if (range < 90f && isActive)
            {
                GenDraw.DrawRadiusRing(center: Position, radius: range);
            }
        }


        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look<bool>(value: ref isActive, label: "isActive", defaultValue: true, forceSave: false);
            Scribe_Values.Look<bool>(value: ref isFilling, label: "isFilling", defaultValue: true,
                forceSave: false);
            Scribe_Values.Look<bool>(value: ref gaveSacrifice, label: "gaveSacrificing", defaultValue: false,
                forceSave: false);
            Scribe_Values.Look<bool>(value: ref isSacrificing, label: "isSacrificing", defaultValue: true,
                forceSave: false);
            Scribe_Values.Look<int>(value: ref age, label: "age", defaultValue: 0, forceSave: false);
            Scribe_Values.Look<int>(value: ref rareTicks, label: "rareTicks", defaultValue: 250, forceSave: false);
            Scribe_Values.Look<int>(value: ref ticksToReturn, label: "ticksToReturn", defaultValue: -999,
                forceSave: false);
            Scribe_Values.Look<int>(value: ref ticksToDeSpawn, label: "ticksToDeSpawn", defaultValue: -999,
                forceSave: false);
            Scribe_References.Look<Lord>(refee: ref lord, label: "defenseLord", saveDestroyedThings: false);
            Scribe_References.Look<CosmicHorrorPawn>(refee: ref spawnedChthonian, label: "spawnedChthonian",
                saveDestroyedThings: false);
            Scribe_Deep.Look<ThingOwner>(target: ref container, label: "container", ctorArgs: new object[]
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
            if (base.GetInspectString() != "") stringBuilder.Append(value: base.GetInspectString());
            stringBuilder.AppendLine(value: "DiscoveredDaysAgo".Translate(args: new object[]
            {
                age.TicksToDays().ToString(format: "F1")
            }));
            if (isActive) stringBuilder.AppendLine(value: "CausingSanityLoss".Translate());
            else stringBuilder.AppendLine(value: "NotCausingSanityLoss".Translate());
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

            if (isActive)
            {
                if (!isSacrificing && !isFilling)
                {
                    Command_Action command_Action = new Command_Action()
                    {
                        action = new Action(ProcessInput),
                        defaultLabel = "CommandPitSacrifice".Translate(),
                        defaultDesc = "CommandPitSacrificeDesc".Translate(),
                        hotKey = KeyBindingDefOf.Misc1,
                        icon = ContentFinder<Texture2D>.Get(itemPath: "UI/Commands/ForPrisoners", reportFailure: true)
                    };
                    yield return command_Action;
                }
                else
                {
                    Command_Action command_Cancel = new Command_Action()
                    {
                        action = new Action(ProcessInput),
                        defaultLabel = "CommandCancelConstructionLabel".Translate(),
                        defaultDesc = "CommandCancelPitSacrificeDesc".Translate(),
                        hotKey = KeyBindingDefOf.Designator_Cancel,
                        icon = ContentFinder<Texture2D>.Get(itemPath: "UI/Designators/Cancel", reportFailure: true)
                    };
                    yield return command_Cancel;
                }
            }

            if (!isFilling && !isSacrificing)
            {
                Command_Action command_FillHole = new Command_Action()
                {
                    action = new Action(TryFillHole),
                    defaultLabel = "CommandFillHole".Translate()
                };
                if (isActive) command_FillHole.defaultDesc = "CommandFillHoleActiveDesc".Translate();
                else command_FillHole.defaultDesc = "CommandFillHoleDesc".Translate();
                command_FillHole.hotKey = KeyBindingDefOf.Misc1;
                command_FillHole.icon =
                    ContentFinder<Texture2D>.Get(itemPath: "Ui/Icons/FillHole", reportFailure: true);
                yield return command_FillHole;
            }
            else
            {
                Command_Action command_Cancel = new Command_Action()
                {
                    action = new Action(TryCancelFillHole),
                    defaultLabel = "CommandCancelConstructionLabel".Translate(),
                    defaultDesc = "CommandCancelFillHoleDesc".Translate(),
                    hotKey = KeyBindingDefOf.Designator_Cancel,
                    icon = ContentFinder<Texture2D>.Get(itemPath: "UI/Designators/Cancel", reportFailure: true)
                };
                yield return command_Cancel;
            }
        }

        #endregion Overrides
    }
}