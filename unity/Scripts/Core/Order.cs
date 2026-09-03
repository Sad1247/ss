namespace BellaNotte.Core
{
    /// <summary>Un ticket en salle : un client, une recette, une patience qui s'epuise.</summary>
    public sealed class Order
    {
        public int Id { get; }
        public string Client { get; }
        public Recipe Recette { get; }
        public float PatienceMax { get; }
        public float Patience { get; private set; }

        public float RatioPatience => PatienceMax <= 0f ? 0f : Patience / PatienceMax;
        public bool EstPerdue => Patience <= 0f;

        public Order(int id, string client, Recipe recette, float patienceMax)
        {
            Id = id; Client = client; Recette = recette;
            PatienceMax = patienceMax; Patience = patienceMax;
        }

        public void Attendre(float deltaTemps)
        {
            Patience -= deltaTemps;
            if (Patience < 0f) Patience = 0f;
        }
    }
}
