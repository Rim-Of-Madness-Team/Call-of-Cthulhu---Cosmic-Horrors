using Verse;

namespace CosmicHorror
{
    class HediffWithComps_SanityLoss : HediffWithComps
    {
        public override void PostTick()
        {
            base.PostTick();

            if (this.pawn != null)
            {
                if (this.pawn.RaceProps != null)
                {
                    if (this.pawn.RaceProps.IsMechanoid ||
                        base.pawn is CosmicHorrorPawn)
                    {
                        MakeSane();
                    }
                }
            }
        }

        public void MakeSane()
        {
            this.Severity -= 1f;
            base.pawn.health.Notify_HediffChanged(this);
        }
    }
}
