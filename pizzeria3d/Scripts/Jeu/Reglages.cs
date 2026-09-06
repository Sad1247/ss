namespace Pizzeria3D
{
    /// <summary>
    /// Tout l'equilibrage du jeu. Regle : aucune valeur de gameplay ailleurs.
    /// </summary>
    public static class Reglages
    {
        // --- joueur ---
        public const float VitesseJoueur = 4.5f;
        public const float VitesseRotation = 14f;
        public const int CapacitePortee = 8;        // pizzas portees sur la tete
        public const float RayonRamassage = 1.6f;   // distance pour prendre / poser
        public const float RayonJoueur = 0.42f;     // encombrement, pour les collisions
        public const float DelaiTransfert = 0.09f;  // secondes entre deux pizzas

        // --- four ---
        public const float DureeCuisson = 6.2f;     // une pizza toutes les N secondes
        public const int StockFourMax = 12;

        // --- comptoir ---
        public const int StockComptoirMax = 16;

        // --- caissier ---
        public const float VitesseCaissier = 3.3f;
        /// <summary>Ce que le caissier prend au four en un voyage.</summary>
        public const int CapacitePorteeCaissier = 3;
        /// <summary>En dessous, il part chercher des pizzas au four.</summary>
        public const int SeuilRechargeComptoir = 4;

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
        /// <summary>Ils flanent : c'est le personnel qui court, pas la clientele.</summary>
        public const float VitesseClient = 2.1f;
        /// <summary>
        /// Allure de reference de leur demarche. Volontairement plus haute que
        /// leur vitesse reelle : a egalite, le pas part a fond et ils ont l'air
        /// de courir sur place.
        /// </summary>
        public const float AllureClient = 3.6f;

        // --- argent ---
        /// <summary>
        /// MISE AU POINT UNIQUEMENT — argent donne au lancement pour essayer
        /// les achats sans jouer la partie. Remettre a 0 avant publication.
        /// </summary>
        public const int ArgentDepart = 5000;

        public const float RayonRamassageBillet = 1.4f;
        public const float VitesseBillet = 9f;      // vol du billet vers le joueur

        // --- service au comptoir ---
        /// <summary>Distance a laquelle le joueur tient lui-meme la caisse.</summary>
        public const float RayonService = 2.8f;

        // --- zone d'achat ---
        public const int PrixCaissier = 250;
        /// <summary>La piece qui s'ouvre derriere la porte du fond.</summary>
        public const int PrixPetitePiece = 400;
        public const float DebitAchat = 45f;        // euros par seconde en restant dessus

        // --- pile de pizzas ---
/// <summary>Hauteur d'une pizza, qui sert aussi d'ecart dans une pile.</summary>
        public const float EpaisseurPizza = 0.10f;
        /// <summary>Diametre de la pate. Le rapport aux deux fait l'allure de la pizza.</summary>
        public const float DiametrePizza = 0.78f;

        // --- emballage ---
        /// <summary>Cote et hauteur d'une boite : plus haute qu'une pizza nue.</summary>
        public const float LargeurBoite = 0.80f;
        public const float EpaisseurBoite = 0.16f;
        /// <summary>Secondes que met le caissier a mettre une pizza en boite.</summary>
        public const float DelaiEmballage = 0.30f;
        /// <summary>
        /// Cartons en attente sur le plan de travail, en un seul tas pose dans
        /// un coin. Trois, c'est bas : la pile reste sous la hauteur d'epaule.
        /// </summary>
        public const int BoitesEnReserve = 3;
        /// <summary>
        /// Secondes entre deux cartons livres. Court, parce que la rangee tient
        /// moins de cartons que le caissier ne porte de pizzas.
        /// </summary>
        public const float DelaiReappro = 0.5f;
    }
}
