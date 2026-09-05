namespace Pizzeria3D
{
    /// <summary>
    /// Tout l'equilibrage du jeu. Regle : aucune valeur de gameplay ailleurs.
    /// </summary>
    public static class Reglages
    {
        // --- joueur ---
        public const float VitesseJoueur = 6.5f;
        public const float VitesseRotation = 14f;
        public const int CapacitePortee = 8;        // pizzas portees sur la tete
        public const float RayonRamassage = 1.6f;   // distance pour prendre / poser
        public const float RayonJoueur = 0.42f;     // encombrement, pour les collisions
        public const float DelaiTransfert = 0.09f;  // secondes entre deux pizzas

        // --- four ---
        public const float DureeCuisson = 7f;       // une pizza toutes les N secondes
        public const int StockFourMax = 12;

        // --- comptoir ---
        public const int StockComptoirMax = 16;

        // --- clients ---
// Une pizza toutes les 7 secondes, c'est lent : sans espacer les arrivees
        // dans la meme proportion, la file deborde et tous les clients partent.
        public const float DelaiClient = 9f;
        public const int FileMax = 5;
        public const int PizzasParClientMin = 1;
        public const int PizzasParClientMax = 3;
        public const int PrixPizza = 6;
        public const float PatienceClient = 30f;
        /// <summary>Temps que met un client a passer commande, tiroir ouvert.</summary>
        public const float DureeCommande = 3f;
        public const float VitesseClient = 3.2f;

        // --- argent ---
        public const float RayonRamassageBillet = 1.4f;
        public const float VitesseBillet = 9f;      // vol du billet vers le joueur

        // --- service au comptoir ---
        /// <summary>Distance a laquelle le joueur tient lui-meme la caisse.</summary>
        public const float RayonService = 2.8f;

        // --- zone d'achat ---
        public const int PrixCaissier = 250;
        public const float DebitAchat = 45f;        // euros par seconde en restant dessus

        // --- pile de pizzas ---
        public const float EpaisseurPizza = 0.20f;   // pate plus haute, croute plus epaisse
        public const float HauteurTete = 1.82f;   // juste au-dessus de la casquette
    }
}
