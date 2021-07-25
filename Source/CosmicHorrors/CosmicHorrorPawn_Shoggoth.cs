using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace CosmicHorror
{
    public class CosmicHorrorPawn_Shoggoth : CosmicHorrorPawn
    {
        List<Pawn> parts = new List<Pawn>();

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
        }

        public override void ExposeData()
        {
            base.ExposeData();
        }
    }
}
