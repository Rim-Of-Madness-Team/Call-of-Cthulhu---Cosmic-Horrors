namespace CosmicHorror
{
    public class CosmicHorrorWeight
    {
        public string DefName { get; set; }
        public float Weight { get; set; }


        public CosmicHorrorWeight(string newName, float newWeight)
        {
            DefName = newName;
            Weight = newWeight;
        }
    }
}