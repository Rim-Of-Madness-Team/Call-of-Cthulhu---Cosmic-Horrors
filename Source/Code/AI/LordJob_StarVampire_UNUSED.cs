using Verse;
using Verse.AI.Group;

namespace RimWorld
{
    public class LordJob_StarVampire_UNUSED : LordJob
    {
        private Faction faction;

        private IntVec3 stageLoc;

        private int ticksUntilAssault = Rand.Range(min: 3000, max: 6000);

        public LordJob_StarVampire_UNUSED()
        {
        }

        public LordJob_StarVampire_UNUSED(Faction faction, IntVec3 stageLoc)
        {
            this.faction = faction;
            this.stageLoc = stageLoc;
        }

        public override StateGraph CreateGraph()
        {
            StateGraph stateGraph = new StateGraph();
            LordToil_Stage lordToil_Stage = new LordToil_Stage(stagingLoc: stageLoc);
            stateGraph.StartingToil = lordToil_Stage;
            LordToil startingToil = stateGraph
                .AttachSubgraph(subGraph: new LordJob_AssaultColony(assaulterFaction: faction, canKidnap: true,
                    canTimeoutOrFlee: true, sappers: false, useAvoidGridSmart: false, canSteal: true).CreateGraph())
                .StartingToil;
            Transition transition = new Transition(firstSource: lordToil_Stage, target: startingToil);
            transition.AddTrigger(trigger: new Trigger_TicksPassed(tickLimit: ticksUntilAssault));
            transition.AddTrigger(trigger: new Trigger_FractionPawnsLost(fraction: 0.3f));
            transition.AddPreAction(action: new TransitionAction_WakeAll());
            stateGraph.AddTransition(transition: transition);
            return stateGraph;
        }

        public override void ExposeData()
        {
            Scribe_References.Look<Faction>(refee: ref faction, label: "faction", saveDestroyedThings: false);
            Scribe_Values.Look<IntVec3>(value: ref stageLoc, label: "stageLoc", defaultValue: default(IntVec3),
                forceSave: false);
        }
    }
}