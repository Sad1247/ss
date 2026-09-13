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

        // --- fatigue et locaux du personnel ---
        public const float FatigueMax = 100f;
        /// <summary>Points de fatigue par seconde de travail effectif.</summary>
        public const float FatigueParSeconde = 2f;
        /// <summary>Recuperation de base par seconde de repos, avant bonus de salle.</summary>
        public const float RecuperationParSeconde = 8f;
        /// <summary>Fatigue a partir de laquelle l'employe part se reposer.</summary>
        public const float SeuilDepartRepos = 85f;
        /// <summary>Fatigue en dessous de laquelle il reprend son poste.</summary>
        public const float SeuilFinRepos = 15f;
        /// <summary>A quelle distance du siege on considere l'employe assis.</summary>
        public const float RayonSiege = 0.6f;

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
        /// <summary>Temps qu'un client passe attable, une fois servi.</summary>
        public const float DureeRepas = 9f;
        /// <summary>Plateaux propres empiles sur le plan de mise en boite.</summary>
        public const int PlateauxSurLePlan = 3;
        /// <summary>Plateaux dresses d'avance sur le comptoir.</summary>
        public const int PlateauxAuComptoir = 2;
        /// <summary>Hauteur d'un plateau, qui sert d'ecart dans une pile.</summary>
        public const float EpaisseurPlateau = 0.22f;
        /// <summary>En combien de parts se mange une pizza sur place.</summary>
        public const int QuartiersParPizza = 4;
        /// <summary>De combien il descend pour s'asseoir sur la chaise.</summary>
        public const float HauteurAssise = -0.25f;
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
        /// <summary>
        /// Salaires affiches sur l'ecran du bureau. Ils ne sont pas encore
        /// preleves : la fiche de paie se lit, elle ne se paie pas.
        /// </summary>
        public const int SalairePatron = 0;
        public const int SalaireCaissier = 45;

        /// <summary>L'heure a laquelle la pizzeria ouvre, le premier jour.</summary>
        public const int HeureOuverture = 9;
        /// <summary>Passe cette heure, plus personne ne pousse la porte.</summary>
        public const int HeureFermeture = 21;
        /// <summary>
        /// L'heure a laquelle le caissier prend son sac et rentre chez lui. Une
        /// heure apres la fermeture : il finit de servir ceux qui sont entres.
        /// </summary>
        public const int HeureDepartCaissier = 22;
        /// <summary>
        /// Vitesse de l'horloge : 144 secondes de jeu par seconde reelle,
        /// c'est-a-dire une journee entiere — minuit a minuit — en dix minutes
        /// de manette. L'avance rapide ramene cela a deux minutes et demie.
        /// </summary>
        public const float SecondesParSeconde = 144f;
        /// <summary>La piece qui s'ouvre derriere la porte du fond.</summary>
        public const int PrixPetitePiece = 400;
        /// <summary>La salle de repos du personnel, sa voisine.</summary>
        public const int PrixSalleRepos = 500;
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
