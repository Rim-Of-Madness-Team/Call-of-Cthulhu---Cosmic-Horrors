using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using JecsTools;
using Verse;
using UnityEngine;
using Verse.AI;
using RimWorld;

namespace CosmicHorror
{
    public class CosmicHorrorFloatMenuPatch : FloatMenuPatch
    {
        public override IEnumerable<KeyValuePair<_Condition, Func<Vector3, Pawn, Thing, List<FloatMenuOption>>>>
            GetFloatMenus()
        {
            List<KeyValuePair<_Condition, Func<Vector3, Pawn, Thing, List<FloatMenuOption>>>> floatMenus =
                new List<KeyValuePair<_Condition, Func<Vector3, Pawn, Thing, List<FloatMenuOption>>>>();

            _Condition madnessCondition = new _Condition(condition: _ConditionType.IsType, data: typeof(Pawn));
            Func<Vector3, Pawn, Thing, List<FloatMenuOption>> madnessFunc =
                delegate(Vector3 clickPos, Pawn pawn, Thing curThing)
                {
                    if (!Utility.IsCultsLoaded())
                    {
                        List<FloatMenuOption> opts = null;
                        Pawn target = curThing as Pawn;
                        if (pawn == target)
                        {
                            if (Utility.HasSanityLoss(pawn: pawn))
                            {
                                opts = new List<FloatMenuOption>();
                                Action action = delegate
                                {
                                    var newMentalState = (Rand.Value > 0.05)
                                        ? DefDatabase<MentalStateDef>.AllDefs.InRandomOrder()
                                            .FirstOrDefault(predicate: x => x.IsAggro == false)
                                        : MentalStateDefOf.Berserk;
                                    Utility.DebugReport(x: "Selected mental state: " + newMentalState.label);
                                    if (pawn.Drafted) pawn.drafter.Drafted = false;
                                    pawn.ClearMind();
                                    pawn.pather.StopDead();
                                    if (!pawn.mindState.mentalStateHandler
                                            .TryStartMentalState(stateDef: newMentalState))
                                    {
                                        Messages.Message(
                                            text: "ROM_TradedSanityLossForMadnessFailed".Translate(
                                                arg1: pawn.LabelShort), lookTargets: pawn,
                                            def: MessageTypeDefOf.RejectInput);
                                        return;
                                    }

                                    Messages.Message(
                                        text: "ROM_TradedSanityLossForMadness".Translate(arg1: pawn.LabelShort),
                                        lookTargets: pawn,
                                        def: MessageTypeDefOf.ThreatSmall);
                                    Utility.RemoveSanityLoss(pawn: pawn);
                                };
                                opts.Add(item: new FloatMenuOption(label: "ROM_TradeSanityForMadness".Translate(),
                                    action: action,
                                    priority: MenuOptionPriority.High, mouseoverGuiAction: null,
                                    revalidateClickTarget: target, extraPartWidth: 0f, extraPartOnGUI: null,
                                    revalidateWorldClickTarget: null));
                                return opts;
                            }
                        }
                    }

                    return null;
                };
            KeyValuePair<_Condition, Func<Vector3, Pawn, Thing, List<FloatMenuOption>>> curSec =
                new KeyValuePair<_Condition, Func<Vector3, Pawn, Thing, List<FloatMenuOption>>>(key: madnessCondition,
                    value: madnessFunc);
            floatMenus.Add(item: curSec);
            return floatMenus;
        }
    }
}