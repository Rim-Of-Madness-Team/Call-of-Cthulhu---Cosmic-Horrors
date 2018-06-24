using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace CosmicHorror
{
    public class FactionTracker : WorldComponent
    {
        private Dictionary<Faction, List<Tile>> scoutedLocations = new Dictionary<Faction, List<Tile>>();
        private List<Faction> scoutedLocationsKeysWorkingList;
        private List<List<Tile>> scoutedLocationsValuesWorkingList;

        public bool HasScoutedTile(Faction fac, Tile checkingTile)
        {
            return scoutedLocations[fac]?.FirstOrDefault(tile => tile == checkingTile) != null;
        }

        public void AddScoutedLocation(Faction fac, Tile scoutedTile)
        {
            if (scoutedLocations[fac] == null)
                scoutedLocations[fac] = new List<Tile>();
            scoutedLocations[fac].Add(scoutedTile);
        }
        
        public FactionTracker(World world) : base(world)
        {
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look<Faction, List<Tile>>(ref this.scoutedLocations, "scoutedLocations", LookMode.Reference,
                LookMode.Deep, ref this.scoutedLocationsKeysWorkingList, ref this.scoutedLocationsValuesWorkingList);
            
        }

        public Faction ResolveHorrorFactionByIncidentPoints(float points)
        {
            List<CosmicHorrorFactionWeight> factionList = new List<CosmicHorrorFactionWeight>
            {
                new CosmicHorrorFactionWeight("ROM_DeepOne", 4)
            };
            if (points > 1400f) factionList.Add(new CosmicHorrorFactionWeight("ROM_StarSpawn", 1));
            if (points > 700f) factionList.Add(new CosmicHorrorFactionWeight("ROM_Shoggoth", 2));
            if (points > 350f) factionList.Add(new CosmicHorrorFactionWeight("ROM_MiGo", 4));
            CosmicHorrorFactionWeight f = GenCollection.RandomElementByWeight<CosmicHorrorFactionWeight>(factionList,
                fac => fac.Weight);
            Faction resolvedFaction = Find.FactionManager.FirstFactionOfDef(FactionDef.Named(f.DefName));

            //This is a special case.
            //If the player has the Cults mod.
            //If they are working with Dagon.
            //Then let's do something different...
            if (Cthulhu.Utility.IsCultsLoaded() && f.DefName == "ROM_DeepOne")
            {
                if (resolvedFaction.RelationWith(Faction.OfPlayer, false).kind != FactionRelationKind.Hostile)
                {
                    //Do MiGo instead.
                    Cthulhu.Utility.DebugReport("Cosmic Horror Raid Report: Special Cult Case Handled");
                    resolvedFaction = Find.FactionManager.FirstFactionOfDef(FactionDef.Named("ROM_MiGo"));
                }
            }
            //this.attackingFaction = resolvedFaction.def;
            Cthulhu.Utility.DebugReport("Cosmic Horror Raid Report: " + resolvedFaction.def.ToString() + "selected");
            return resolvedFaction;
        }

        
    }
}