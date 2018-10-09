using Verse;

namespace CosmicHorror
{
    public class PawnExtension : DefModExtension
    {
        public bool invisible = false;
        public bool huntAlert = true;
        public float regenRate = 0.0f;
        public int regenInterval = 0;
        public float sanityLossRate = 0.03f;
        public float sanityLossMax = 0.3f;
        public float painFactor = 1.0f;
        public bool generateApparel = false;
    }
}
