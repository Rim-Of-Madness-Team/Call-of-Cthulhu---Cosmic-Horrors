using Verse;

namespace CosmicHorror
{
    class HediffWithComps_SanityLoss : HediffWithComps
    {
        public override void PostTick()
        {
            base.PostTick();

            if (pawn != null)
            {
                if (pawn.RaceProps != null)
                {
                    if (pawn.RaceProps.IsMechanoid ||
                        pawn is CosmicHorrorPawn)
                    {
                        MakeSane();
                    }
                }
            }
        }

        public void MakeSane()
        {
            Severity -= 1f;
            pawn.health.Notify_HediffChanged(hediff: this);
        }
    }
}