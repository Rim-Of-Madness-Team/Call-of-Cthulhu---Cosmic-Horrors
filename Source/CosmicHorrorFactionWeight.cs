namespace CosmicHorror
{
    public class CosmicHorrorFactionWeight
    {
        public string DefName { get; set; }
        public float Weight { get; set; }


        public CosmicHorrorFactionWeight(string newName, float newWeight)
        {
            this.DefName = newName;
            this.Weight = newWeight;
        }
    }
}