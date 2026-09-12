using UnityEngine;

namespace Pizzeria3D
{
    /// <summary>
    /// Un client : arrive, avance dans la file, recoit ses pizzas une a une,
    /// paie, puis s'attable si la salle a de la place — sinon il emporte ses
    /// boites et s'en va. S'il attend trop longtemps, il part sans payer.
    ///
    /// Seul celui qui ne commande qu'une pizza mange sur place, et il la
    /// recoit nue : on n'emballe que ce qui s'emporte. Il retient sa place
    /// des le debut de la commande, sinon un autre la prendrait pendant
    /// qu'on le sert.
    /// </summary>
    public sealed class Client : MonoBehaviour
    {
        public int Pizzas { get; private set; }
        public bool EstServi => _recues >= Pizzas;
        public bool EstArrive { get; private set; }

        /// <summary>Le client parle au caissier : rien ne lui est remis avant la fin.</summary>
        public bool CommandeCommencee { get; private set; }
        public bool CommandeFinie => CommandeCommencee && _resteCommande <= 0f;

        Comptoir _comptoir;
        Pile _sac;
        Bulle _bulle;
        Demarche _demarche;
        TableRepas _table;        // la place qu'il a retenue, s'il en a trouve une
        bool _versLaTable;        // servi, il marche vers son siege
        bool _attable;
        float _resteRepas;
        int _quartiersManges;
        Vector3 _cible;
        bool _sEnVa;
        int _recues;
        float _patience;
        float _compteurRemise;
        float _resteCommande = Reglages.DureeCommande;

        public static Client Creer(Comptoir comptoir, int rang)
        {
            var go = new GameObject("Client");
            var c = go.AddComponent<Client>();
            c._comptoir = comptoir;
            c.Pizzas = Random.Range(Reglages.PizzasParClientMin, Reglages.PizzasParClientMax + 1);
            c._patience = Reglages.PatienceClient;

            // arrive depuis le fond, en dehors du champ
            go.transform.position = comptoir.PlaceDeLaFile(rang) + new Vector3(0f, 0f, -8f);
            c._cible = comptoir.PlaceDeLaFile(rang);
            c.Silhouette();
            return c;
        }

        /// <summary>
        /// Chaque client est habille au hasard : hauts, pantalons et chaussures
        /// piochés dans des palettes distinctes, cheveux courts ou longs. Sans
        /// cette variete, la file ressemble a une armee de clones.
        /// </summary>
        void Silhouette()
        {
            bool femme = Random.value < 0.5f;

            // Carrure : la plupart des gens sont dans la moyenne, les extremes
            // restent minoritaires — une file entierement obese ou maigre ne
            // ressemblerait a rien.
            float tirage = Random.value;
            float largeur = tirage < 0.25f ? Random.Range(0.80f, 0.88f)     // maigres
                          : tirage < 0.70f ? Random.Range(0.94f, 1.08f)     // moyens
                                           : Random.Range(1.18f, 1.36f);    // corpulents

            var a = new Personnage.Apparence
            {
                Tshirt     = Hauts[Random.Range(0, Hauts.Length)],
                Pantalon   = Bas[Random.Range(0, Bas.Length)],
                Chaussures = Chaussures[Random.Range(0, Chaussures.Length)],
                Peau       = Peaux[Random.Range(0, Peaux.Length)],
                Cheveux    = Cheveux[Random.Range(0, Cheveux.Length)],
                // les femmes portent les cheveux longs, les hommes courts
                Tete = femme ? Personnage.Coiffure.CheveuxLongs
                             : Personnage.Coiffure.CheveuxCourts,
                Largeur = largeur,
                Hauteur = femme ? Random.Range(0.92f, 1.00f) : Random.Range(0.98f, 1.06f),
            };

            var membres = Personnage.Construire(transform, a);

            // ils marchent jusqu'au comptoir : sans demarche ils glisseraient
            var d = _demarche = gameObject.AddComponent<Demarche>();
            d.VitesseReference = Reglages.AllureClient;
            d.Corps = membres.Corps;
            d.HancheG = membres.HancheG; d.HancheD = membres.HancheD;
            d.GenouG = membres.GenouG;   d.GenouD = membres.GenouD;
            d.EpauleG = membres.EpauleG; d.EpauleD = membres.EpauleD;

            // il porte ses boites a bout de bras, comme le personnel
            _sac = Portage.Creer(transform, "Sac");
            _sac.Max = Reglages.PizzasParClientMax;

            _bulle = Bulle.Creer(transform, 2.15f);
            _bulle.Afficher(Pizzas);
        }

        // --- garde-robe ---
        static readonly Color[] Hauts =
        {
            Bloc.Couleur(0xE05C4E), Bloc.Couleur(0x4B8FE0), Bloc.Couleur(0x62B36B),
            Bloc.Couleur(0xE0B44A), Bloc.Couleur(0x9A6FD0), Bloc.Couleur(0xE08AB4),
            Bloc.Couleur(0x38B2A8), Bloc.Couleur(0xF0F0EA),
        };
        static readonly Color[] Bas =
        {
            Bloc.Couleur(0x36527A), Bloc.Couleur(0x2E2E36), Bloc.Couleur(0x7A6A56),
            Bloc.Couleur(0x5A5F68), Bloc.Couleur(0x8C4A4A),
        };
        static readonly Color[] Chaussures =
        {
            Bloc.Couleur(0xF2F2F2), Bloc.Couleur(0x22242A), Bloc.Couleur(0xC0392B),
            Bloc.Couleur(0x3D6EB4), Bloc.Couleur(0x8A5A2B),
        };
        static readonly Color[] Peaux =
        {
            Bloc.Couleur(0xF0C8A0), Bloc.Couleur(0xD9A066), Bloc.Couleur(0xA9713F),
            Bloc.Couleur(0x6E4326), Bloc.Couleur(0x40281A),
        };
        static readonly Color[] Cheveux =
        {
            Bloc.Couleur(0x2B1D14), Bloc.Couleur(0x6B4423), Bloc.Couleur(0xC9A227),
            Bloc.Couleur(0x8C2F2F), Bloc.Couleur(0x1A1A1E), Bloc.Couleur(0xD8D8D8),
        };

        public void RangA(int rang)
        {
            if (_sEnVa) return;
            _cible = _comptoir.PlaceDeLaFile(rang);
        }

        void Update()
        {
            if (_demarche != null && _sac != null) _demarche.BrasPortent = !_sac.EstVide;
            if (_attable) { Manger(); return; }

            Avancer();
            // Ni celui qui s'en va, ni celui qui rejoint sa table n'attend.
            // Retenir une place, en revanche, ne rend pas patient : sans cette
            // distinction il restait au comptoir indefiniment et bloquait la
            // file derriere lui.
            if (_sEnVa || _versLaTable || !EstArrive) return;

            _patience -= Time.deltaTime;
            if (_patience <= 0f)
            {
                _comptoir.Oublier(this);
                Partir();
            }
        }

        void Avancer()
        {
            var cible = _sEnVa ? _comptoir.PointDeSortie : _cible;
            var delta = cible - transform.position;
            delta.y = 0f;

            if (delta.sqrMagnitude < 0.02f)
            {
                if (_sEnVa) { Destroy(gameObject); return; }
                if (_versLaTable) { Asseoir(); return; }
                EstArrive = true;
                return;
            }
            var pas = delta.normalized;
            transform.position += pas * Reglages.VitesseClient * Time.deltaTime;
            // Il regarde ou il va. Sans cela il traversait la salle de profil,
            // ou a reculons, ce qui se voyait tout de suite.
            transform.rotation = Quaternion.Slerp(transform.rotation,
                                                  Quaternion.LookRotation(pas, Vector3.up),
                                                  Reglages.VitesseRotation * Time.deltaTime);
        }

        /// <summary>
        /// Il s'installe : cale sur la chaise, tourne vers le plateau, et sa
        /// pizza quitte ses mains pour l'assiette.
        /// </summary>
        void Asseoir()
        {
            _attable = true;
            _resteRepas = Reglages.DureeRepas;

            var place = _table.Place;
            transform.position = new Vector3(place.x, Reglages.HauteurAssise, place.z);
            transform.rotation = Quaternion.LookRotation(_table.VersLaTable(place), Vector3.up);
            if (_demarche != null) _demarche.Assis = true;

            if (_sac != null) _sac.Vider();     // elle passe de ses mains a la table
            _table.PoserPizza();
        }

        /// <summary>
        /// Le repas s'egrene, une part a la fois : la pizza fond dans
        /// l'assiette au lieu de disparaitre d'un bloc. A la fin il ne laisse
        /// que des restes, rend la place et sort.
        /// </summary>
        void Manger()
        {
            _resteRepas -= Time.deltaTime;

            // une part avalee a chaque quart du repas ecoule
            float part = Reglages.DureeRepas / Reglages.QuartiersParPizza;
            int avalees = Reglages.QuartiersParPizza
                        - Mathf.CeilToInt(Mathf.Max(0f, _resteRepas) / part);
            while (_table != null && _quartiersManges < avalees && _table.CroquerUnQuartier())
                _quartiersManges++;

            if (_resteRepas > 0f) return;

            _attable = false;
            _versLaTable = false;
            if (_demarche != null) _demarche.Assis = false;
            transform.position = new Vector3(transform.position.x, 0f, transform.position.z);
            if (_table != null)
            {
                // il laisse l'addition sur la table, avec ses restes
                int montant = Pizzas * Comptabilite.PrixPizza;
                Billet.Lacher(_table.transform.position, montant,
                              _comptoir != null ? _comptoir.Joueur : null, 0.25f, 1.05f);
                Comptabilite.EnregistrerVente(Pizzas, montant);
                _table.LaisserOrdures();
                _table.Liberer(this);
                _table = null;
            }
            _sEnVa = true;
        }

        /// <summary>Egrene les secondes de commande une fois le client au comptoir.</summary>
        public void Commander(float deltaTemps)
        {
            if (!CommandeCommencee) ChoisirSaPlace();
            CommandeCommencee = true;
            if (_resteCommande > 0f) _resteCommande -= deltaTemps;
        }

        /// <summary>
        /// Retient la table s'il n'a commande qu'une pizza. C'est ici que se
        /// decide s'il mangera sur place, donc aussi si on l'emballe.
        /// </summary>
        void ChoisirSaPlace()
        {
            if (Pizzas != 1) return;
            var table = _comptoir != null ? _comptoir.Table : null;
            if (table != null && table.Accueillir(this)) _table = table;
        }

        /// <summary>Vrai s'il a une place retenue : sa pizza ne sera pas emballee.</summary>
        public bool SurPlace => _table != null;

        /// <summary>
        /// Prend ce qu'on lui tend si le rythme de remise le permet : une
        /// boite a emporter, ou un plateau s'il mange sur place.
        /// </summary>
        public bool Recevoir(Pile.Forme forme)
        {
            if (EstServi) return false;
            if (_compteurRemise > 0f) { _compteurRemise -= Time.deltaTime; return false; }
            _compteurRemise = Reglages.DelaiTransfert * 2f;
            _recues++;
            if (_sac != null) _sac.Ajouter(forme);
            if (_bulle != null) _bulle.Afficher(Pizzas - _recues);
            return true;
        }

        public void Partir()
        {
            if (_bulle != null) _bulle.Afficher(0);   // la commande n'a plus lieu d'etre

            // La satisfaction se lit ici, une seule fois : servi ou non, et
            // avec quelle patience il lui restait — c'est le temps qu'il aura
            // vraiment attendu.
            Comptabilite.EnregistrerDepart(EstServi, Reglages.PatienceClient - _patience);

            // Servi avec sa place retenue : il va s'attabler.
            if (EstServi && _table != null)
            {
                _versLaTable = true;
                _cible = _table.Place;
                EstArrive = false;
                return;
            }

            // Parti sans manger — lasse d'attendre : il rend la place.
            if (_table != null) { _table.Liberer(this); _table = null; }
            _sEnVa = true;
        }

        /// <summary>Vrai tant qu'il mange, assis a la table de la salle.</summary>
        public bool Attable => _attable;

        /// <summary>Ce qu'il reste a lui remettre, tel qu'affiche dans sa bulle.</summary>
        public int Restant => Pizzas - _recues;

        /// <summary>Vrai des qu'il quitte la file, servi ou lasse d'attendre.</summary>
        public bool SEnVa => _sEnVa;
    }
}
