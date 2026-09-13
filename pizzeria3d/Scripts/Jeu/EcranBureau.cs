using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Pizzeria3D
{
    /// <summary>
    /// L'ecran de l'ordinateur du bureau : un vrai terminal de gestion, pas
    /// une fenetre decorative. Une barre laterale mene a six onglets — vue
    /// d'ensemble, personnel, menu, restaurant, finances, statistiques — et
    /// chaque chiffre qui s'y affiche vient d'un systeme reel du jeu : la
    /// Banque, la Comptabilite, le registre du Personnel, le caissier lui
    /// meme. Un bouton embauche ou licencie pour de bon ; un autre ameliore
    /// vraiment le four. S'affiche par-dessus le jeu des que le patron
    /// s'assoit et ouvre le capot ; se ferme d'un vrai bouton, ou en se
    /// levant.
    /// </summary>
    public sealed class EcranBureau : MonoBehaviour
    {
        // les teintes de la distribution : aubergine, orange, gris Yaru
        static readonly Color Aubergine = Bloc.Couleur(0x2C001E);
        static readonly Color DockSombre = Bloc.Couleur(0x1D0413);
        static readonly Color Orange = Bloc.Couleur(0xE95420);
        static readonly Color OrangeSombre = Bloc.Couleur(0xB33E15);
        static readonly Color Entete = Bloc.Couleur(0x303030);
        static readonly Color Papier = Bloc.Couleur(0xFAFAFA);
        static readonly Color Encre = Bloc.Couleur(0x3D3D3D);
        static readonly Color Filet = Bloc.Couleur(0xDCDCDC);
        static readonly Color Vert = Bloc.Couleur(0x0E8420);
        static readonly Color Rouge = Bloc.Couleur(0xC0392B);
        static readonly Color Gris = Bloc.Couleur(0x8A8A8A);
        static readonly Color BoutonActif = Bloc.Couleur(0x4A2338);
        static readonly Color BoutonRepos = Bloc.Couleur(0x2C001E);

        public Joueur Joueur;
        /// <summary>Le portable du bureau : l'ecran suit son capot.</summary>
        public OrdinateurPortable Portable;
        /// <summary>Le caissier : l'onglet Personnel l'embauche et le licencie pour de bon.</summary>
        public Caissier Caissier;
        /// <summary>
        /// La dalle qu'on paie en marchant dessus pour l'embaucher. Le bouton
        /// du bureau paie la meme embauche instantanement ; l'une des deux
        /// facons de payer doit effacer l'autre, sinon la seconde repaierait.
        /// </summary>
        public ZoneAchat DalleEmbauche;
        /// <summary>La salle de repos : l'onglet Locaux y lit places et occupation.</summary>
        public SalleDeRepos SalleRepos;

        GameObject _ecran;
        Text[] _statuts;
        Text[] _productivites;
        Text _masse, _pendule, _titre;
        Text _texteEmploi;
        Button _boutonEmploi;
        Font _police;

        readonly System.Collections.Generic.Dictionary<string, GameObject> _pages
            = new System.Collections.Generic.Dictionary<string, GameObject>();
        readonly System.Collections.Generic.Dictionary<string, Image> _fondsOnglets
            = new System.Collections.Generic.Dictionary<string, Image>();
        string _onglet = "Apercu";
        bool _fermeDeForce;

        // --- textes des pages, rafraichis a chaque image ---
        Text _txArgent, _txRevenuJour, _txDepenseJour, _txBenefice, _txClients, _txSatisfaction;
        Text _txPrixPizza, _txVentesMenu;
        Text _txFour, _txBoutonFour;
        Text _txRevenuTotal, _txDepenseTotal, _txBeneficeTotal, _txSalaires;
        Text[] _txHistorique;
        Text _txClientsServis, _txPizzasVendues, _txArgentGagne, _txAttente, _txSatisfStats;
        Button _boutonAmeliorerFour;
        Text _txNiveauRH, _txBoutonRH, _txNiveauRepos, _txPlacesRepos, _txBoutonRepos, _txBureauxEmployes;
        Button _boutonAmeliorerRH, _boutonAmeliorerRepos;

        /// <summary>Vrai quand l'ecran est affiche par-dessus le jeu.</summary>
        public bool Ouvert => _ecran != null && _ecran.activeSelf;

        /// <summary>L'onglet actuellement affiche.</summary>
        public string Onglet => _onglet;

        /// <summary>Combien de lignes de personnel sont affichees.</summary>
        public int Lignes => _statuts != null ? _statuts.Length : 0;

        /// <summary>Le statut affiche sur la ligne demandee.</summary>
        public string Statut(int ligne)
            => _statuts != null && ligne >= 0 && ligne < _statuts.Length
               ? _statuts[ligne].text : null;

        /// <summary>La productivite affichee sur la ligne demandee.</summary>
        public string Productivite(int ligne)
            => _productivites != null && ligne >= 0 && ligne < _productivites.Length
               ? _productivites[ligne].text : null;

        /// <summary>
        /// La valeur affichee sous une cle : « Argent », « RevenuJour »,
        /// « Benefice », « Clients », « Satisfaction », « PrixPizza »,
        /// « VentesMenu », « Four », « BoutonFour », « RevenuTotal »,
        /// « DepenseTotal », « Salaires », « BeneficeTotal », « Historique »,
        /// « ClientsServis », « PizzasVendues », « ArgentGagne », « Attente »,
        /// « SatisfactionStats ». Une seule porte d'entree pour le banc
        /// d'essai, plutot qu'une propriete par chiffre affiche.
        /// </summary>
        public string Valeur(string cle)
        {
            switch (cle)
            {
                case "Argent": return _txArgent?.text;
                case "RevenuJour": return _txRevenuJour?.text;
                case "DepenseJour": return _txDepenseJour?.text;
                case "Benefice": return _txBenefice?.text;
                case "Clients": return _txClients?.text;
                case "Satisfaction": return _txSatisfaction?.text;
                case "PrixPizza": return _txPrixPizza?.text;
                case "VentesMenu": return _txVentesMenu?.text;
                case "Four": return _txFour?.text;
                case "BoutonFour": return _txBoutonFour?.text;
                case "RevenuTotal": return _txRevenuTotal?.text;
                case "DepenseTotal": return _txDepenseTotal?.text;
                case "Salaires": return _txSalaires?.text;
                case "BeneficeTotal": return _txBeneficeTotal?.text;
                case "Historique": return _txHistorique != null && _txHistorique.Length > 0 ? _txHistorique[0].text : null;
                case "ClientsServis": return _txClientsServis?.text;
                case "PizzasVendues": return _txPizzasVendues?.text;
                case "ArgentGagne": return _txArgentGagne?.text;
                case "Attente": return _txAttente?.text;
                case "SatisfactionStats": return _txSatisfStats?.text;
                case "MasseSalariale": return _masse?.text;
                case "NiveauRH": return _txNiveauRH?.text;
                case "BoutonRH": return _txBoutonRH?.text;
                case "NiveauRepos": return _txNiveauRepos?.text;
                case "PlacesRepos": return _txPlacesRepos?.text;
                case "BoutonRepos": return _txBoutonRepos?.text;
                case "BureauxEmployes": return _txBureauxEmployes?.text;
                default: return null;
            }
        }

        /// <summary>Le libelle du bouton d'embauche/licenciement.</summary>
        public string TexteEmploi => _texteEmploi != null ? _texteEmploi.text : null;

        void Awake()
        {
            Construire();
            _ecran.SetActive(false);
        }

        void Update()
        {
            if (Joueur == null) Joueur = Object.FindObjectOfType<Joueur>();

            // Le bouton Fermer coupe l'ecran sur le champ ; le rouvrir avant
            // que le capot ait fini de se rabattre le rallumerait aussitot.
            if (_fermeDeForce)
            {
                if (Portable == null || !Portable.Ouvert) _fermeDeForce = false;
                else { if (Ouvert) _ecran.SetActive(false); return; }
            }

            bool voulu = Portable != null
                ? Portable.Ouvert
                : Joueur != null && Joueur.Assis;
            if (voulu != Ouvert) _ecran.SetActive(voulu);
            if (voulu) Rafraichir();
        }

        /// <summary>
        /// Le vrai bouton Fermer : il ferme l'ecran sur-le-champ, leve le
        /// patron et l'ecarte du fauteuil — sinon le bureau, qui rassoit
        /// quiconque s'arrete a portee les mains vides, l'y aurait remis
        /// avant la fin de l'image.
        /// </summary>
        void Fermer()
        {
            _fermeDeForce = true;
            if (Ouvert) _ecran.SetActive(false);
            if (Joueur == null) return;
            Joueur.SeLever();
            var bureau = Portable != null ? Portable.Bureau : null;
            if (bureau == null) return;
            var loin = -bureau.VersLeBureau(bureau.Place);
            if (loin.sqrMagnitude < 0.0001f) loin = Vector3.back;
            Joueur.transform.position = bureau.Place + loin * (bureau.Rayon + 1.0f);
        }

        void AfficherOnglet(string nom)
        {
            _onglet = nom;
            foreach (var paire in _pages) paire.Value.SetActive(paire.Key == nom);
            foreach (var paire in _fondsOnglets)
                paire.Value.color = paire.Key == nom ? BoutonActif : BoutonRepos;
            if (_titre != null) _titre.text = "  " + Libelle(nom);
        }

        static string Libelle(string onglet)
        {
            switch (onglet)
            {
                case "Apercu": return "Vue d'ensemble";
                case "Personnel": return "Personnel — Pizzeria";
                case "Menu": return "Menu";
                case "Restaurant": return "Restaurant";
                case "Finances": return "Finances";
                case "Statistiques": return "Statistiques";
                case "Locaux": return "Locaux";
                default: return onglet;
            }
        }

        /// <summary>
        /// Les seules valeurs qui changent en cours de partie. Toutes les
        /// pages sont rafraichies, meme cachees : rien de plus cher qu'une
        /// poignee de Text a remplacer, et l'onglet actif l'est toujours a
        /// jour des qu'on y revient.
        /// </summary>
        void Rafraichir()
        {
            if (Horloge.Active != null) _pendule.text = Horloge.Active.Affichage;

            // --- Personnel ---
            var fiches = Personnel.Fiches;
            for (int i = 0; i < _statuts.Length && i < fiches.Count; i++)
            {
                bool en = fiches[i].EstEmbauche;
                _statuts[i].text = en ? "En poste" : "Poste vacant";
                _statuts[i].color = en ? Vert : Gris;
                _productivites[i].text = Productivite(fiches[i].Nom, en);
            }
            _masse.text = "Masse salariale : " + Personnel.MasseSalariale + " / jour";
            bool embauche = Caissier != null && Caissier.Embauche;
            bool rhComplet = !embauche && Personnel.NombreEnPoste >= Comptabilite.CapaciteRH;
            _texteEmploi.text = embauche
                ? "Licencier le caissier"
                : rhComplet ? "Bureau RH complet"
                : $"Embaucher un caissier ({Reglages.PrixCaissier} $)";
            if (_boutonEmploi != null)
                _boutonEmploi.interactable = embauche
                                            || (!rhComplet && Banque.Solde >= Reglages.PrixCaissier);

            // --- Vue d'ensemble ---
            _txArgent.text = Banque.Solde + " $";
            _txRevenuJour.text = "+" + Comptabilite.RevenuJour + " $";
            _txDepenseJour.text = "-" + Comptabilite.DepenseJour + " $";
            _txBenefice.text = Comptabilite.BeneficeJour + " $";
            _txBenefice.color = Comptabilite.BeneficeJour >= 0 ? Vert : Rouge;
            var comptoir = Object.FindObjectOfType<Comptoir>();
            _txClients.text = (comptoir != null ? comptoir.Arrivees : 0).ToString();
            _txSatisfaction.text = Comptabilite.SatisfactionPourcent + " %";

            // --- Menu ---
            _txPrixPizza.text = Comptabilite.PrixPizza + " $";
            _txVentesMenu.text = Comptabilite.PizzasVendues + " pizzas vendues — "
                                + Comptabilite.RevenuTotal + " $ de recette";

            // --- Restaurant ---
            _txFour.text = $"Four — niveau {Comptabilite.NiveauFour}/{Comptabilite.NiveauFourMax}"
                          + $"  (cuisson {Comptabilite.DureeCuissonActuelle:0.0} s)";
            bool auMax = Comptabilite.NiveauFour >= Comptabilite.NiveauFourMax;
            _txBoutonFour.text = auMax ? "Four au maximum" : $"Ameliorer le four ({Comptabilite.PrixAmeliorationFour} $)";
            if (_boutonAmeliorerFour != null)
                _boutonAmeliorerFour.interactable = !auMax && Banque.Solde >= Comptabilite.PrixAmeliorationFour;

            // --- Finances ---
            _txRevenuTotal.text = "Revenus : " + Comptabilite.RevenuTotal + " $";
            _txDepenseTotal.text = "Depenses : " + Comptabilite.DepenseTotal + " $";
            _txSalaires.text = "Salaires du jour : " + Personnel.MasseSalariale + " $";
            _txBeneficeTotal.text = "Benefice cumule : " + Comptabilite.BeneficeTotal + " $";
            RafraichirHistorique();

            // --- Statistiques ---
            int servis = Comptabilite.ClientsSatisfaits + Comptabilite.ClientsInsatisfaits;
            _txClientsServis.text = "Clients servis : " + Comptabilite.ClientsSatisfaits + " / " + servis;
            _txPizzasVendues.text = "Pizzas vendues : " + Comptabilite.PizzasVendues;
            _txArgentGagne.text = "Argent gagne : " + Comptabilite.RevenuTotal + " $";
            _txAttente.text = "Temps d'attente moyen : " + Comptabilite.TempsAttenteMoyen.ToString("0.0") + " s";
            _txSatisfStats.text = "Satisfaction : " + Comptabilite.SatisfactionPourcent + " %";

            // --- Locaux ---
            bool rhAuMax = Comptabilite.NiveauBureauRH >= Comptabilite.NiveauLocalMax;
            _txNiveauRH.text = $"Niveau {Comptabilite.NiveauBureauRH}/{Comptabilite.NiveauLocalMax} — "
                              + $"capacite {Personnel.NombreEnPoste}/{Comptabilite.CapaciteRH} employes";
            _txBoutonRH.text = rhAuMax ? "Bureau RH au maximum"
                                       : $"Ameliorer le bureau RH ({Comptabilite.PrixAmeliorationRH} $)";
            if (_boutonAmeliorerRH != null)
                _boutonAmeliorerRH.interactable = !rhAuMax && Banque.Solde >= Comptabilite.PrixAmeliorationRH;

            bool reposAuMax = Comptabilite.NiveauSalleRepos >= Comptabilite.NiveauLocalMax;
            int placesOccupees = SalleRepos != null ? SalleRepos.PlacesOccupees : 0;
            int placesOuvertes = SalleRepos != null ? SalleRepos.PlacesOuvertes : Comptabilite.CapaciteSalleRepos;
            _txNiveauRepos.text = $"Niveau {Comptabilite.NiveauSalleRepos}/{Comptabilite.NiveauLocalMax} — "
                                 + $"recuperation x{Comptabilite.BonusRecuperation:0.0}";
            _txPlacesRepos.text = $"Places : {placesOccupees}/{placesOuvertes} occupees en ce moment";
            _txBoutonRepos.text = reposAuMax ? "Salle de repos au maximum"
                                             : $"Ameliorer la salle de repos ({Comptabilite.PrixAmeliorationSalleRepos} $)";
            if (_boutonAmeliorerRepos != null)
                _boutonAmeliorerRepos.interactable =
                    !reposAuMax && Banque.Solde >= Comptabilite.PrixAmeliorationSalleRepos;

            // Aucun bureau individuel construit pour l'instant : le jeu n'a
            // encore ni manager ni comptable a y affecter. Le dire plutot
            // que d'afficher un chiffre invente.
            _txBureauxEmployes.text = "0 construit — arrive avec le manager et le comptable";
        }

        static string Productivite(string nom, bool enPoste)
        {
            if (!enPoste) return "—";
            if (nom == "Caissier")
            {
                var c = Object.FindObjectOfType<Caissier>();
                if (c == null) return "—";
                int pourcent = (int)(c.Productivite * 100f);
                return $"{c.Livrees} boites — {pourcent}%";
            }
            return "—";
        }

        /// <summary>
        /// Une ligne de texte par jour clos, pas un seul bloc a sauts de
        /// ligne : plus simple a mettre en page, et chaque ligne reste
        /// verifiable a part.
        /// </summary>
        void RafraichirHistorique()
        {
            var h = Comptabilite.Historique;
            for (int i = 0; i < _txHistorique.Length; i++)
            {
                if (i >= h.Count)
                {
                    _txHistorique[i].text = i == 0 ? "Aucun jour clos pour l'instant." : "";
                    continue;
                }
                var j = h[i];
                _txHistorique[i].text = $"Jour {j.Numero} : +{j.Revenu} / -{j.Depense} = {j.Benefice} $";
            }
        }

        // ------------------------------------------------------------------

        void Construire()
        {
            _police = Hud.Police();
            var police = _police;

            var canvasGo = new GameObject("EcranBureauCanvas",
                                          typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(transform, false);
            canvasGo.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            // Au-dessus du HUD : l'ecran de l'ordinateur cache le jeu, il ne
            // se glisse pas dessous.
            canvasGo.GetComponent<Canvas>().overrideSorting = true;
            canvasGo.GetComponent<Canvas>().sortingOrder = 10;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = 0.5f;

            // La taille visee, 980x760, suppose un canevas d'au moins cette
            // taille une fois la resolution de reference appliquee. Sur un
            // ecran d'une autre forme (une fenetre d'editeur etroite, une
            // tablette large), le canevas resolu peut etre plus petit dans un
            // sens : sans ce calcul, l'ordinateur depassait du cadre au lieu
            // de rester centre et entier a l'ecran.
            float largeurDispo, hauteurDispo;
            TailleDuCanevas(scaler, out largeurDispo, out hauteurDispo);
            const float marge = 24f;
            float largeurEcran = Mathf.Clamp(largeurDispo - marge * 2f, 600f, 980f);
            float hauteurEcran = Mathf.Clamp(hauteurDispo - marge * 2f, 480f, 760f);
            const float hauteurBarre = 44f;
            const float largeurBarreLaterale = 170f;
            float largeurFenetre = largeurEcran - largeurBarreLaterale;
            float hauteurCorps = hauteurEcran - hauteurBarre;

            _ecran = Cadre("EcranOrdi", canvasGo.transform, Aubergine,
                           new Vector2(0.5f, 0.5f), new Vector2(0f, 0f),
                           new Vector2(largeurEcran, hauteurEcran));

            // la barre du haut : titre de l'OS et pendule, comme un vrai bureau
            var barre = Cadre("BarreHaut", _ecran.transform, DockSombre,
                              new Vector2(0.5f, 1f), new Vector2(0f, -hauteurBarre * 0.5f),
                              new Vector2(largeurEcran, hauteurBarre));
            _pendule = Hud.Texte(barre.transform, police, "Pizzeria OS", 24,
                                 TextAnchor.MiddleCenter, Color.white);
            Hud.Etirer(_pendule.GetComponent<RectTransform>());

            // la barre laterale : la vraie navigation, plus un dock decoratif
            var lateral = Cadre("BarreLaterale", _ecran.transform, DockSombre,
                                new Vector2(0f, 0.5f), new Vector2(largeurBarreLaterale * 0.5f, -hauteurBarre * 0.5f),
                                new Vector2(largeurBarreLaterale, hauteurCorps));

            // Pas d'icone : la police d'interface par defaut n'a pas de glyphe
            // emoji, et rien d'autre dans le jeu n'a de sprite a offrir.
            string[] onglets = { "Apercu", "Personnel", "Menu", "Restaurant",
                                 "Finances", "Statistiques", "Locaux" };
            const float hauteurBouton = 78f;
            // L'espacement se resserre tout seul s'il faut caser plus
            // d'onglets que la hauteur n'en offre a l'aise : 90 par defaut,
            // moins si necessaire, jamais assez peu pour qu'un bouton sorte
            // du bas de la barre laterale.
            float espacement = onglets.Length > 1
                ? Mathf.Min(90f, (hauteurCorps - hauteurBouton - 20f) / (onglets.Length - 1))
                : 0f;
            float yBouton = hauteurBouton * 0.5f + 10f;
            for (int i = 0; i < onglets.Length; i++)
            {
                string cible = onglets[i];
                var boutonOnglet = Bouton("Onglet" + cible, lateral.transform, BoutonRepos,
                                          new Vector2(0.5f, 1f), new Vector2(0f, -yBouton),
                                          new Vector2(largeurBarreLaterale - 16f, hauteurBouton), police,
                                          Libelle(cible).Split(' ')[0], 22, Color.white,
                                          () => AfficherOnglet(cible));
                _fondsOnglets[cible] = boutonOnglet.GetComponent<Image>();
                yBouton += espacement;
            }

            // la fenetre elle-meme, a droite de la barre laterale
            var fenetre = Cadre("Fenetre", _ecran.transform, Papier,
                                new Vector2(1f, 0.5f), new Vector2(-largeurFenetre * 0.5f, -hauteurBarre * 0.5f),
                                new Vector2(largeurFenetre, hauteurCorps));

            var titre = Cadre("EnteteFenetre", fenetre.transform, Entete,
                              new Vector2(0.5f, 1f), new Vector2(0f, -34f), new Vector2(largeurFenetre, 68f));
            _titre = Hud.Texte(titre.transform, police, "  Vue d'ensemble", 32,
                                TextAnchor.MiddleLeft, Color.white);
            Hud.Etirer(_titre.GetComponent<RectTransform>());

            // le vrai bouton Fermer, en haut a droite de la fenetre
            Bouton("BoutonFermer", titre.transform, OrangeSombre,
                  new Vector2(1f, 0.5f), new Vector2(-38f, 0f), new Vector2(46f, 46f),
                  police, "X", 28, Color.white, Fermer);

            // --- les sept pages, empilees au meme endroit ---
            _pages["Apercu"] = PageApercu(fenetre.transform, police, largeurFenetre);
            _pages["Personnel"] = PagePersonnel(fenetre.transform, police, largeurFenetre);
            _pages["Menu"] = PageMenu(fenetre.transform, police, largeurFenetre);
            _pages["Restaurant"] = PageRestaurant(fenetre.transform, police, largeurFenetre);
            _pages["Finances"] = PageFinances(fenetre.transform, police, largeurFenetre);
            _pages["Statistiques"] = PageStatistiques(fenetre.transform, police, largeurFenetre);
            _pages["Locaux"] = PageLocaux(fenetre.transform, police, largeurFenetre);

            AfficherOnglet("Apercu");
        }

        // --- Vue d'ensemble --------------------------------------------------

        GameObject PageApercu(Transform parent, Font police, float largeur)
        {
            var page = PageVide("PageApercu", parent);
            float y = -110f;
            _txArgent = Ligne(page.transform, police, ref y, "Argent actuel", Encre);
            _txRevenuJour = Ligne(page.transform, police, ref y, "Revenus du jour", Vert);
            _txDepenseJour = Ligne(page.transform, police, ref y, "Depenses du jour", Rouge);
            _txBenefice = Ligne(page.transform, police, ref y, "Benefice du jour", Encre);
            _txClients = Ligne(page.transform, police, ref y, "Clients recus", Encre);
            _txSatisfaction = Ligne(page.transform, police, ref y, "Satisfaction moyenne", Encre);
            return page;
        }

        // --- Personnel ---------------------------------------------------------

        GameObject PagePersonnel(Transform parent, Font police, float largeur)
        {
            var page = PageVide("PagePersonnel", parent);

            float y = -100f;
            Colonnes(page.transform, police, y, "NOM", "POSTE", "STATUT", "PRODUCTIVITE", "SALAIRE", Orange, 22);
            y -= 26f;
            Cadre("Filet", page.transform, Orange,
                  new Vector2(0.5f, 1f), new Vector2(0f, y), new Vector2(largeur - 30f, 4f));

            var fiches = Personnel.Fiches;
            _statuts = new Text[fiches.Count];
            _productivites = new Text[fiches.Count];
            for (int i = 0; i < fiches.Count; i++)
            {
                y -= 70f;
                var f = fiches[i];
                Text prod;
                _statuts[i] = Colonnes(page.transform, police, y, f.Nom, f.Poste, "", "",
                                       f.Salaire + " $/jour", Encre, 26, out prod);
                _productivites[i] = prod;
                y -= 36f;
                Cadre("Filet", page.transform, Filet,
                      new Vector2(0.5f, 1f), new Vector2(0f, y), new Vector2(largeur - 30f, 2f));
            }

            y -= 44f;
            _masse = Hud.Texte(page.transform, police, "", 28, TextAnchor.MiddleLeft, Encre);
            var rm = _masse.GetComponent<RectTransform>();
            rm.anchorMin = new Vector2(0.5f, 1f); rm.anchorMax = new Vector2(0.5f, 1f);
            rm.pivot = new Vector2(0.5f, 0.5f);
            rm.anchoredPosition = new Vector2(0f, y);
            rm.sizeDelta = new Vector2(largeur - 60f, 40f);

            y -= 60f;
            _boutonEmploi = Bouton("BoutonEmploi", page.transform, Orange,
                                   new Vector2(0.5f, 1f), new Vector2(0f, y),
                                   new Vector2(360f, 56f), police, "", 24, Color.white, BasculerEmploi);
            _texteEmploi = Piece(_boutonEmploi.transform, "Texte")?.GetComponent<Text>();

            return page;
        }

        /// <summary>
        /// Un seul bouton fait les deux : il embauche s'il n'y a personne, il
        /// licencie sinon. L'argent bouge pour de vrai dans les deux sens —
        /// aucun remboursement au renvoi, comme dans la vraie vie.
        /// </summary>
        void BasculerEmploi()
        {
            if (Caissier == null) return;
            if (Caissier.Embauche)
            {
                Caissier.Licencier();
                Hud.Annonce("Caissier licencie.");
                return;
            }
            if (Personnel.NombreEnPoste >= Comptabilite.CapaciteRH)
            {
                Hud.Annonce("Bureau RH complet : ameliore-le pour embaucher plus.");
                return;
            }
            if (Banque.Solde < Reglages.PrixCaissier)
            {
                Hud.Annonce("Pas assez d'argent pour embaucher.");
                return;
            }
            Banque.Retirer(Reglages.PrixCaissier);
            Comptabilite.EnregistrerDepense(Reglages.PrixCaissier);
            Caissier.Reprendre();
            // La dalle au sol paierait la meme embauche une seconde fois :
            // elle n'a plus lieu d'etre une fois payee depuis le bureau.
            if (DalleEmbauche != null) Destroy(DalleEmbauche.gameObject);
            Hud.Annonce("Caissier embauche !");
        }

        static Transform Piece(Transform parent, string nom)
        {
            for (int i = 0; i < parent.childCount; i++)
            {
                var enfant = parent.GetChild(i);
                if (enfant.gameObject.name == nom) return enfant;
            }
            return null;
        }

        // --- Menu ----------------------------------------------------------

        GameObject PageMenu(Transform parent, Font police, float largeur)
        {
            var page = PageVide("PageMenu", parent);
            float y = -110f;

            Hud.Texte(page.transform, police, "Margherita — la seule pizza au menu pour l'instant",
                      26, TextAnchor.MiddleLeft, Encre).With(t =>
            {
                var rt = t.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.5f, 1f); rt.anchorMax = new Vector2(0.5f, 1f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = new Vector2(0f, y);
                rt.sizeDelta = new Vector2(largeur - 60f, 40f);
            });
            y -= 70f;

            Hud.Texte(page.transform, police, "Prix de vente", 26, TextAnchor.MiddleLeft, Encre).With(t =>
            {
                var rt = t.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.5f, 1f); rt.anchorMax = new Vector2(0.5f, 1f);
                rt.pivot = new Vector2(0f, 0.5f);
                rt.anchoredPosition = new Vector2(-largeur * 0.5f + 30f, y);
                rt.sizeDelta = new Vector2(260f, 44f);
            });
            Bouton("BoutonPrixMoins", page.transform, Orange, new Vector2(0.5f, 1f),
                  new Vector2(60f, y), new Vector2(52f, 44f), police, "-", 30, Color.white,
                  () => { Comptabilite.DefinirPrixPizza(Comptabilite.PrixPizza - 1); });
            _txPrixPizza = Colonne(page.transform, police, 130f, y, 120f, "", TextAnchor.MiddleCenter, Encre, 28);
            Bouton("BoutonPrixPlus", page.transform, Orange, new Vector2(0.5f, 1f),
                  new Vector2(272f, y), new Vector2(52f, 44f), police, "+", 30, Color.white,
                  () => { Comptabilite.DefinirPrixPizza(Comptabilite.PrixPizza + 1); });
            y -= 70f;

            _txVentesMenu = Hud.Texte(page.transform, police, "", 24, TextAnchor.MiddleLeft, Encre);
            var rv = _txVentesMenu.GetComponent<RectTransform>();
            rv.anchorMin = new Vector2(0.5f, 1f); rv.anchorMax = new Vector2(0.5f, 1f);
            rv.pivot = new Vector2(0.5f, 0.5f);
            rv.anchoredPosition = new Vector2(0f, y);
            rv.sizeDelta = new Vector2(largeur - 60f, 40f);
            y -= 60f;

            Hud.Texte(page.transform, police,
                      "De nouvelles recettes arriveront dans une prochaine mise a jour.",
                      20, TextAnchor.MiddleLeft, Gris).With(t =>
            {
                var rt = t.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.5f, 1f); rt.anchorMax = new Vector2(0.5f, 1f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = new Vector2(0f, y);
                rt.sizeDelta = new Vector2(largeur - 60f, 60f);
            });

            return page;
        }

        // --- Restaurant ------------------------------------------------------

        GameObject PageRestaurant(Transform parent, Font police, float largeur)
        {
            var page = PageVide("PageRestaurant", parent);
            float y = -110f;

            _txFour = Hud.Texte(page.transform, police, "", 26, TextAnchor.MiddleLeft, Encre);
            var rf = _txFour.GetComponent<RectTransform>();
            rf.anchorMin = new Vector2(0.5f, 1f); rf.anchorMax = new Vector2(0.5f, 1f);
            rf.pivot = new Vector2(0.5f, 0.5f);
            rf.anchoredPosition = new Vector2(0f, y);
            rf.sizeDelta = new Vector2(largeur - 60f, 44f);
            y -= 60f;

            _boutonAmeliorerFour = Bouton("BoutonAmeliorerFour", page.transform, Orange,
                                          new Vector2(0.5f, 1f), new Vector2(0f, y),
                                          new Vector2(400f, 56f), police, "", 24, Color.white,
                                          () =>
                                          {
                                              if (Comptabilite.AmeliorerFour())
                                                  Hud.Annonce("Four ameliore !");
                                              else
                                                  Hud.Annonce("Pas assez d'argent, ou four au maximum.");
                                          });
            _txBoutonFour = Piece(_boutonAmeliorerFour.transform, "Texte")?.GetComponent<Text>();
            y -= 90f;

            Hud.Texte(page.transform, police,
                      "Tables, chaises et decorations : a venir.",
                      20, TextAnchor.MiddleLeft, Gris).With(t =>
            {
                var rt = t.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.5f, 1f); rt.anchorMax = new Vector2(0.5f, 1f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = new Vector2(0f, y);
                rt.sizeDelta = new Vector2(largeur - 60f, 90f);
            });

            return page;
        }

        // --- Finances --------------------------------------------------------

        GameObject PageFinances(Transform parent, Font police, float largeur)
        {
            var page = PageVide("PageFinances", parent);
            float y = -110f;
            _txRevenuTotal = LigneSimple(page.transform, police, ref y, largeur, Vert);
            _txDepenseTotal = LigneSimple(page.transform, police, ref y, largeur, Rouge);
            _txSalaires = LigneSimple(page.transform, police, ref y, largeur, Encre);
            _txBeneficeTotal = LigneSimple(page.transform, police, ref y, largeur, Encre);

            y -= 20f;
            Hud.Texte(page.transform, police, "Historique des derniers jours", 22,
                      TextAnchor.MiddleLeft, Orange).With(t =>
            {
                var rt = t.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.5f, 1f); rt.anchorMax = new Vector2(0.5f, 1f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = new Vector2(0f, y);
                rt.sizeDelta = new Vector2(largeur - 60f, 34f);
            });
            y -= 40f;
            _txHistorique = new Text[5];
            for (int i = 0; i < _txHistorique.Length; i++)
            {
                _txHistorique[i] = Hud.Texte(page.transform, police, "", 22, TextAnchor.MiddleLeft, Encre);
                var rh = _txHistorique[i].GetComponent<RectTransform>();
                rh.anchorMin = new Vector2(0.5f, 1f); rh.anchorMax = new Vector2(0.5f, 1f);
                rh.pivot = new Vector2(0.5f, 0.5f);
                rh.anchoredPosition = new Vector2(0f, y);
                rh.sizeDelta = new Vector2(largeur - 60f, 34f);
                y -= 38f;
            }

            return page;
        }

        // --- Statistiques ----------------------------------------------------

        GameObject PageStatistiques(Transform parent, Font police, float largeur)
        {
            var page = PageVide("PageStatistiques", parent);
            float y = -110f;
            _txClientsServis = LigneSimple(page.transform, police, ref y, largeur, Encre);
            _txPizzasVendues = LigneSimple(page.transform, police, ref y, largeur, Encre);
            _txArgentGagne = LigneSimple(page.transform, police, ref y, largeur, Vert);
            _txAttente = LigneSimple(page.transform, police, ref y, largeur, Encre);
            _txSatisfStats = LigneSimple(page.transform, police, ref y, largeur, Encre);
            return page;
        }

        // --- Locaux -----------------------------------------------------------

        GameObject PageLocaux(Transform parent, Font police, float largeur)
        {
            var page = PageVide("PageLocaux", parent);
            float y = -110f;

            Hud.Texte(page.transform, police, "Bureau RH", 26, TextAnchor.MiddleLeft, Orange).With(t =>
            {
                var rt = t.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.5f, 1f); rt.anchorMax = new Vector2(0.5f, 1f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = new Vector2(0f, y);
                rt.sizeDelta = new Vector2(largeur - 60f, 34f);
            });
            y -= 44f;
            _txNiveauRH = LigneSimple(page.transform, police, ref y, largeur, Encre);
            y += 8f;
            _boutonAmeliorerRH = Bouton("BoutonAmeliorerRH", page.transform, Orange,
                                        new Vector2(0.5f, 1f), new Vector2(0f, y),
                                        new Vector2(400f, 52f), police, "", 22, Color.white,
                                        () =>
                                        {
                                            if (Comptabilite.AmeliorerBureauRH())
                                                Hud.Annonce("Bureau RH ameliore !");
                                            else
                                                Hud.Annonce("Pas assez d'argent, ou bureau au maximum.");
                                        });
            _txBoutonRH = Piece(_boutonAmeliorerRH.transform, "Texte")?.GetComponent<Text>();
            y -= 78f;

            Hud.Texte(page.transform, police, "Salle de repos", 26, TextAnchor.MiddleLeft, Orange).With(t =>
            {
                var rt = t.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.5f, 1f); rt.anchorMax = new Vector2(0.5f, 1f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = new Vector2(0f, y);
                rt.sizeDelta = new Vector2(largeur - 60f, 34f);
            });
            y -= 44f;
            _txNiveauRepos = LigneSimple(page.transform, police, ref y, largeur, Encre);
            _txPlacesRepos = LigneSimple(page.transform, police, ref y, largeur, Encre);
            y += 8f;
            _boutonAmeliorerRepos = Bouton("BoutonAmeliorerRepos", page.transform, Orange,
                                           new Vector2(0.5f, 1f), new Vector2(0f, y),
                                           new Vector2(400f, 52f), police, "", 22, Color.white,
                                           () =>
                                           {
                                               if (Comptabilite.AmeliorerSalleRepos())
                                                   Hud.Annonce("Salle de repos amelioree !");
                                               else
                                                   Hud.Annonce("Pas assez d'argent, ou salle au maximum.");
                                           });
            _txBoutonRepos = Piece(_boutonAmeliorerRepos.transform, "Texte")?.GetComponent<Text>();
            y -= 78f;

            Hud.Texte(page.transform, police, "Bureaux employes", 26, TextAnchor.MiddleLeft, Orange).With(t =>
            {
                var rt = t.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.5f, 1f); rt.anchorMax = new Vector2(0.5f, 1f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = new Vector2(0f, y);
                rt.sizeDelta = new Vector2(largeur - 60f, 34f);
            });
            y -= 44f;
            _txBureauxEmployes = LigneSimple(page.transform, police, ref y, largeur, Gris);

            return page;
        }

        // --- utilitaires de mise en page --------------------------------------

        static GameObject PageVide(string nom, Transform parent)
        {
            var page = new GameObject(nom, typeof(RectTransform));
            page.transform.SetParent(parent, false);
            Hud.Etirer(page.GetComponent<RectTransform>());
            return page;
        }

        /// <summary>Une ligne « libelle : valeur », le libelle fixe et la valeur rafraichie.</summary>
        Text Ligne(Transform parent, Font police, ref float y, string libelle, Color couleur)
        {
            Colonne(parent, police, -390f, y, 420f, libelle, TextAnchor.MiddleLeft, Encre, 26);
            var val = Colonne(parent, police, 40f, y, 350f, "", TextAnchor.MiddleRight, couleur, 30);
            y -= 68f;
            return val;
        }

        /// <summary>Une ligne de texte libre, rafraichie en entier — pour les phrases toutes faites.</summary>
        static Text LigneSimple(Transform parent, Font police, ref float y, float largeur, Color couleur)
        {
            var t = Hud.Texte(parent, police, "", 27, TextAnchor.MiddleLeft, couleur);
            var rt = t.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 1f); rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(0f, y);
            rt.sizeDelta = new Vector2(largeur - 60f, 40f);
            y -= 54f;
            return t;
        }

        /// <summary>Une ligne du tableau du personnel : nom, poste, statut, productivite, salaire.</summary>
        static Text Colonnes(Transform parent, Font police, float y, string nom, string poste,
                             string statut, string productivite, string salaire, Color couleur, int taille,
                             out Text texteProductivite)
        {
            Colonne(parent, police, -390f, y, 150f, nom, TextAnchor.MiddleLeft, couleur, taille);
            Colonne(parent, police, -230f, y, 140f, poste, TextAnchor.MiddleLeft, couleur, taille);
            var t = Colonne(parent, police, -80f, y, 170f, statut, TextAnchor.MiddleLeft, couleur, taille);
            texteProductivite = Colonne(parent, police, 100f, y, 140f, productivite,
                                        TextAnchor.MiddleLeft, couleur, taille - 6);
            Colonne(parent, police, 250f, y, 140f, salaire, TextAnchor.MiddleRight, couleur, taille);
            return t;
        }

        /// <summary>L'en-tete du tableau : cinq colonnes, sans ligne de statut a renvoyer.</summary>
        static void Colonnes(Transform parent, Font police, float y, string nom, string poste,
                             string statut, string productivite, string salaire, Color couleur, int taille)
            => Colonnes(parent, police, y, nom, poste, statut, productivite, salaire, couleur, taille, out _);

        static Text Colonne(Transform parent, Font police, float x, float y, float largeur,
                            string contenu, TextAnchor ancre, Color couleur, int taille)
        {
            var t = Hud.Texte(parent, police, contenu, taille, ancre, couleur);
            var rt = t.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 1f); rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0f, 0.5f);
            rt.anchoredPosition = new Vector2(x, y);
            rt.sizeDelta = new Vector2(largeur, 44f);
            return t;
        }

        /// <summary>
        /// La taille du canevas, une fois la resolution de reference
        /// appliquee — le meme calcul que CanvasScaler fait lui-meme en mode
        /// Scale With Screen Size, pour connaitre la place vraiment
        /// disponible avant de poser l'ordinateur dessus.
        /// </summary>
        static void TailleDuCanevas(CanvasScaler scaler, out float largeur, out float hauteur)
        {
            float sw = Mathf.Max(1f, Screen.width), sh = Mathf.Max(1f, Screen.height);
            float rw = Mathf.Max(1f, scaler.referenceResolution.x);
            float rh = Mathf.Max(1f, scaler.referenceResolution.y);
            float logL = Mathf.Log(sw / rw, 2f);
            float logH = Mathf.Log(sh / rh, 2f);
            float facteur = Mathf.Pow(2f, Mathf.Lerp(logL, logH, scaler.matchWidthOrHeight));
            largeur = sw / facteur;
            hauteur = sh / facteur;
        }

        static GameObject Cadre(string nom, Transform parent, Color couleur, Vector2 ancre,
                                Vector2 position, Vector2 taille)
        {
            var go = new GameObject(nom, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = ancre; rt.anchorMax = ancre; rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = position;
            rt.sizeDelta = taille;
            go.AddComponent<Image>().color = couleur;
            return go;
        }

        /// <summary>
        /// Un vrai bouton : fond colore, texte au milieu, transition de
        /// teinte pour les etats survol/appui — pas juste un cadre qui
        /// ressemble a un bouton.
        /// </summary>
        static Button Bouton(string nom, Transform parent, Color couleur, Vector2 ancre,
                             Vector2 position, Vector2 taille, Font police, string texte,
                             int tailleTexte, Color couleurTexte, UnityAction agir)
        {
            var go = Cadre(nom, parent, couleur, ancre, position, taille);
            var image = go.GetComponent<Image>();
            var bouton = go.AddComponent<Button>();
            bouton.targetGraphic = image;
            bouton.transition = Selectable.Transition.ColorTint;
            bouton.onClick.AddListener(agir);
            var t = Hud.Texte(go.transform, police, texte, tailleTexte, TextAnchor.MiddleCenter, couleurTexte);
            Hud.Etirer(t.GetComponent<RectTransform>());
            return bouton;
        }
    }

    static class ExtensionsEcranBureau
    {
        /// <summary>Applique une configuration a un objet et le renvoie — pour chainer sans variable.</summary>
        public static T With<T>(this T objet, System.Action<T> configurer)
        {
            configurer(objet);
            return objet;
        }
    }
}
