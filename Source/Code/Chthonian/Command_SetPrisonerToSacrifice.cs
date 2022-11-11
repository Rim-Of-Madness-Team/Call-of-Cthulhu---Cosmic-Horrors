using RimWorld;
using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace CosmicHorror
{
    public interface IPrisonerToSacrificeSettable
    {
        Map Map { get; }

        Pawn GetPawnToSacrifice();

        void SetPawnToSacrifice(Pawn sacrifice);

        bool CanAcceptSacrificeNow();
    }


    [StaticConstructorOnStartup]
    public class Command_SetPrisonerToSacrifice : Command
    {
        public IPrisonerToSacrificeSettable settable;

        private List<IPrisonerToSacrificeSettable> settables;

        private static readonly Texture2D SetPrisonerToSacrificeTex =
            ContentFinder<Texture2D>.Get(itemPath: "UI/Commands/SetPlantToGrow", reportFailure: true);

        public Command_SetPrisonerToSacrifice()
        {
            //this.tutorTag = "GrowingZoneSetPlant";
            Pawn sacrifice = null;
            bool flag = false;
            foreach (object current in Find.Selector.SelectedObjects)
            {
                IPrisonerToSacrificeSettable prisonerToSacrificeSettable = current as IPrisonerToSacrificeSettable;
                if (prisonerToSacrificeSettable != null)
                {
                    if (sacrifice != null && sacrifice != prisonerToSacrificeSettable.GetPawnToSacrifice())
                    {
                        flag = true;
                        break;
                    }

                    sacrifice = prisonerToSacrificeSettable.GetPawnToSacrifice();
                }
            }

            icon = SetPrisonerToSacrificeTex;
            if (flag)
            {
                defaultLabel = "CommandSelectPlantToGrowMulti".Translate();
            }
            else
            {
                defaultLabel = "CommandSelectPlantToGrow".Translate(args: new object[]
                {
                    sacrifice.Label
                });
            }
        }

        public override void ProcessInput(Event ev)
        {
            base.ProcessInput(ev: ev);
            List<FloatMenuOption> list = new List<FloatMenuOption>();
            if (settables == null)
            {
                settables = new List<IPrisonerToSacrificeSettable>();
            }

            if (!settables.Contains(item: settable))
            {
                settables.Add(item: settable);
            }

            Map map = Find.CurrentMap;
            foreach (Pawn current in map.mapPawns.PrisonersOfColonySpawned)
            {
                if (!current.Dead)
                {
                    string text = current.LabelCap;
                    List<FloatMenuOption> arg_121_0 = list;
                    Func<Rect, bool> extraPartOnGUI = (Rect rect) =>
                        Widgets.InfoCardButton(x: rect.x + 5f, y: rect.y + (rect.height - 24f) / 2f, thing: current);
                    arg_121_0.Add(item: new FloatMenuOption(label: text, action: delegate
                        {
                            string s = tutorTag + "-" + current.Label;
                            if (!TutorSystem.AllowAction(ep: s))
                            {
                                return;
                            }

                            for (int i = 0; i < settables.Count; i++)
                            {
                                settables[index: i].SetPawnToSacrifice(sacrifice: current);
                            }
                        }, priority: MenuOptionPriority.Default, mouseoverGuiAction: null, revalidateClickTarget: null,
                        extraPartWidth: 29f, extraPartOnGUI: extraPartOnGUI, revalidateWorldClickTarget: null));
                }
            }

            Find.WindowStack.Add(window: new FloatMenu(options: list));
        }

        public override bool InheritInteractionsFrom(Gizmo other)
        {
            if (settables == null)
            {
                settables = new List<IPrisonerToSacrificeSettable>();
            }

            settables.Add(item: ((Command_SetPrisonerToSacrifice)other).settable);
            return false;
        }

        private void WarnAsAppropriate(ThingDef plantDef)
        {
            if (plantDef.plant.sowMinSkill > 0)
            {
                foreach (Pawn current in settable.Map.mapPawns.FreeColonistsSpawned)
                {
                    if (current.skills.GetSkill(skillDef: SkillDefOf.Plants).Level >= plantDef.plant.sowMinSkill &&
                        !current.Downed && current.workSettings.WorkIsActive(w: WorkTypeDefOf.Growing))
                    {
                        return;
                    }
                }

                Find.WindowStack.Add(window: new Dialog_MessageBox(text: "NoGrowerCanPlant".Translate(args: new object[]
                    {
                        plantDef.label,
                        plantDef.plant.sowMinSkill
                    }).CapitalizeFirst(), buttonAText: null, buttonAAction: null, buttonBText: null,
                    buttonBAction: null,
                    title: null, buttonADestructive: false));
            }
        }

        private bool IsPlantAvailable(ThingDef plantDef)
        {
            List<ResearchProjectDef> sowResearchPrerequisites = plantDef.plant.sowResearchPrerequisites;
            if (sowResearchPrerequisites == null)
            {
                return true;
            }

            for (int i = 0; i < sowResearchPrerequisites.Count; i++)
            {
                if (!sowResearchPrerequisites[index: i].IsFinished)
                {
                    return false;
                }
            }

            return true;
        }
    }
}