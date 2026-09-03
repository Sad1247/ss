namespace BellaNotte.Core
{
    /// <summary>
    /// Toutes les constantes d'equilibrage, reprises du prototype HTML.
    /// Regle : aucune valeur numerique de gameplay ailleurs que dans ce fichier.
    /// </summary>
    public static class GameConfig
    {
        // --- economie ---
        public const int PrixDeBase = 6;
        public const int PrixParGarniture = 2;
        public const float RatioPourboire = 0.5f;   // bonus si la pizza est parfaite

        // --- penalites de composition (retirees du ratio de paiement) ---
        public const float PenaliteManquant = 0.28f;
        public const float PenaliteEnTrop = 0.18f;
        public const float PenaliteMauvaiseBase = 0.35f;

        // --- cuisson ---
        /// <summary>Duree reelle pour atteindre une cuisson de 1.0.</summary>
        public const float DureeCuisson = 4.2f;
        public const float ZoneParfaiteMin = 0.92f;
        public const float ZoneParfaiteMax = 1.14f;
        public const float SeuilCrue = 0.60f;       // en dessous : invendable
        public const float SeuilCramee = 1.45f;     // au dessus : invendable
        public const float CuissonMax = 1.60f;      // sortie automatique, pizza perdue

        public const float ScoreCuissonParfaite = 1.0f;
        public const float ScoreCuissonPale = 0.6f;
        public const float ScoreCuissonTropCuite = 0.5f;

        // --- clients ---
        public const int MaxCommandesEnAttente = 4;
        public const int MaxGarnituresParPizza = 5;
        public const int ClientsPerdusMax = 3;

        public static float PatienceMax(int niveau) => Max(22f, 52f - niveau * 3f);
        public static float DelaiEntreClients(int niveau) => Max(3.2f, 9f - niveau * 0.7f);
        public static int PizzasPourNiveauSuivant(int niveau) => 4 + niveau;

        static float Max(float a, float b) => a > b ? a : b;
    }
}
