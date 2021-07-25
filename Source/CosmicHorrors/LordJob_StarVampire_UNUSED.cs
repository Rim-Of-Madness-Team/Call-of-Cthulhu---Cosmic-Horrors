using Verse;
using Verse.AI.Group;

namespace RimWorld
{
    public class LordJob_StarVampire_UNUSED : LordJob
    {
        private Faction faction;

        private IntVec3 stageLoc;

        private int ticksUntilAssault = Rand.Range(3000, 6000);

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
            LordToil_Stage lordToil_Stage = new LordToil_Stage(this.stageLoc);
            stateGraph.StartingToil = lordToil_Stage;
            LordToil startingToil = stateGraph.AttachSubgraph(new LordJob_AssaultColony(this.faction, true, true, false, false, true).CreateGraph()).StartingToil;
            Transition transition = new Transition(lordToil_Stage, startingToil);
            transition.AddTrigger(new Trigger_TicksPassed(this.ticksUntilAssault));
            transition.AddTrigger(new Trigger_FractionPawnsLost(0.3f));
            transition.AddPreAction(new TransitionAction_WakeAll());
            stateGraph.AddTransition(transition);
            return stateGraph;
        }

        public override void ExposeData()
        {
            Scribe_References.Look<Faction>(ref this.faction, "faction", false);
            Scribe_Values.Look<IntVec3>(ref this.stageLoc, "stageLoc", default(IntVec3), false);
        }
    }
}
