using RimWorld;
using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace CosmicHorror
{
    public interface IPrisonerToSacrificeSettable
    {
        Map Map
        {
            get;
        }

        Pawn GetPawnToSacrifice();

        void SetPawnToSacrifice(Pawn sacrifice);

        bool CanAcceptSacrificeNow();
    }


    [StaticConstructorOnStartup]
    public class Command_SetPrisonerToSacrifice : Command
    {
        public IPrisonerToSacrificeSettable settable;

        private List<IPrisonerToSacrificeSettable> settables;

        private static readonly Texture2D SetPrisonerToSacrificeTex = ContentFinder<Texture2D>.Get("UI/Commands/SetPlantToGrow", true);

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
            this.icon = Command_SetPrisonerToSacrifice.SetPrisonerToSacrificeTex;
            if (flag)
            {
                this.defaultLabel = "CommandSelectPlantToGrowMulti".Translate();
            }
            else
            {
                this.defaultLabel = "CommandSelectPlantToGrow".Translate(new object[]
                {
                    thingDef.label
                });
            }
        }

        public override void ProcessInput(Event ev)
        {
            base.ProcessInput(ev);
            List<FloatMenuOption> list = new List<FloatMenuOption>();
            if (this.settables == null)
            {
                this.settables = new List<IPrisonerToSacrificeSettable>();
            }
            if (!this.settables.Contains(this.settable))
            {
                this.settables.Add(this.settable);
            }
            Map map = Find.CurrentMap;
            foreach (Pawn current in map.mapPawns.PrisonersOfColonySpawned)
            {
                if (!current.Dead)
                {
                    string text = current.LabelCap;
                    List<FloatMenuOption> arg_121_0 = list;
                    Func<Rect, bool> extraPartOnGUI = (Rect rect) => Widgets.InfoCardButton(rect.x + 5f, rect.y + (rect.height - 24f) / 2f, localPlantDef);
                    arg_121_0.Add(new FloatMenuOption(text, delegate
                    {
                        string s = this.tutorTag + "-" + localPlantDef.defName;
                        if (!TutorSystem.AllowAction(s))
                        {
                            return;
                        }
                        for (int i = 0; i < this.settables.Count; i++)
                        {
                            this.settables[i].SetPlantDefToGrow(localPlantDef);
                        }
                        PlayerKnowledgeDatabase.KnowledgeDemonstrated(ConceptDefOf.SetGrowingZonePlant, KnowledgeAmount.Total);
                        this.WarnAsAppropriate(localPlantDef);
                        TutorSystem.Notify_Event(s);
                    }, MenuOptionPriority.Default, null, null, 29f, extraPartOnGUI, null));
                }
            }
            Find.WindowStack.Add(new FloatMenu(list));
        }

        public override bool InheritInteractionsFrom(Gizmo other)
        {
            if (this.settables == null)
            {
                this.settables = new List<IPlantToGrowSettable>();
            }
            this.settables.Add(((Command_SetPlantToGrow)other).settable);
            return false;
        }

        private void WarnAsAppropriate(ThingDef plantDef)
        {
            if (plantDef.plant.sowMinSkill > 0)
            {
                foreach (Pawn current in this.settable.Map.mapPawns.FreeColonistsSpawned)
                {
                    if (current.skills.GetSkill(SkillDefOf.Plants).Level >= plantDef.plant.sowMinSkill && !current.Downed && current.workSettings.WorkIsActive(WorkTypeDefOf.Growing))
                    {
                        return;
                    }
                }
                Find.WindowStack.Add(new Dialog_MessageBox("NoGrowerCanPlant".Translate(new object[]
                {
                    plantDef.label,
                    plantDef.plant.sowMinSkill
                }).CapitalizeFirst(), null, null, null, null, null, false));
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
                if (!sowResearchPrerequisites[i].IsFinished)
                {
                    return false;
                }
            }
            return true;
        }
    }
}
