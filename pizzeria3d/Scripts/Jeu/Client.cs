using UnityEngine;

namespace Pizzeria3D
{
    /// <summary>
    /// Un client : arrive, avance dans la file, recoit ses pizzas une a une,
    /// paie, puis s'en va. S'il attend trop longtemps, il part sans payer.
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
            var d = gameObject.AddComponent<Demarche>();
            d.VitesseReference = Reglages.VitesseClient;
            d.Corps = membres.Corps;
            d.HancheG = membres.HancheG; d.HancheD = membres.HancheD;
            d.EpauleG = membres.EpauleG; d.EpauleD = membres.EpauleD;

            var sac = new GameObject("Sac");
            sac.transform.SetParent(transform, false);
            sac.transform.localPosition = new Vector3(0.55f, 1.1f, 0f);
            _sac = sac.AddComponent<Pile>();
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
            Avancer();
            if (_sEnVa || !EstArrive) return;

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
                EstArrive = true;
                return;
            }
            transform.position += delta.normalized * Reglages.VitesseClient * Time.deltaTime;
        }

        /// <summary>Egrene les secondes de commande une fois le client au comptoir.</summary>
        public void Commander(float deltaTemps)
        {
            CommandeCommencee = true;
            if (_resteCommande > 0f) _resteCommande -= deltaTemps;
        }

        /// <summary>
        /// Prend une boite si le rythme de remise le permet. Elles s'empilent
        /// dans son sac les unes sur les autres, sans jamais se melanger a une
        /// pizza nue : le comptoir n'en sert pas.
        /// </summary>
        public bool Recevoir(bool emballee)
        {
            if (EstServi) return false;
            if (_compteurRemise > 0f) { _compteurRemise -= Time.deltaTime; return false; }
            _compteurRemise = Reglages.DelaiTransfert * 2f;
            _recues++;
            if (_sac != null) _sac.Ajouter(emballee);
            if (_bulle != null) _bulle.Afficher(Pizzas - _recues);
            return true;
        }

        public void Partir()
        {
            _sEnVa = true;
            if (_bulle != null) _bulle.Afficher(0);   // la commande n'a plus lieu d'etre
        }

        /// <summary>Ce qu'il reste a lui remettre, tel qu'affiche dans sa bulle.</summary>
        public int Restant => Pizzas - _recues;

        /// <summary>Vrai des qu'il quitte la file, servi ou lasse d'attendre.</summary>
        public bool SEnVa => _sEnVa;
    }
}
